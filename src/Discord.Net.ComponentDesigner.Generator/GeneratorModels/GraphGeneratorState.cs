using System;
using Discord.CX.Comparers;
using Discord.CX.Util;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public sealed class GraphGeneratorState(
    string key,
    GeneratorOptions generatorOptions,
    CXDesignerGeneratorState cx
) : IEquatable<GraphGeneratorState>
{
    public string Key { get; init; } = key;
    public GeneratorOptions GeneratorOptions { get; init; } = generatorOptions;
    public Compilation Compilation => CX.SemanticModel.Compilation;
    public CXDesignerGeneratorState CX { get; init; } = cx;

    public GraphGeneratorState WithCX(CXDesignerGeneratorState cx)
        => new(Key, GeneratorOptions, cx);
    
    public bool Equals(GraphGeneratorState other)
        => Key == other.Key &&
           GeneratorOptions.Equals(other.GeneratorOptions) &&
           CXDesignerGeneratorStateComparer.WithoutSpan.Equals(CX, other.CX);

    public override bool Equals(object? obj)
        => obj is GraphGeneratorState other && Equals(other);

    public override int GetHashCode()
        => Hash.Combine(
            Key,
            GeneratorOptions,
            CXDesignerGeneratorStateComparer.WithoutSpan.GetHashCode(CX)
        );
}