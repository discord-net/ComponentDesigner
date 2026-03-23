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

        for (var i = 0; i < state.Properties.Count; i++)
        {
            var parameter = state.Properties[i];
            var parameterSymbol = state.Symbol.Parameters[i];

            var parameterValue = state.GetPropertyValue(parameter);

            // if (parameterValue.IsNone)
            // {
            //     if (!parameter.IsOptional)
            //     {
            //         bag.Add(
            //             state.ElementIdentifierTextSpanOrBetter.Report(
            //                 Diagnostic.RequiredPropertyNotSpecified(functionalComponent, parameter)
            //             )
            //         );
            //     }
            //
            //     continue;
            // }

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
            var isCollection = false;
            var innerSymbol = typeSymbol;

            if (
                !typeSymbol.Equals(context.CompilationProvider.String!) &&
                typeSymbol.TryGetEnumerableType(out var inner)
            )
            {
                isCollection = true;
                innerSymbol = inner;
            }

            using var _ = StringBuilder.Pooled(out var sb);
            using var bag = PooledDiagnosticBag.Get();
            var valueCount = 0;
            var componentOptions = new ComponentOptions(
                new RendererTypingContext(typeSymbol)
            );

            foreach (var value in propertyValue.AsFlattened)
            {
                switch (value)
                {
                    case ComponentPropertyValue.Component component:
                        Append(context
                            .RenderGraphNode(
                                component.GraphNode,
                                componentOptions,
                                cancellationToken
                            )
                            .AsSource
                            .Unwrap(bag)
                        );
                        break;
                    case ComponentPropertyValue.Literal
                        or ComponentPropertyValue.Interpolation
                        or ComponentPropertyValue.None:
                        Append(
                            GetGeneratorForSymbol(context.CompilationProvider, innerSymbol)
                                .Render(context, value, cancellationToken)
                                .Unwrap(bag)
                        );
                        break;
                }
            }

            if (isCollection)
            {
                if (valueCount is 0)
                {
                    if (propertyValue.Property.IsOptional)
                        return ($"default", bag.ToCollection());
                    
                    return ("[]", bag.ToCollection());
                }

                return (
                    $"""

                     [
                         {sb.ToString().WithNewlinePadding(4)}
                     ]
                     """,
                    bag.ToCollection()
                );
            }

            switch (valueCount)
            {
                case 0:
                    if (propertyValue.Property.IsOptional)
                        return ("default", bag.ToCollection());
                
                    return Result<string>.FromDiagnostics([
                        Diagnostic
                            .RequiredPropertyNotSpecified(functionalComponent, propertyValue.Property)
                            .At(state.ElementIdentifierTextSpanOrBetter),
                        ..bag.ToCollection()
                    ]);
                case 1:
                    return (sb.ToString(), bag.ToCollection());
                default:
                    return Result<string>.FromDiagnostics([
                        Diagnostic
                            .TooManyPropertyValues(propertyValue.Property)
                            .At(propertyValue),
                        ..bag.ToCollection()
                    ]);
            }

            void Append(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                valueCount++;

                if (sb.Length > 0)
                    sb.AppendLine(",");

                sb.Append(value);
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