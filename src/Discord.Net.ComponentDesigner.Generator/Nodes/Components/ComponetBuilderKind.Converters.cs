using System;

namespace Discord.CX.Nodes.Components;

public static class ComponentBuilderKindConverters
{
    public readonly record struct Options(
        bool AllowSpread,
        bool AllowConversionFromMessageToModalComponents,
        bool AllowConversionFromModalToMessageComponents
    );

    public delegate Result<string> Converter(LocalSource source, ComponentBuilderKind target, Options options);

    private const string CXMessageComponentRef = "global::Discord.CXMessageComponent";
    private const string CXModalComponentRef = "global::Discord.CXModalComponent";
    private const string CXComponentRef = "global::Discord.CXComponent";
    private const string ComponentBuilderV2Ref = "global::Discord.ComponentBuilderV2";
    private const string ModalBuilderRef = "global::Discord.ModalBuilder";

    public static Result<string> Convert(
        LocalSource source,
        ComponentBuilderKind from,
        ComponentBuilderKind to,
        bool allowSpreads = false,
        bool allowConversionFromMessageToModalComponents = false,
        bool allowConversionFromModalToMessageComponents = false
    )
    {
        Converter converter = from.TypeOnly switch
        {
            ComponentBuilderKind.IMessageComponentBuilder => ConvertIMessageComponentBuilder,
            ComponentBuilderKind.IMessageComponent => ConvertIMessageComponent,
            ComponentBuilderKind.CXMessageComponent => ConvertCXMessageComponent,
            ComponentBuilderKind.CXModalComponent => ConvertCXModalComponent,
            ComponentBuilderKind.CXComponent => ConvertCXComponent,
            ComponentBuilderKind.MessageComponent => ConvertMessageComponent,
            ComponentBuilderKind.ModalComponent => ConvertModalComponent,
            ComponentBuilderKind.ComponentBuilderV2 => ConvertComponentBuilderV2,
            ComponentBuilderKind.ModalBuilder => ConvertModalBuilder,
            _ => throw new ArgumentOutOfRangeException(nameof(from), from, null)
        };

        var options = new Options(
            allowSpreads,
            allowConversionFromMessageToModalComponents,
            allowConversionFromModalToMessageComponents
        );

        if (!from.IsCollection)
        {
            return converter(
                source,
                to,
                options
            );
        }

        return from switch
        {
            _ when to.IsCollection => converter(
                    source with { Value = "x" },
                    to,
                    options
                )
                .Map(x => $"{source}.SelectMany(x => {x})"),

            _ => ConvertIMessageComponent(
                    source with {Value = "x"},
                    to,
                    options with { AllowSpread = true }
                )
                .Map(x => $"{source}.Select(x => {x})")
        };
    }

