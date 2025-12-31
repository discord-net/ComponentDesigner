using Discord.CX.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Discord.CX.Util;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components.SelectMenus;

public enum SelectKind
{
    String,
    User,
    Role,
    Channel,
    Mentionable
}

public sealed class SelectMenuComponentNode : ComponentNode
{
    public sealed record MissingTypeState(GraphNode GraphNode, ICXNode Source)
        : ComponentState(GraphNode, Source);

    public sealed record InvalidTypeState(
        GraphNode GraphNode,
        ICXNode Source,
        string? Kind = null
    ) : ComponentState(GraphNode, Source)
    {
        public bool Equals(InvalidTypeState? other)
        {
            if (other is null) return false;

            return
                Kind == other.Kind &&
                base.Equals(other);
        }

        public override int GetHashCode()
            => Hash.Combine(Kind, base.GetHashCode());
    }

    public sealed record SelectState(
        GraphNode GraphNode,
        ICXNode Source,
        SelectKind Kind,
        EquatableArray<SelectMenuDefaultValue> Defaults,
        EquatableArray<SelectMenuInterpolatedOption> InterpolatedOptions
    ) : ComponentState(GraphNode, Source)
    {
        public bool Equals(SelectState? other)
        {
            if (other is null) return false;

            return
                Kind == other.Kind &&
                Defaults.Equals(other.Defaults) &&
                InterpolatedOptions.Equals(other.InterpolatedOptions) &&
                base.Equals(other);
        }

        public override int GetHashCode()
            => Hash.Combine(Kind, Defaults, InterpolatedOptions, base.GetHashCode());
    }

    public override string Name => "select-menu";

    public override IReadOnlyList<string> Aliases { get; } = ["select"];

    public ComponentProperty Id { get; }
    public ComponentProperty Type { get; }
    public ComponentProperty CustomId { get; }
    public ComponentProperty Placeholder { get; }
    public ComponentProperty MinValues { get; }
    public ComponentProperty MaxValues { get; }
    public ComponentProperty Required { get; }
    public ComponentProperty Disabled { get; }

    public override ImmutableArray<ComponentProperty> Properties { get; }

    public override bool HasChildren => true;

    public SelectMenuComponentNode()
    {
        Properties =
        [
            Id = ComponentProperty.Id,
            Type = new(
                "type",
                isOptional: true,
                synthetic: true
            ),
            CustomId = new(
                "customId",
                isOptional: false,
                renderer: CXValueGenerator.String
            ),
            Placeholder = new(
                "placeholder",
                isOptional: true,
                renderer: CXValueGenerator.String
            ),
            MinValues = new(
                "minValues",
                isOptional: true,
                aliases: ["min"],
                renderer: CXValueGenerator.Integer,
                validators:
                [
                    Validators.IntRange(
                        Constants.STRING_SELECT_MIN_VALUES,
                        Constants.STRING_SELECT_MAX_VALUES
                    )
                ]
            ),
            MaxValues = new(
                "maxValues",
                isOptional: true,
                aliases: ["max"],
                renderer: CXValueGenerator.Integer,
                validators:
                [
                    Validators.IntRange(
                        Constants.STRING_SELECT_MIN_VALUES + 1,
                        Constants.STRING_SELECT_MAX_VALUES
                    )
                ]
            ),
            Required = new(
                "required",
                isOptional: true,
                renderer: CXValueGenerator.Boolean
            ),
            Disabled = new(
                "disabled",
                isOptional: true,
                renderer: CXValueGenerator.Boolean,
                dotnetParameterName: "isDisabled"
            )
        ];
    }

    public override void AddGraphNode(ComponentGraphInitializationContext context)
    {
        if (!AutoActionRowComponentNode.AddPossibleAutoRowNode(this, context))
        {
            context.Push(
                this,
                cxNode: context.CXNode
            );
        }
    }

