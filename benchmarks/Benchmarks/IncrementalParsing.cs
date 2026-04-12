using System.Reflection;
using BenchmarkDotNet.Attributes;
using ComponentDesigner.Parser;

namespace Benchmarks;

[MemoryDiagnoser]
public class IncrementalParsing
{
    public string Source1 { get; }
    public string Source2 { get; }

    public CXDocument Source1Parsed { get; set; } = null!;
    public IReadOnlyList<CXTextChange> Changes { get; set; } = null!;
    
    public IncrementalParsing()
    {
        var asm = Assembly
            .GetExecutingAssembly();
        
        var i1 = asm.GetManifestResourceStream("Benchmarks.samples.incr-1.txt");
        var i2 = asm.GetManifestResourceStream("Benchmarks.samples.incr-2.txt");

        if (i1 is null || i2 is null)
            throw new InvalidOperationException("Missing embedded resources");

        using var sr1 = new StreamReader(i1);
        Source1 = sr1.ReadToEnd();
        
        using var sr2 = new StreamReader(i2);
        Source2 = sr2.ReadToEnd();
    }

    [Benchmark]
    public CXDocument ParseFull()
        => CXParser.Parse(CXSourceText.From(Source1).CreateReader());

    [GlobalSetup(Target = nameof(IncrementalChange))]
    public void SetupIncremental()
    {
        var source1 = CXSourceText.From(Source1);
        var change = new CXTextChange(
            new(source1.Lines[350].Start + 37, 1),
            string.Empty
        );
        Changes = [change];
        Source1Parsed = CXParser.Parse(source1.CreateReader());
    }
    
    [Benchmark]
    public CXDocument IncrementalChange()
    {
        return Source1Parsed.IncrementalParse(
            CXSourceText.From(Source2).CreateReader(),
            Changes
        );
    }
}