    private static Result<string> ConvertModalBuilder(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent => $"new {CXMessageComponentRef}({source}.Components)",
        ComponentBuilderKind.CXModalComponent => $"new {CXModalComponentRef}({source}.Components)",
        ComponentBuilderKind.ComponentBuilderV2
            when options.AllowConversionFromModalToMessageComponents
            => $"new {ComponentBuilderV2Ref}({source}.Components)",
        ComponentBuilderKind.ModalBuilder => source.Value,

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Components",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Build().Components",
        
        _ when target.IsCollection =>
            ConvertModalBuilder(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.ModalBuilder),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertComponentBuilderV2(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent => $"new {CXMessageComponentRef}({source}.Components)",
        ComponentBuilderKind.CXModalComponent
            when options.AllowConversionFromMessageToModalComponents
            => $"new {CXModalComponentRef}({source}.Components)",
        ComponentBuilderKind.ComponentBuilderV2 => source.Value,
        ComponentBuilderKind.ModalBuilder
            when options.AllowConversionFromMessageToModalComponents
            => $"new {ModalBuilderRef}({source}.Components)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Components",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Build().Components",


        _ when target.IsCollection =>
            ConvertComponentBuilderV2(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.ComponentBuilderV2),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertModalComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread =>
            $"..{source}.Components.Select(x => x.ToBuilder())",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent
            when options.AllowConversionFromModalToMessageComponents
            => $"new {CXMessageComponentRef}({source}.Components)",
        ComponentBuilderKind.CXModalComponent => $"{CXModalComponentRef}.From({source})",
        ComponentBuilderKind.CXComponent => $"new {CXComponentRef}({source}.Components)",
        ComponentBuilderKind.MessageComponent
            when options.AllowConversionFromModalToMessageComponents
            => $"new {ComponentBuilderV2Ref}({source}.Components).Build()",
        ComponentBuilderKind.ModalComponent => source.Value,
        ComponentBuilderKind.ComponentBuilderV2
            when options.AllowConversionFromModalToMessageComponents
            => $"new {ComponentBuilderV2Ref}({source}.Components)",
        ComponentBuilderKind.ModalBuilder => $"new {ModalBuilderRef}({source}.Components)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Components.Select(x => x.ToBuilder())",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Components",

        _ when target.IsCollection =>
            ConvertModalComponent(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.ModalComponent),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertMessageComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread =>
            $"..{source}.Components.Select(x => x.ToBuilder())",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent => $"{CXMessageComponentRef}.From({source})",
        ComponentBuilderKind.CXModalComponent
            when options.AllowConversionFromMessageToModalComponents
            => $"new {CXModalComponentRef}({source}.Components)",
        ComponentBuilderKind.CXComponent => $"new {CXComponentRef}({source}.Components)",
        ComponentBuilderKind.MessageComponent => source.Value,
        ComponentBuilderKind.ModalComponent
            when options.AllowConversionFromMessageToModalComponents
            => $"new {ModalBuilderRef}({source}.Components).Build()",
        ComponentBuilderKind.ComponentBuilderV2 => $"new {ComponentBuilderV2Ref}({source}.Components)",
        ComponentBuilderKind.ModalBuilder
            when options.AllowConversionFromMessageToModalComponents
            => $"new {ModalBuilderRef}({source}.Components)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Components.Select(x => x.ToBuilder())",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Components",

        _ when target.IsCollection =>
            ConvertMessageComponent(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.MessageComponent),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertCXComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread => $"..{source}.Builders",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent => $"new {CXMessageComponentRef}({source}.Builders)",
        ComponentBuilderKind.CXModalComponent => $"new {CXModalComponentRef}({source}.Builders)",
        ComponentBuilderKind.CXComponent => source.Value,
        ComponentBuilderKind.MessageComponent => $"new {ComponentBuilderV2Ref}({source}.Builders).Build()",
        ComponentBuilderKind.ModalComponent => $"new {ModalBuilderRef}({source}.Builders).Build()",
        ComponentBuilderKind.ComponentBuilderV2 => $"new {ComponentBuilderV2Ref}({source}.Builders)",
        ComponentBuilderKind.ModalBuilder => $"new {ModalBuilderRef}({source}.Builders)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Builders",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Components",

        _ when target.IsCollection =>
            ConvertCXComponent(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.CXComponent),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertCXModalComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread => $"..{source}.Builders",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent
            when options.AllowConversionFromModalToMessageComponents
            => $"new {CXMessageComponentRef}({source}.Builders)",
        ComponentBuilderKind.CXModalComponent => source.Value,
        ComponentBuilderKind.CXComponent => source.Value,
        ComponentBuilderKind.MessageComponent
            when options.AllowConversionFromModalToMessageComponents
            => $"new {ComponentBuilderV2Ref}({source}.Builders).Build()",
        ComponentBuilderKind.ModalComponent => $"{source}.Build()",
        ComponentBuilderKind.ComponentBuilderV2
            when options.AllowConversionFromModalToMessageComponents
            => $"new {ComponentBuilderV2Ref}({source}.Builders)",
        ComponentBuilderKind.ModalBuilder => $"new {ModalBuilderRef}({source}.Builders)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Builders",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Components",

        _ when target.IsCollection =>
            ConvertCXModalComponent(source, target.TypeOnly, options with { AllowSpread = true })
                .Map(x => $"[{x}]"),

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.CXModalComponent),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertCXMessageComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder when options.AllowSpread => $"..{source}.Builders",
        ComponentBuilderKind.IMessageComponent when options.AllowSpread => $"..{source}.Components",
        ComponentBuilderKind.CXMessageComponent => source.Value,
        ComponentBuilderKind.CXModalComponent
            when options.AllowConversionFromMessageToModalComponents
            => $"new {CXModalComponentRef}({source}.Builders)",
        ComponentBuilderKind.CXComponent => source.Value,
        ComponentBuilderKind.MessageComponent => $"{source}.Build()",
        ComponentBuilderKind.ModalComponent
            when options.AllowConversionFromMessageToModalComponents
            => $"new {ModalBuilderRef}({source}.Builders).Build()",
        ComponentBuilderKind.ComponentBuilderV2 => $"new {ComponentBuilderV2Ref}({source}.Builders)",
        ComponentBuilderKind.ModalBuilder
            when options.AllowConversionFromMessageToModalComponents
            => $"new {ModalBuilderRef}({source}.Builders)",

        ComponentBuilderKind.CollectionOfIMessageComponentBuilders => $"{source}.Builders",
        ComponentBuilderKind.CollectionOfIMessageComponents => $"{source}.Components",

        _ => target.IsCollection
            ? ConvertCXMessageComponent(source, target.TypeOnly, options with { AllowSpread = true }).Map(x => $"[{x}]")
            : Result<string>.FromDiagnostic(
                Diagnostics.InvalidInterleavedComponentInCurrentContext(
                    nameof(ComponentBuilderKind.CXMessageComponent),
                    target.ToString()
                ),
                source
            )
    };

