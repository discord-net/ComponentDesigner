using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Discord.CX.Nodes.Components.SelectMenus;
using Discord.CX.Parser;
using Discord.CX.Util;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components;

public sealed record LabelComponentState(
    GraphNode GraphNode,
    ICXNode Source,
    CXValue? ChildValue,
    CXElement? ChildElement
) : ComponentState(GraphNode, Source)
{
    public bool Equals(LabelComponentState? other)
    {
        if (other is null) return false;

        return
            (ChildValue?.Equals(other.ChildValue) ?? other.ChildValue is null) &&
            (ChildElement?.Equals(other.ChildElement) ?? other.ChildElement is null) &&
            base.Equals(other);
    }

    public override int GetHashCode()
        => Hash.Combine(ChildValue, ChildElement, base.GetHashCode());
}

public sealed class LabelComponentNode : ComponentNode<LabelComponentState>
{
    public override string Name => "label";

    public ComponentProperty Component { get; }
    public ComponentProperty Value { get; }
    public ComponentProperty Description { get; }

    public override bool HasChildren => true;

    public override ImmutableArray<ComponentProperty> Properties { get; }

    private static readonly ComponentRenderingOptions ChildRenderingOptions = new(
        TypingContext: new(
            CanSplat: false,
            ConformingType: ComponentBuilderKind.IMessageComponentBuilder
        )
    );

    public LabelComponentNode()
    {
        Properties =
        [
            ComponentProperty.Id,
            Component = new(
                "component",
                renderer: CXValueGenerator.Component
            ),
            Value = new(
                "value",
                renderer: CXValueGenerator.String
            ),
            Description = new(
                "description",
                isOptional: true,
                renderer: CXValueGenerator.String
            )
        ];
    }

    public override LabelComponentState? CreateState(ComponentStateInitializationContext context,
        IList<DiagnosticInfo> diagnostics)
    {
        if (context.CXNode is not CXElement element) return null;


        CXValue? childValue = null;
        CXElement? component = null;

        switch (element.Children.FirstOrDefault())
        {
            case CXValue value:
            {
                // state.SubstitutePropertyValue(Value, value);
                // state.ChildValue = value;
                childValue = value;

                if (element.Children.Count > 1 && element.Children[1] is CXElement labelComponent)
                    component = labelComponent;
                break;
            }
            case CXElement labelComponent:
                component = labelComponent;
                break;
        }

        var state = new LabelComponentState(
            context.GraphNode,
            element,
            childValue,
            component
        );

        if (state.ChildValue is not null) state.SubstitutePropertyValue(Value, state.ChildValue);
        if (state.ChildElement is not null)
            state.SubstitutePropertyValue(Component,
                new CXValue.Element(
                    CXToken.CreateSynthetic(CXTokenKind.OpenParenthesis),
                    state.ChildElement,
                    CXToken.CreateSynthetic(CXTokenKind.CloseParenthesis)
                )
            );

        return state;
    }

    public override void Validate(
        LabelComponentState state,
        IComponentContext context,
        IList<DiagnosticInfo> diagnostics
    )
    {
        base.Validate(state, context, diagnostics);

        var component = state.GetProperty(Component);

        if (component.Attribute is not null && state.ChildValue is not null)
        {
            // specified both in attribute and in the children
            diagnostics.Add(
                Diagnostics.LabelComponentDuplicate,
                component.Attribute
            );

            diagnostics.Add(
                Diagnostics.LabelComponentDuplicate,
                state.ChildValue
            );
        }

        if (context.KnownTypes.LabelBuilderType is null)
        {
            diagnostics.Add(
                Diagnostics.MissingTypeInAssembly(nameof(context.KnownTypes.LabelBuilderType)),
                state.Source
            );
        }

        foreach (var child in ((CXElement)state.Source).Children)
        {
            if (child != state.ChildValue && child != state.ChildElement)
            {
                diagnostics.Add(
                    Diagnostics.TooManyChildrenInLabel,
                    child
                );
            }
        }

        var labelChild = state.GetProperty(Component).GraphNode;

        if (labelChild is not null && !IsValidLabelChild(labelChild.Inner))
        {
            diagnostics.Add(
                Diagnostics.InvalidLabelChild,
                labelChild.State.Source
            );
        }
    }

    private static bool IsValidLabelChild(ComponentNode node)
        => node is IDynamicComponentNode
            or SelectMenuComponentNode
            or TextInputComponentNode
            or FileUploadComponentNode;

    public override Result<string> Render(
        LabelComponentState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Combine((state.Children.FirstOrDefault()?.Render(context, options: ChildRenderingOptions)).Or("null"))
        .Map(x =>
            $"new {context.KnownTypes.LabelBuilderType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}{
                string.Join($",{Environment.NewLine}", [x.Left, x.Right])
                    .Prefix(4)
                    .WithNewlinePadding(4)
                    .WrapIfSome(Environment.NewLine)
                    .Map(x => $"({x})")
            }"
        )
        .Map(state.ConformResult(ComponentBuilderKind.IMessageComponentBuilder, options.TypingContext));
}