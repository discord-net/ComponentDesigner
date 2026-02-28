using ComponentDesigner;

namespace Discord;

public sealed class ComponentTyping : IComponentTypingProvider
{
    private delegate Result<string> Converter(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    );

    private static readonly Dictionary<ComponentBuilderKind, Converter> _converters = new()
    {
        { ComponentBuilderKind.IMessageComponentBuilder, ConvertToIMessageComponentBuilder },
        { ComponentBuilderKind.IMessageComponent, ConvertToIMessageComponent },
        { ComponentBuilderKind.MessageComponent, ConvertToMessageComponent },
        { ComponentBuilderKind.ComponentBuilderV2, ConvertToComponentBuilderV2 },
        { ComponentBuilderKind.ModalComponent, ConvertToModalComponent },
        { ComponentBuilderKind.ModalBuilder, ConvertToModalBuilder },
        { ComponentBuilderKind.CXComponent, ConvertToCXComponent },
        { ComponentBuilderKind.CXMessageComponent, ConvertToCXMessageComponent },
        { ComponentBuilderKind.CXModalComponent, ConvertToCXModalComponent },
    };

    public bool IsValidComponentType(
        IComponentContext context,
        ICSharpTypeSymbol? symbol,
        CancellationToken cancellationToken = default
    ) => ComponentBuilderType.TryGetFromSymbol(symbol, context.CompilationProvider, cancellationToken, out _);

    public Result<string> Convert(
        IComponentContext context,
        SourcedValue<string> source,
        ICSharpTypeSymbol from,
        ICSharpTypeSymbol to,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !ComponentBuilderType.TryGetFromSymbol(
                from,
                context.CompilationProvider,
                cancellationToken,
                out var left
            )
        )
        {
            return Diagnostic
                .TypeMismatch("component", from)
                .At(source);
        }

        if (
            !ComponentBuilderType.TryGetFromSymbol(
                to,
                context.CompilationProvider,
                cancellationToken,
                out var right
            )
        )
        {
            return Diagnostic
                .TypeMismatch("component", to)
                .At(source);
        }

        if (!_converters.TryGetValue(left.Kind, out var converter))
            return Diagnostic
                .NoConversionForComponents(from, to)
                .At(source);

        return converter(
            context,
            source,
            right,
            left.IsCollection,
            cancellationToken
        );
    }

    private static Result<string> ConvertToCXModalComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.CXModalComponent, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToCXMessageComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.CXMessageComponent, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToCXComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.CXComponent, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToModalBuilder(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.ModalBuilder, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToModalComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.ModalComponent, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToComponentBuilderV2(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.ComponentBuilderV2, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToMessageComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    )
    {
        // TODO
        return Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.MessageComponent, asCollection),
                FormatName(target)
            )
            .At(source);
    }

    private static Result<string> ConvertToIMessageComponent(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    ) => target.Kind switch
    {
        ComponentBuilderKind.IMessageComponentBuilder => (asCollection, target.IsCollection) switch
        {
            (false, false) => $"{source}.Build()",
            (true, false) => $"[{source}.Build()]",
            (false, true) => (
                $"{source}.Single().Build()",
                Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
            ),
            (true, true) => $"{source}.Select(x => x.Build())"
        },
        ComponentBuilderKind.IMessageComponent => (asCollection, target.IsCollection) switch
        {
            (false, false) or (true, true) => source.Value,
            (true, false) => $"[{source}]",
            (false, true) => (
                $"{source}.Single()",
                Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
            ),
        },
        ComponentBuilderKind.MessageComponent
            or ComponentBuilderKind.ModalComponent
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => (
                    $"{source}.Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, false) => $"{source}.Components",
                (false, true) => (
                    $"{source}.Single().Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, true) => $"{source}.SelectMany(x => x.Components)"
            },
        ComponentBuilderKind.ComponentBuilderV2
            or ComponentBuilderKind.ModalBuilder
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => (
                    $"{source}.Components.Single().Build()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, false) => $"{source}.Components.Select(x => x.Build())",
                (false, true) => (
                    $"{source}.Single().Components.Single().Build()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, true) => $"{source}.SelectMany(x => x.Components.Select(x => x.Build()))"
            },
        ComponentBuilderKind.CXComponent
            or ComponentBuilderKind.CXMessageComponent
            or ComponentBuilderKind.CXModalComponent
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => (
                    $"{source}.Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, false) => $"{source}.Components",
                (false, true) => (
                    $"{source}.Single().Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
                ),
                (true, true) => $"{source}.SelectMany(x => x.Components)"
            },
        _ => Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.IMessageComponent, asCollection),
                FormatName(target)
            )
            .At(source)
    };

    private static Result<string> ConvertToIMessageComponentBuilder(
        IComponentContext context,
        SourcedValue<string> source,
        ComponentBuilderType target,
        bool asCollection,
        CancellationToken cancellationToken
    ) => target.Kind switch
    {
        ComponentBuilderKind.IMessageComponentBuilder => (asCollection, target.IsCollection) switch
        {
            (true, true) => $"..{source}",
            (false, _) => source.Value,
            (true, false) => (
                $"{source}.Single()",
                Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)
            )
        },
        ComponentBuilderKind.IMessageComponent => (asCollection, target.IsCollection) switch
        {
            (false, false) => $"{source}.ToBuilder()",
            (true, false) => $"[{source}.ToBuilder()]",
            (false, true) => ($"{source}.Single().ToBuilder()",
                Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
            (true, true) => $"[..{source}.Select(x => x.ToBuilder())]"
        },
        ComponentBuilderKind.MessageComponent or ComponentBuilderKind.ModalComponent
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => ($"{source}.Components.Single().ToBuilder()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, false) => $"{source}.Components.Select(x => x.ToBuilder())",
                (false, true) => ($"{source}.Single().Components.Single().ToBuilder()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, true) => $"{source}.SelectMany(x => x.Components.Select(x => x.ToBuilder()))"
            },
        ComponentBuilderKind.ModalBuilder or ComponentBuilderKind.ComponentBuilderV2
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => ($"{source}.Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, false) => $"{source}.Components",
                (false, true) => ($"{source}.Single().Components.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, true) => $"{source}.SelectMany(x => x.Components)"
            },
        ComponentBuilderKind.CXComponent
            or ComponentBuilderKind.CXMessageComponent
            or ComponentBuilderKind.CXModalComponent
            => (asCollection, target.IsCollection) switch
            {
                (false, false) => ($"{source}.Builders.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, false) => $"{source}.Builders",
                (false, true) => ($"{source}.Single().Builders.Single()",
                    Diagnostic.UsingRuntimeValidation("IEnumerable.Single()").At(source)),
                (true, true) => $"{source}.SelectMany(x => x.Builders)"
            },
        _ => Diagnostic
            .NoConversionForComponents(
                FormatName(ComponentBuilderKind.IMessageComponentBuilder, asCollection),
                FormatName(target)
            )
            .At(source)
    };

    private static string FormatName(
        ComponentBuilderType type
    ) => FormatName(type.Kind, type.IsCollection);

    private static string FormatName(
        ComponentBuilderKind kind,
        bool isCollection
    ) => isCollection ? $"{kind}[]" : kind.ToString();
}