    private static Result<string> ConvertIMessageComponentBuilder(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder => source.Value,
        ComponentBuilderKind.IMessageComponent => $"{source}.Build()",
        ComponentBuilderKind.CXMessageComponent => $"new {CXMessageComponentRef}({source})",
        ComponentBuilderKind.CXModalComponent => $"new {CXModalComponentRef}({source})",
        ComponentBuilderKind.CXComponent => $"new {CXComponentRef}({source})",
        ComponentBuilderKind.MessageComponent => $"new {ComponentBuilderV2Ref}({source}).Build()",
        ComponentBuilderKind.ModalComponent => $"new {ModalBuilderRef}({source}).Build()",
        ComponentBuilderKind.ComponentBuilderV2 => $"new {ComponentBuilderV2Ref}({source})",
        ComponentBuilderKind.ModalBuilder => $"new {ModalBuilderRef}({source})",

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.IMessageComponentBuilder),
                target.ToString()
            ),
            source
        )
    };

    private static Result<string> ConvertIMessageComponent(
        LocalSource source,
        ComponentBuilderKind target,
        Options options
    ) => target switch
    {
        ComponentBuilderKind.IMessageComponentBuilder => $"{source}.ToBuilder()",
        ComponentBuilderKind.IMessageComponent => source.Value,
        ComponentBuilderKind.CXMessageComponent => $"new {CXMessageComponentRef}({source})",
        ComponentBuilderKind.CXModalComponent => $"new {CXModalComponentRef}({source})",
        ComponentBuilderKind.CXComponent => $"new {CXComponentRef}({source})",
        ComponentBuilderKind.MessageComponent => $"new {ComponentBuilderV2Ref}({source}).Build()",
        ComponentBuilderKind.ModalComponent => $"new {ModalBuilderRef}({source}).Build()",
        ComponentBuilderKind.ComponentBuilderV2 => $"new {ComponentBuilderV2Ref}({source})",
        ComponentBuilderKind.ModalBuilder => $"new {ModalBuilderRef}({source})",

        _ => Result<string>.FromDiagnostic(
            Diagnostics.InvalidInterleavedComponentInCurrentContext(
                nameof(ComponentBuilderKind.IMessageComponent),
                target.ToString()
            ),
            source
        )
    };
}