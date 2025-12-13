using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Discord.CX.Nodes.Components.Custom;

public sealed class FunctionalComponentNodeState : ComponentState
{
    public IReadOnlyList<ComponentChildrenAdapter.ComponentChild> Children { get; init; }
}

public class FunctionalComponentNode : ComponentNode<FunctionalComponentNodeState>, IDynamicComponentNode
{
    public override bool HasChildren => ChildrenParameter is not null;

    public override string Name => $"<functional {Method.ToDisplayString()}>";

    public override IReadOnlyList<ComponentProperty> Properties { get; }

    public IMethodSymbol Method { get; }
    public ComponentBuilderKind Kind { get; }
    public IParameterSymbol? ChildrenParameter { get; }

    private readonly ComponentChildrenAdapter? _adapter;

    private FunctionalComponentNode(
        IMethodSymbol method,
        ComponentBuilderKind kind,
        IReadOnlyList<ComponentProperty> properties,
        IParameterSymbol? childrenParameter,
        Compilation compilation
    )
    {
        Method = method;
        Kind = kind;
        ChildrenParameter = childrenParameter;
        Properties = properties;

        _adapter = childrenParameter is not null
            ? ComponentChildrenAdapter.Create(
                compilation,
                childrenParameter.Type,
                childrenParameter.HasExplicitDefaultValue,
                this
            )
            : null;
    }

    public static FunctionalComponentNode Create(IMethodSymbol method, ComponentBuilderKind kind,
        Compilation compilation)
    {
        var properties = new List<ComponentProperty>();
        IParameterSymbol? childrenParameter = null;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var parameter = method.Parameters[i];

            // TODO: should we restrict children to the last parameter only?
            if (i == method.Parameters.Length - 1)
            {
                // check for child parameter
                if (
                    parameter
                    .GetAttributes()
                    .Any(x =>
                        compilation
                            .GetKnownTypes()
                            .CXChildrenAttribute!
                            .Equals(x.AttributeClass, SymbolEqualityComparer.Default)
                    )
                )
                {
                    childrenParameter = parameter;
                }
            }

            properties.Add(new(
                parameter.Name,
                parameter.HasExplicitDefaultValue || ReferenceEquals(childrenParameter, parameter),
                renderer: Renderers.CreateRenderer(parameter.Type)
            ));
        }

        return new FunctionalComponentNode(
            method,
            kind,
            properties,
            childrenParameter,
            compilation
        );
    }

    public override FunctionalComponentNodeState? CreateState(ComponentStateInitializationContext context)
        => new()
        {
            Source = context.Node,
            Children = _adapter?.AdaptToState(context) ?? []
        };

    private string MethodReference =>
        $"{Method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{Method.Name}";

    public override Result<string> Render(
        FunctionalComponentNodeState state,
        IComponentContext context,
        ComponentRenderingOptions options
    ) => state
        .RenderProperties(this, context)
        .Combine(
            (
                _adapter?.ChildrenRenderer(context, state, state.Children)
                    .Map(x => $"{ChildrenParameter?.Name}: {x}")
            )
            .Or(string.Empty)
        )
        .Map(x =>
        {
            var args = string.Join(
                $",{Environment.NewLine}",
                ((IEnumerable<string>)[x.Left, x.Right]).Where(x => !string.IsNullOrWhiteSpace(x))
            );

            var source = $"{MethodReference}({
                args.WithNewlinePadding(4)
                    .PrefixIfSome(4)
                    .WrapIfSome(Environment.NewLine)
            })";

            var typingContext = options.TypingContext;

            if (typingContext is null)
            {
                if (state.IsRootNode)
                {
                    typingContext = context.RootTypingContext;
                }
                else
                {
                    /*
                     * TODO: unknown typing context may imply a bug where a parent component isn't supplying their
                     * required typing information
                     */

                    Debug.Fail("Unknown typing context in functional node");
                    typingContext = context.RootTypingContext;
                }
            }

            var value = ComponentBuilderKindUtils.Convert(
                source,
                Kind,
                typingContext.Value.ConformingType,
                typingContext.Value.CanSplat
            );

            if (value is null)
            {
                /*
                 * we've failed to convert, this case implies that whatever the type of this interleaved node is, it doesn't
                 * conform to the current constraints
                 */

                return Result<string>.FromDiagnostic(
                    Diagnostics.InvalidInterleavedComponentInCurrentContext(
                        Method.ReturnType.ToDisplayString(),
                        typingContext.Value.ConformingType.ToString()
                    ),
                    state.Source
                );
            }

            return value;
        });
}