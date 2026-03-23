using System.Text;
using ComponentDesigner.Nodes;
using ComponentDesigner.Parser;
using ComponentDesigner.Util;

namespace ComponentDesigner.CSharp;

partial class BaseCSharpRenderer
{
    public virtual Result<RenderedComponent> RenderFunctionalComponent(
        IRendererContext context,
        FunctionalComponentNode functionalComponent,
        FunctionalState state,
        RendererTypingContext? typingContext = null,
        CancellationToken cancellationToken = default
    )
    {
        var bag = PooledDiagnosticBag.Get();

        using var _ = StringBuilder.Pooled(out var parameters);

        for (var i = 0; i < state.Parameters.Count; i++)
        {
            var parameter = state.Parameters[i];
            var parameterSymbol = state.Symbol.Parameters[i];

            var parameterValue = state.GetPropertyValue(parameter);

            if (parameterValue.IsNone)
            {
                if (!parameter.IsOptional)
                {
                    bag.Add(
                        state.ElementIdentifierTextSpanOrBetter.Report(
                            Diagnostic.RequiredPropertyNotSpecified(functionalComponent, parameter)
                        )
                    );
                }

                continue;
            }

            var result = BuildPropertyValue(parameterSymbol.Type, parameterValue);

            bag.Add(result.Diagnostics);

            if (result.HasValue) AppendParameter(parameters, parameter.Name, result.Value);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        if (parameters.Length > 0)
        {
            parameters.Insert(0, Environment.NewLine).AppendLine();
        }

        return new(
            $"{MakeMethodReference(state.CXNode, context, state.Symbol)}({parameters})"
        );

        Result<string> BuildPropertyValue(
            ICSharpTypeSymbol typeSymbol,
            ComponentPropertyValue propertyValue
        )
        {
            var property = propertyValue.Property;

            switch (propertyValue)
            {
                case ComponentPropertyValue.Literal
                    or ComponentPropertyValue.Interpolation
                    or ComponentPropertyValue.None
                    when (property.Kind & ComponentPropertyValueKind.SyntaxValue) > 0:
                    return GetGeneratorForSymbol(
                            context.CompilationProvider,
                            typeSymbol
                        )
                        .Render(
                            context,
                            propertyValue,
                            cancellationToken
                        );
                case ComponentPropertyValue.Component { GraphNode: var graphNode }
                    when property.Kind.HasFlag(ComponentPropertyValueKind.Component):
                    return context
                        .RenderGraphNode(
                            graphNode,
                            new(new(typeSymbol)),
                            cancellationToken
                        )
                        .Map(x => x.Source);

                case ComponentPropertyValue.Many many
                    when property.Kind.HasFlag(ComponentPropertyValueKind.Many):
                {
                    if (many.AsSingle is { } single)
                    {
                        return BuildPropertyValue(typeSymbol, single);
                    }

                    var innerKind = many.Kind ^ ComponentPropertyValueKind.Many;
                    var allowed = innerKind & property.Kind;

                    ICSharpTypeSymbol? innerSymbol = null;

                    var isEnumerable = !typeSymbol.Equals(context.CompilationProvider.String!) &&
                                       typeSymbol.TryGetEnumerableType(out innerSymbol);

                    innerSymbol ??= typeSymbol;

                    if ((allowed & ComponentPropertyValueKind.SingleSyntaxValue) == allowed)
                    {
                        if (!isEnumerable)
                        {
                            return GetGeneratorForSymbol(
                                    context.CompilationProvider,
                                    typeSymbol
                                )
                                .Render(
                                    context,
                                    propertyValue,
                                    cancellationToken
                                );
                        }

                        using var resultBuilder = Result<string>.Builder;
                        using var _ = StringBuilder.Pooled(out var sb);

                        foreach (var innerValue in many.Values)
                        {
                            if (
                                innerValue.Kind is ComponentPropertyValueKind.None ||
                                (innerValue.Kind & ComponentPropertyValueKind.SingleSyntaxValue) == innerValue.Kind
                            )
                            {
                                if (
                                    GetGeneratorForSymbol(
                                        context.CompilationProvider,
                                        innerSymbol
                                    )
                                    .Render(
                                        context,
                                        propertyValue,
                                        cancellationToken
                                    )
                                    .TryUnwrap(resultBuilder, out var source)
                                )
                                {
                                    if (sb.Length > 0) sb.AppendLine(",");

                                    sb.Append(source);
                                }

                                continue;
                            }

                            resultBuilder.AddDiagnostic(
                                Diagnostic
                                    .InvalidPropertyValue(
                                        innerValue,
                                        ComponentPropertyValueKind.SingleSyntaxValue
                                    )
                                    .At(innerValue)
                            );
                        }

                        if (sb.Length is 0) return resultBuilder.WithValue("[]").Build();

                        return resultBuilder
                            .WithValue(
                                $"""
                                 [
                                     {sb.ToString().WithNewlinePadding(4)}
                                 ]
                                 """
                            )
                            .Build();
                    }

                    if (allowed is ComponentPropertyValueKind.Component)
                    {
                        return BuildManyComponents(many, typeSymbol);
                    }

                    // bad configuration
                    return Diagnostic
                        .InvalidPropertyValue(
                            propertyValue,
                            allowed
                        )
                        .At(propertyValue);
                }

                default:
                    return Diagnostic
                        .InvalidPropertyValue(propertyValue)
                        .At(propertyValue);
            }
        }

        Result<string> BuildManyComponents(ComponentPropertyValue.Many many, ICSharpTypeSymbol symbol)
        {
            using var resultBuilder = Result<string>.Builder;
            using var _ = StringBuilder.Pooled(out var sb);

            foreach (var innerValue in many.Values)
            {
                // should always be a component
                if (innerValue is not ComponentPropertyValue.Component { GraphNode: var graphNode })
                    throw new InvalidOperationException(
                        "Parity between Many.Kind does not match its values"
                    );

                if (sb.Length > 0)
                    sb.AppendLine(",");
                if (
                    context
                    .RenderGraphNode(
                        graphNode,
                        new(new(symbol)),
                        cancellationToken
                    )
                    .TryUnwrap(resultBuilder, out var renderedComponent)
                )
                {
                    sb.Append(renderedComponent.Source);
                }
            }

            if (sb.Length is 0)
                return resultBuilder.WithValue("[]").Build();

            return resultBuilder
                .WithValue(
                    $"""
                     [
                         {sb.ToString().WithNewlinePadding(4)}
                     ]
                     """
                )
                .Build();
        }

        static void AppendParameter(StringBuilder builder, string name, string value)
        {
            if (builder.Length > 0) builder.AppendLine(",");
            builder.Append("    ").Append(name).Append(": ").Append(value.WithNewlinePadding(4));
        }

        static string MakeMethodReference(CXElement element, IComponentContext context, ICSharpMethodSymbol symbol)
        {
            switch (element.OpeningTag.Identifier)
            {
                case CXIdentifier.Simple:
                    return $"{symbol.ContainingType.ToQualifiedName()}.{symbol.Name}";
                case CXIdentifier.Interpolated { InterpolationToken: { } token }:
                    var info = context.GetInterpolationInfo(token);

                    return $"{context.GetReferenceToDesignerValue(info, info.Symbol)}.{symbol.Name}";

                default: throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }
}