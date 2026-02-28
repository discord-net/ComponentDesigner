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

            if (parameterValue is { HasValue: false, RequiresValue: true })
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

            var result = BuildPropertyValue(parameterSymbol, parameterSymbol.Type, parameterValue);
            
            bag.Add(result.Diagnostics);
            
            if(result.HasValue) AppendParameter(parameters, parameter.Name, result.Value);
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
            ICSharpParameterSymbol parameterSymbol,
            ICSharpTypeSymbol typeSymbol,
            ComponentPropertyValue propertyValue
        )
        {
            var property = propertyValue.Property;

            switch (propertyValue)
            {
                case ComponentPropertyValue.AttributeComponent attributeElement:
                    return context
                        .RenderGraphNode(
                            attributeElement.GraphNode,
                            new(new(typeSymbol)),
                            cancellationToken
                        )
                        .Map(x => x.Source);
                case ComponentPropertyValue.SyntaxValue:
                case ComponentPropertyValue.Missing:
                case ComponentPropertyValue.AttributeValue:
                    return GetGeneratorForSymbol(
                        context.CompilationProvider,
                        typeSymbol
                    ).Render(context, propertyValue, cancellationToken: cancellationToken);

                case ComponentPropertyValue.Component child:
                    return context.RenderGraphNode(
                        child.GraphNode,
                        new(
                            TypingContext: new(typeSymbol)
                        ),
                        cancellationToken
                    ).Map(x => x.Source);

                case ComponentPropertyValue.Many many:
                {
                    if (
                        typeSymbol.Equals(context.CompilationProvider.String) ||
                        !typeSymbol.TryGetEnumerableType(out var inner)
                    )
                    {
                        if (many.Values.Count <= 1)
                        {
                            return BuildPropertyValue(
                                parameterSymbol,
                                typeSymbol,
                                many.Values.FirstOrDefault() ??
                                new ComponentPropertyValue.Missing(property, many.TextSpan)
                            );
                        }

                        // more than one value, cardinality doesn't match
                        return Diagnostic
                            .OnlyOneChildAllowed(functionalComponent)
                            .At(many);
                    }

                    return many
                        .Values
                        .Select(x => BuildPropertyValue(parameterSymbol, inner, x))
                        .FlattenAll()
                        .Map(x =>
                        {
                            using var _ = StringBuilder.Pooled(out var sb);

                            // start on a new line
                            sb.AppendLine();
                            sb.AppendLine("[");

                            for (var i = 0; i < x.Count; i++)
                            {
                                if (i > 0) sb.AppendLine(",");

                                sb.Append("    ").Append(x[i].WithNewlinePadding(4));
                            }

                            if (x.Count > 0) sb.AppendLine();
                            sb.Append(']');

                            return sb.ToString();
                        });
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(propertyValue));
            }
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