    public override ComponentState? Create(
        ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        if (context.CXNode is not CXElement element) return null;

        var typeAttribute = element.Attributes
            .FirstOrDefault(x => x.Identifier.ToLowerInvariant() is "type");

        if (typeAttribute is null)
            return new MissingTypeState(context.GraphNode, context.CXNode);

        if (typeAttribute.Value is not CXValue.StringLiteral { HasInterpolations: false } typeValue)
            return new InvalidTypeState(context.GraphNode, context.CXNode);

        if (!TryGetSelectKind(typeValue.Tokens.ToString(), out var kind))
            return new InvalidTypeState(context.GraphNode, context.CXNode, typeValue.Tokens.ToString());

        var defaults = new List<SelectMenuDefaultValue>();
        var interpolatedOptions = new List<SelectMenuInterpolatedOption>();

        switch (kind)
        {
            case SelectKind.Channel or SelectKind.Role or SelectKind.User or SelectKind.Mentionable:
                defaults.AddRange(element.Children.SelectMany(FlattenNode).Select(SelectMenuDefaultValue.Create));
                break;
            case SelectKind.String:
                context.AddChildren(element.Children.OfType<CXElement>());
                var candidates = GetCandidateInterpolationOptions(context.CXNode).ToArray();

                foreach (var candidate in candidates)
                {
                    if (
                        SelectMenuInterpolatedOption.TryCreate(
                            context.GraphContext,
                            candidate,
                            diagnostics,
                            out var option)
                    ) interpolatedOptions.Add(option);
                }

                break;
        }

        return new SelectState(context.GraphNode, context.CXNode, kind, [..defaults], [..interpolatedOptions]);

        static IEnumerable<ICXNode> GetCandidateInterpolationOptions(ICXNode node)
        {
            if (node is not CXElement element) yield break;

            foreach
            (
                var child
                in element.Children.SelectMany(IEnumerable<ICXNode> (x) =>
                    x is CXValue.Multipart mp ? mp.Tokens : [x]
                )
            )
            {
                if (
                    child is CXToken { Kind: CXTokenKind.Interpolation } ||
                    child is CXValue.Interpolation
                ) yield return child;
            }
        }

        static IEnumerable<ICXNode> FlattenNode(CXNode node)
            => node switch
            {
                CXValue.Multipart multipart => multipart.Tokens,
                _ => [node]
            };

        static bool TryGetSelectKind(string name, out SelectKind kind)
        {
            switch (name.ToLowerInvariant())
            {
                case "string" or "text":
                    kind = SelectKind.String;
                    return true;
                case "user":
                    kind = SelectKind.User;
                    return true;
                case "role":
                    kind = SelectKind.Role;
                    return true;
                case "channel":
                    kind = SelectKind.Channel;
                    return true;
                case "mention" or "mentionable":
                    kind = SelectKind.Mentionable;
                    return true;
            }

            kind = default;
            return false;
        }
    }

    public override void Validate(ComponentState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        base.Validate(state, context, diagnostics);

        switch (state)
        {
            case MissingTypeState:
                diagnostics.Add(
                    Diagnostics.MissingSelectMenuType,
                    state.Source
                );
                return;
            case InvalidTypeState { Kind: var kind }:
                diagnostics.Add(
                    kind is not null
                        ? Diagnostics.SpecifiedInvalidSelectMenuType(kind)
                        : Diagnostics.InvalidSelectMenuType,
                    state.Source
                );
                return;
            case SelectState selectState:

                Validators.Range(context, state, MinValues, MaxValues, diagnostics);

                switch (selectState.Kind)
                {
                    case SelectKind.String:
                        ValidateStringSelectMenu(selectState, context, diagnostics);
                        break;
                    default:
                        ValidateDefaultValues(selectState, context, diagnostics);
                        break;
                }

                break;
        }
    }

    private void ValidateDefaultValues(SelectState state, IComponentContext context, IList<DiagnosticInfo> diagnostics)
    {
        foreach (var defaultValue in state.Defaults)
        {
            defaultValue.Validate(context, state, diagnostics);

            switch (state.Kind, defaultValue.Kind)
            {
                // any unknown kinds don't get checked
                case (_, SelectMenuDefaultValueKind.Unknown): continue;

                case (SelectKind.Channel, not SelectMenuDefaultValueKind.Channel)
                    or (SelectKind.User, not SelectMenuDefaultValueKind.User)
                    or (SelectKind.Role, not SelectMenuDefaultValueKind.Role):
                    diagnostics.Add(
                        Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu(
                            defaultValue.Kind.ToString(),
                            state.Kind.ToString()
                        ),
                        defaultValue.Owner
                    );
                    continue;
            }
        }
    }

    private void ValidateStringSelectMenu(
        SelectState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        var validChildrenCount = 0;

        foreach (var child in state.Children)
        {
            if (!IsValidStringSelectChild(child.Inner))
            {
                diagnostics.Add(
                    Diagnostics.InvalidStringSelectChild(child.Inner.Name),
                    child.State.Source
                );
            }
            else validChildrenCount++;
        }

        switch (validChildrenCount)
        {
            case 0:
                diagnostics.Add(
                    Diagnostics.EmptyStringSelectMenu,
                    state.Source
                );
                break;
            case > Constants.STRING_SELECT_MAX_VALUES:
                diagnostics.Add(
                    Diagnostics.TooManyStringSelectMenuChildren,
                    TextSpan.FromBounds(
                        state.Children.Skip(Constants.STRING_SELECT_MAX_VALUES).First()
                            .State!.Source.Span.Start,
                        state.Children.Skip(Constants.STRING_SELECT_MAX_VALUES).Last()
                            .State!.Source.Span.End
                    )
                );
                break;
        }
    }

    private static bool IsValidStringSelectChild(ComponentNode node)
        => node is IDynamicComponentNode or SelectMenuOptionComponentNode;

