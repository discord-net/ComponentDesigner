global using CSharpValueTransformer =
    ComponentDesigner.Nodes.ComponentPropertyValueTransformer<
        ComponentDesigner.IRenderContext<ComponentDesigner.CSharp.CSharpRender>,
        ComponentDesigner.CSharp.CSharpRender
    >;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ComponentDesigner;
using ComponentDesigner.CSharp;
using ComponentDesigner.Nodes;
using ComponentDesigner.Util;

namespace Discord;

partial class DiscordNetRenderer
{
    private delegate Result<CSharpRender> Converter(
        IRenderContext<CSharpRender> context,
        CSharpRender render,
        ICSharpTypeSymbol target,
        CancellationToken cancellationToken
    );

    private delegate Result<CSharpRender> ConverterPipeline(
        IRenderContext<CSharpRender> context,
        CSharpRender render,
        ICSharpTypeSymbol target,
        Converter next,
        CancellationToken cancellationToken
    );

    private readonly struct PropertySpec
    {
        public string Name { get; }
        public ComponentProperty? Property { get; }
        private readonly object _valueProvider;

        private PropertySpec(string name, ComponentProperty? property, string valueProvider)
        {
            _valueProvider = valueProvider;
            Name = name;
            Property = property;
        }

        private PropertySpec(string name, ComponentProperty? property, CSharpValueGenerator valueProvider)
        {
            _valueProvider = valueProvider;
            Name = name;
            Property = property;
        }

        private PropertySpec(
            string name,
            ComponentProperty? property,
            CSharpValueTransformer valueProvider
        )
        {
            _valueProvider = valueProvider;
            Name = name;
            Property = property;
        }

        public bool TryGetConstantValue([MaybeNullWhen(false)] out string constant)
            => (constant = _valueProvider as string) is not null;

        public Result<CSharpRender> GetValue(
            IRenderContext<CSharpRender> context,
            ComponentPropertyValue propertyValue,
            CancellationToken cancellationToken
        ) => _valueProvider switch
        {
            CSharpValueGenerator valueGenerator => valueGenerator.Render(
                context,
                propertyValue,
                cancellationToken
            ),
            CSharpValueTransformer propertyRenderer
                => propertyRenderer(
                    context,
                    propertyValue,
                    cancellationToken
                ),
            string str => new CSharpRender(
                propertyValue.TextSpan,
                str,
                context.CompilationProvider.String(CXTextSpan.Empty, cancellationToken).GetValueOrDefault()
            ),
            _ => throw new InvalidOperationException($"Unknown value provider {_valueProvider.GetType()}")
        };

        public static implicit operator PropertySpec(
            (string, string) tuple
        ) => new(tuple.Item1, null, tuple.Item2);

        public static implicit operator PropertySpec(
            (string, ComponentProperty, CSharpValueGenerator) tuple
        ) => new(tuple.Item1, tuple.Item2, tuple.Item3);

        public static implicit operator PropertySpec(
            (string, ComponentProperty, ComponentPropertyValueTransformer<IRenderContext, CSharpRender>) tuple
        ) => new(tuple.Item1, tuple.Item2, tuple.Item3);

        public static implicit operator PropertySpec(
            (string, ComponentProperty, CSharpValueTransformer)
                tuple
        ) => new(
            tuple.Item1,
            tuple.Item2,
            tuple.Item3
        );
    }


