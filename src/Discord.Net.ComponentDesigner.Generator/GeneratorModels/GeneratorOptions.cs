using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Discord.CX;

public readonly record struct GeneratorOptions(
    bool EnableAutoRows = false,
    bool EnableAutoTextDisplay = false,
    LanguageVersion? CSharpLangVersion = null
)
{
    public static readonly GeneratorOptions Default = new();
    
    private const string ENABLE_AUTO_ROWS_KEY = "build_property.EnableAutoRows";
    private const string ENABLE_AUTO_TEXT_DISPLAY = "build_property.EnableAutoTextDisplay";

    public GeneratorOptions WithOverloads(ComponentDesignerOptionOverloads overloads)
    {
        if (overloads.IsEmpty) return this;

        return this with
        {
            EnableAutoRows = overloads.EnableAutoRows.GetValueOrDefault(EnableAutoRows),
            EnableAutoTextDisplay = overloads.EnableAutoTextDisplays.GetValueOrDefault(EnableAutoTextDisplay)
        };
    }
    
    public static IncrementalValueProvider<GeneratorOptions> CreateProvider(
        IncrementalGeneratorInitializationContext context
    )
    {
        return context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(CreateOptions)
            .WithTrackingName(TrackingNames.GENERATOR_OPTIONS);

        static GeneratorOptions CreateOptions(
            (Compilation Compilation, AnalyzerConfigOptionsProvider Options) tuple,
            CancellationToken token
        )
        {
            var compilation = tuple.Compilation;
            var analyzerConfig = tuple.Options;

            LanguageVersion? langVersion = compilation is CSharpCompilation csharp
                ? csharp.LanguageVersion
                : null;

            var autoRows
                = analyzerConfig
                      .GlobalOptions
                      .TryGetValue(ENABLE_AUTO_ROWS_KEY, out var val) &&
                  bool.TryParse(val.ToLowerInvariant(), out var bl) && bl;
            
            var autoTextDisplay
                = analyzerConfig
                      .GlobalOptions
                      .TryGetValue(ENABLE_AUTO_TEXT_DISPLAY, out val) &&
                  bool.TryParse(val.ToLowerInvariant(), out bl) && bl;

            return new GeneratorOptions(
                EnableAutoRows: autoRows,
                EnableAutoTextDisplay: autoTextDisplay,
                CSharpLangVersion: langVersion
            );
        }
    }
}