    public override Result<string> Render(
        ComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    )
    {
        if (state is not SelectState selectState) return default;

        return state
            .RenderProperties(this, context)
            .Combine(
                selectState.Kind is SelectKind.String
                    ? GetStringSelectOrderedChildrenRenderers(selectState, context)
                        .Select(x => x(selectState, context, options))
                        .FlattenAll()
                        .Map(x => x.Count > 0
                            ? $"""
                               options:
                               [
                                   {string.Join($",{Environment.NewLine}", x).WithNewlinePadding(4)}
                               ]
                               """
                            : string.Empty
                        )
                    : string.Empty
            )
            .Combine(
                selectState.Defaults
                    .Select(x => x.Render(context, selectState))
                    .FlattenAll()
                    .Map(x => x.Count > 0
                        ? $"""
                           defaultValues:
                           [
                               {string.Join($",{Environment.NewLine}", x).WithNewlinePadding(4)}
                           ]
                           """
                        : string.Empty
                    )
            )
            .Map(x =>
            {
                var ((props, children), defaults) = x;

                return
                    $"""
                     new {context.KnownTypes.SelectMenuBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
                         string
                             .Join(
                                 $",{Environment.NewLine}",
                                 ((IEnumerable<string>)
                                 [
                                     $"type: {ToDiscordComponentType(context, selectState.Kind)}",
                                     props,
                                     children,
                                     defaults
                                 ])
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                             )
                             .PrefixIfSome(4)
                             .WithNewlinePadding(4)
                             .WrapIfSome(Environment.NewLine)
                     })
                     """;
            })
            .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));

        static IEnumerable<ComponentNodeRenderer<SelectState>> GetStringSelectOrderedChildrenRenderers(
            SelectState state,
            IComponentContext context
        )
        {
            if (state.Source is not CXElement selectElement) yield break;

            var stack = new Stack<ICXNode>();

            // push the children in reverse
            for (var i = selectElement.Children.Count - 1; i >= 0; i--)
                stack.Push(selectElement.Children[i]);

            var childPointer = 0;
            var interpPointer = 0;

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                switch (current)
                {
                    case CXElement element:
                        var child = state.Children.Count > childPointer
                            ? state.Children[childPointer++]
                            : null;

                        if (ReferenceEquals(element, child?.State.Source))
                            yield return (_, context, options) => child.Render(context, options);

                        break;

                    case CXValue.Multipart multi:
                        for (var i = multi.Tokens.Count - 1; i >= 0; i--)
                            stack.Push(multi.Tokens[i]);
                        break;

                    case CXValue.Interpolation interp
                        when TryFromInterpolationToken(
                            interp.Token,
                            ref interpPointer,
                            state,
                            out var renderer
                        ):
                        yield return renderer;
                        break;
                    case CXToken { Kind: CXTokenKind.Interpolation } token
                        when TryFromInterpolationToken(
                            token,
                            ref interpPointer,
                            state,
                            out var renderer
                        ):
                        yield return renderer;
                        break;
                }
            }

            static bool TryFromInterpolationToken(
                CXToken token,
                ref int pointer,
                SelectState state,
                out ComponentNodeRenderer<SelectState> renderer
            )
            {
                var option = state.InterpolatedOptions.Count > pointer
                    ? state.InterpolatedOptions[pointer++]
                    : null;


                if (ReferenceEquals(option?.Interpolation, token))
                {
                    renderer = option.Render;
                    return true;
                }

                renderer = null!;
                return false;
            }
        }
    }

    private static string ToDiscordComponentType(IComponentContext context, SelectKind kind)
        => $"{context.KnownTypes.ComponentTypeEnumType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{
            kind switch {
                SelectKind.Channel => "ChannelSelect",
                SelectKind.Role => "RoleSelect",
                SelectKind.User => "UserSelect",
                SelectKind.Mentionable => "MentionableSelect",
                SelectKind.String => "SelectMenu",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            }
        }";

    //     public override string Render(ComponentState state, ComponentContext context)
//         => $"""
//             new {context.KnownTypes.SelectMenuBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({
//                 string
//                     .Join(
//                         $",{Environment.NewLine}",
//                         ((IEnumerable<string>)
//                         [
//                             (
//                                 state switch
//                                 {
//                                     UserSelectState => "UserSelect",
//                                     RoleSelectState => "RoleSelect",
//                                     MentionableSelectState => "MentionableSelect",
//                                     ChannelSelectState => "ChannelSelect",
//                                     _ => string.Empty
//                                 }
//                             ).Map(x => $"type: {context.KnownTypes.ComponentTypeEnumType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{x}"),
//                             state.RenderProperties(this, context),
//                             state.RenderChildren(context, x => x.Inner is StringSelectOptionComponentNode)
//                                 .Map(x =>
//                                     $"""
//                                      options:
//                                      [
//                                          {x.WithNewlinePadding(4)}
//                                      ]
//                                      """
//                                 ),
//                             state is SelectStateWithDefaults { Defaults: var defaults }
//                                 ? string
//                                     .Join(
//                                         $",{Environment.NewLine}",
//                                         defaults.Select(x => RenderDefaultValue(context, x))
//                                     )
//                                     .Map(x =>
//                                         $"""
//                                          defaultValues:
//                                          [
//                                              {x.WithNewlinePadding(4)}
//                                          ]
//                                          """
//                                     )
//                                 : string.Empty
//                         ]).Where(x => !string.IsNullOrEmpty(x))
//                     )
//                     .PrefixIfSome(4)
//                     .WithNewlinePadding(4)
//                     .WrapIfSome(Environment.NewLine)
//             })
//             """;
}