    private static Result<CSharpRender> Construct<T>(
        IRenderContext<CSharpRender> context,
        T state,
        Symbols.Fetch<T> symbolFactory,
        CancellationToken cancellationToken,
        params ReadOnlySpan<PropertySpec> properties
    ) where T : ComponentState
    {
        using var bag = PooledDiagnosticBag.Get();

        var symbol = symbolFactory(state, cancellationToken).Unwrap(bag);

        if (symbol is null) return Result<CSharpRender>.FromDiagnostics(bag.ToCollection());

        using var _ = StringBuilder.Pooled(out var sb);

        for (var i = 0; i < properties.Length; i++)
        {
            var propertySpec = properties[i];

            if (propertySpec.Property is null)
            {
                if (!propertySpec.TryGetConstantValue(out var constant))
                {
                    throw new InvalidOperationException("Expected a constant value for synthetic property");
                }

                AddParameter(propertySpec.Name, constant);
                continue;
            }

            var propertyValue = state.GetPropertyValue(propertySpec.Property);

            if (ShouldOmit(propertyValue)) continue;

            var parameterValue = propertySpec.GetValue(
                context,
                propertyValue,
                cancellationToken
            ).Unwrap(bag);

            if (!parameterValue.IsEmpty) AddParameter(propertySpec.Name, parameterValue.Source);
        }

        if (bag.HasErrors) return new(bag.ToCollection());

        var parametersString = sb.Length is 0
            ? string.Empty
            : $"{Environment.NewLine}    {sb.ToString().WithNewlinePadding(4)}{Environment.NewLine}";

        return ApplyRefParameter(
                context,
                new(
                    state.TextSpan,
                    $"new {symbol.ToQualifiedName()}({parametersString})",
                    symbol
                ),
                state,
                cancellationToken
            )
            .PrefaceDiagnostics(bag.ToCollection());

        static Result<CSharpRender> ApplyRefParameter(
            IRenderContext context,
            CSharpRender render,
            ComponentState state,
            CancellationToken cancellationToken
        )
        {
            if (!state.PropertyInfo.TryGet("ref", out var refProperty))
                return render;

            var propertyValue = state.GetPropertyValue(refProperty);

            if (propertyValue.AsSingle is not ComponentPropertyValue.Interpolation interpolation)
                return Diagnostic
                    .InvalidPropertyValue(propertyValue, ComponentPropertyValueKind.Interpolation)
                    .At(propertyValue);

            return context
                .CompilationProvider
                .RefBox(propertyValue, cancellationToken)
                .Map(Result<CSharpRender> (refBoxSymbol) =>
                {
                    if (
                        interpolation.Info.Symbol?.ConstructedFrom is null ||
                        !refBoxSymbol.Equals(interpolation.Info.Symbol.ConstructedFrom)
                    )
                    {
                        return Diagnostic
                            .TypeMismatch(
                                refBoxSymbol,
                                interpolation.Info.Symbol
                            )
                            .At(propertyValue);
                    }

                    if (render.Symbol is not null)
                    {
                        var inner = interpolation.Info.Symbol.TypeArguments[0];
                        if (!context.CompilationProvider.HasImplicitConversionBetween(render.Symbol, inner))
                        {
                            return Diagnostic
                                .TypeMismatch(
                                    render.Symbol,
                                    inner
                                )
                                .At(propertyValue);
                        }
                    }

                    return render with
                    {
                        Source =
                        $"""
                         {context.GetReferenceToDesignerValue(interpolation.Info, interpolation.Info.Symbol)}.Set(
                             {render.Source.WithNewlinePadding(4)}
                         )
                         """
                    };
                });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddParameter(string name, string value)
        {
            if (sb.Length > 0)
                sb.AppendLine(",");

            sb.Append(name).Append(": ").Append(value);
        }

        static bool ShouldOmit(ComponentPropertyValue propertyValue)
            => propertyValue.Property.IsSynthetic ||
               propertyValue is { Property.IsOptional: true, IsNone: true, IsAttributeNameOnly: false };
    }

    private static CSharpValueTransformer Single(StaticTypeSymbolFactory<CXTextSpan> symbolFactory)
        => (context, value, cancellationToken) =>
        {
            var symbol = symbolFactory(context.CompilationProvider, value.TextSpan, cancellationToken);

            if (!symbol.HasValue) return new Result<CSharpRender>(symbol.Diagnostics);

            return RenderSingleValue(context, value, cancellationToken)
                .Map(render => Convert(context, render, symbol.Value, cancellationToken));
        };

    private static CSharpValueTransformer CollectionOf(
        StaticTypeSymbolFactory<CXTextSpan> symbolFactory,
        ConverterPipeline? converter = null
    )
    {
        return (context, value, cancellationToken) =>
        {
            var symbol = symbolFactory(context.CompilationProvider, value.TextSpan, cancellationToken);

            if (!symbol.HasValue) return new Result<CSharpRender>(symbol.Diagnostics);

            return BuildCollectionExpression(
                context,
                symbol.Value,
                value,
                converter,
                cancellationToken
            );
        };

        static Result<CSharpRender> BuildCollectionExpression(
            IRenderContext<CSharpRender> context,
            ICSharpTypeSymbol targetSymbol,
            ComponentPropertyValue propertyValue,
            ConverterPipeline? converter,
            CancellationToken cancellationToken
        )
        {
            using var _ = StringBuilder.Pooled(out var sb);
            using var result = Result<CSharpRender>.Builder;

            foreach (var innerValue in propertyValue.AsFlattened)
            {
                var element = RenderSingleValue(context, innerValue, cancellationToken)
                    .Map(render =>
                        converter?.Invoke(
                            context,
                            render,
                            targetSymbol,
                            Convert,
                            cancellationToken
                        ) ?? Convert(
                            context,
                            render,
                            targetSymbol,
                            cancellationToken
                        )
                    )
                    .Unwrap(result);

                if (element.IsEmpty)
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine(",");

                sb.Append(element.Source);
            }

            if (sb.Length is 0)
                return result.WithValue(new(propertyValue.TextSpan, "[]")).Build();

            return result
                .WithValue(new(
                    propertyValue.TextSpan,
                    $"""

                     [
                         {sb.ToString().WithNewlinePadding(4)}
                     ]
                     """
                ))
                .Build();
        }

        static Result<CSharpRender> Convert(
            IRenderContext<CSharpRender> context,
            CSharpRender render,
            ICSharpTypeSymbol target,
            CancellationToken cancellationToken
        )
        {
            if (
                render.Symbol is null ||
                context.CompilationProvider.HasImplicitConversionBetween(
                    render.Symbol,
                    target
                )
            ) return render;

            if (
                render.Symbol.TryGetEnumerableType(out var inner)
            )
            {
                if (
                    !context.CompilationProvider.HasImplicitConversionBetween(
                        inner,
                        target
                    )
                )
                {
                    return Diagnostic
                        .TypeMismatch(
                            target,
                            render.Symbol
                        )
                        .At(render);
                }

                return render with
                {
                    Source = $"..{render.Source}"
                };
            }

            return DiscordNetRenderer.Convert(
                context,
                render,
                target,
                cancellationToken
            );
        }
    }

    private static Result<CSharpRender> RenderSingleValue(
        IRenderContext<CSharpRender> context,
        ComponentPropertyValue value,
        CancellationToken cancellationToken
    ) => value.AsSingle switch
    {
        ComponentPropertyValue.Component component
            => component.GraphNode.Render(context, cancellationToken),

        ComponentPropertyValue.Interpolation interpolation
            => new CSharpRender(
                interpolation.TextSpan,
                context.GetReferenceToDesignerValue(interpolation.Info, interpolation.Info.Symbol),
                interpolation.Info.Symbol
            ),

        _ => Diagnostic
            .InvalidPropertyValue(
                value,
                ComponentPropertyValueKind.Component | ComponentPropertyValueKind.Interpolation
            )
            .At(value)
    };

    private static readonly CSharpValueTransformer CollectionOfIMessageComponentBuilders
        = CollectionOf(Symbols.IMessageComponentBuilder);

    private static readonly CSharpValueTransformer IMessageComponentBuilder
        = Single(Symbols.IMessageComponentBuilder);
}