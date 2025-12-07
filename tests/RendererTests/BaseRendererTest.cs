using Discord.CX;
using Discord.CX.Nodes;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnitTests.RendererTests;

public abstract class BaseRendererTest : BaseTestWithDiagnostics
{
    protected Compilation Compilation => _compilation ??= Compilations.Create();

    private Compilation? _compilation;

    protected enum ParseMode
    {
        AttributeValue,
        ElementValue
    }


    protected void AssertRenders(
        string? cx,
        PropertyRenderer renderer,
        string expected,
        ParseMode mode = ParseMode.AttributeValue,
        DesignerInterpolationInfo[]? interpolations = null,
        bool isAttributeValue = false,
        bool requiresValue = true,
        bool isOptional = false,
        string? propertyName = null,
        int? wrappingQuoteCount = null
    )
    {
        AssertEmptyDiagnostics();

        ClearDiagnostics();

        CXValue? value = null;
        CXParser? parser = null;

        if (cx is not null)
        {
            var source = CXSourceText.From(cx);

            var interpolationSpans = interpolations?.Select(x => x.Span).ToArray();
        
            parser = new CXParser(source.CreateReader(
                wrappingQuoteCount: wrappingQuoteCount,
                interpolations: interpolationSpans
            ));

            switch (mode)
            {
                case ParseMode.AttributeValue:
                    value = parser.ParseAttributeValue();
                    break;
                case ParseMode.ElementValue:
                    if (!parser.TryParseElementChild([], out var child))
                        Assert.Fail("Can't parse as element value");

                    Assert.IsAssignableFrom<CXValue>(child);

                    value = (CXValue)child;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var document = new CXDoc(
                parser,
                [value]
            );
        }
       

        var context = new MockComponentContext(
            Compilation,
            parser?.Lexer.InterpolationMap ?? [],
            interpolations ?? []
        );

        var propValue = new MockPropertyValue(
            value,
            null,
            value is not null,
            value is not null,
            isAttributeValue,
            requiresValue,
            isOptional,
            propertyName ?? "test-property"
        );

        var result = renderer(context, propValue);

        PushDiagnostics(context.AllDiagnostics);

        Assert.Equal(expected, result);
    }

    private sealed record MockPropertyValue(
        CXValue? Value,
        CXGraph.Node? Node,
        bool IsSpecified,
        bool HasValue,
        bool IsAttributeValue,
        bool RequiresValue,
        bool IsOptional,
        string PropertyName
    ) : IComponentPropertyValue;

    private sealed class MockComponentContext : IComponentContext
    {
        private readonly CXToken[] _interpolationMap;
        private readonly DesignerInterpolationInfo[] _interpolations;

        private sealed record Scope(
            List<Diagnostic> Bag,
            Scope? Parent,
            MockComponentContext Context
        ) : IDisposable
        {
            public void Dispose()
            {
                if (Parent is null) return;

                Context._scope = Parent;
            }
        }

        public KnownTypes KnownTypes => Compilation.GetKnownTypes();
        public Compilation Compilation { get; }

        public bool HasErrors => GlobalDiagnostics.Any(x => x.Severity is DiagnosticSeverity.Error);

        public IReadOnlyList<Diagnostic> GlobalDiagnostics => _globalDiagnostics;
        
        public ComponentTypingContext RootTypingContext => ComponentTypingContext.Default;

        public List<Diagnostic> AllDiagnostics { get; }
        private readonly List<Diagnostic> _globalDiagnostics;
        private Scope _scope;

        public MockComponentContext(
            Compilation compilation,
            CXToken[] interpolationMap,
            DesignerInterpolationInfo[] interpolations
        )
        {
            _interpolationMap = interpolationMap;
            _interpolations = interpolations;
            Compilation = compilation;
            _globalDiagnostics = [];
            AllDiagnostics = [];

            _scope = new(_globalDiagnostics, null, this);
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            _scope.Bag.Add(diagnostic);
            AllDiagnostics.Add(diagnostic);
        }

        public Location GetLocation(TextSpan span)
        {
            return Location.Create(
                "test.cx",
                span,
                default
            );
        }

        public IDisposable CreateDiagnosticScope(List<Diagnostic> bag)
        {
            return _scope = new(bag, _scope, this);
        }

        public string GetDesignerValue(int index, string? type = null)
            => type is not null ? $"designer.GetValue<{type}>({index})" : $"designer.GetValueAsString({index})";


        public DesignerInterpolationInfo GetInterpolationInfo(int index)
            => _interpolations[index];

        public DesignerInterpolationInfo GetInterpolationInfo(CXToken token)
            => GetInterpolationInfo(Array.IndexOf(_interpolationMap, token));
    }
}