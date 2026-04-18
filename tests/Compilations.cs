using ComponentDesigner;
using Discord;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnitTests;

public static class Compilations
{
    public static Compilation Create()
    {
        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(Discord.cx).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CXSyntax).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IDiscordClient).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
            MetadataReference.CreateFromFile(
                Path.Combine(
                    Path.GetDirectoryName(typeof(object).Assembly.Location)!,
                    "System.Runtime.dll"
                )
            ),
        };

        return CSharpCompilation.Create(
            assemblyName: "Tests",
            references: references
        );
    }
}