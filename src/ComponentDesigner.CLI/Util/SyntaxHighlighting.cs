using System.Globalization;
using System.Reflection;
using System.Text;
using ComponentDesigner.Parser;
using Spectre.Console;
using Spectre.Console.Advanced;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using RegistryOptions = TextMateSharp.Grammars.RegistryOptions;

namespace ComponentDesigner.Util;

public class SyntaxHighlighter
{
    private static readonly IRawGrammar GrammarDefinition;
    private static readonly IGrammar Grammar;
    private static readonly Registry Registry;

    static SyntaxHighlighter()
    {
        using var syntaxStream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("ComponentDesigner.CLI.cx.tmLanguage.json");

        if (syntaxStream is null)
        {
            throw new InvalidOperationException("Missing syntax grammar");
        }

        using var sr = new StreamReader(syntaxStream);
        GrammarDefinition = GrammarReader.ReadGrammarSync(sr);

        var options = new LocalRegistryOptions();
        Registry = new Registry(options);
        Grammar = Registry.LoadGrammar("source.cx");
    }

    private sealed class LocalRegistryOptions : IRegistryOptions
    {
        private readonly RegistryOptions _options;

        public LocalRegistryOptions()
        {
            _options = new(ThemeName.Dark);
        }

        public IRawTheme GetTheme(string scopeName)
        {
            return _options.GetTheme(scopeName);
        }

        public IRawGrammar GetGrammar(string scopeName)
        {
            if (scopeName is "source.cx") return GrammarDefinition;

            return _options.GetGrammar(scopeName);
        }

        public ICollection<string> GetInjections(string scopeName)
        {
            return _options.GetInjections(scopeName);
        }

        public IRawTheme GetDefaultTheme()
        {
            return _options.GetDefaultTheme();
        }
    }

    private readonly CXSourceText _source;

    private readonly IToken[] _tokens;
    private readonly int[] _tokenIndexes;

    public SyntaxHighlighter(CXSourceText source)
    {
        _source = source;
        Tokenize(
            source,
            out _tokens,
            out _tokenIndexes
        );
    }

    private static void Tokenize(
        CXSourceText text,
        out IToken[] tokensArray,
        out int[] tokenPositionsArray
    )
    {
        var tokens = new List<IToken>();
        var tokenPositions = new List<int>();

        IStateStack? ruleStack = null;
        for (var i = 0; i < text.Lines.Count; i++)
        {
            var line = text.Lines[i];
            var lineStr = text[line.Start, line.Span.Length];
            var result = Grammar.TokenizeLine(lineStr, ruleStack, TimeSpan.MaxValue);
            ruleStack = result.RuleStack;

            foreach (var token in result.Tokens)
            {
                var startPos = line.Start + token.StartIndex;
                tokens.Add(token);
                tokenPositions.Add(startPos);
            }
        }

        tokenPositionsArray = tokenPositions.ToArray();
        tokensArray = tokens.ToArray();
    }

    public string GetHighlightedSource()
    {
        var theme = Registry.GetTheme();
        var sb = new StringBuilder();

        var needsNewLine = false;
        
        for (var i = 0; i < _tokenIndexes.Length; i++)
        {
            if (needsNewLine)
            {
                sb.AppendLine();
                needsNewLine = false;
            }
            
            var token = _tokens[i];
            var tokenStart = _tokenIndexes[i];
            var tokenEnd = tokenStart + token.Length;
            var slice = _source[tokenStart, token.Length];
            
            sb.Append(GetMarkupForToken(token, theme, slice));

            var line = _source.Lines.GetLineFromPosition(tokenEnd);

            needsNewLine = line.End == tokenEnd;
        }

        return sb.ToString();
    }
    
    public string GetHighlightedSection(int start, int count)
    {
        var theme = Registry.GetTheme();
        
        var sb = new StringBuilder();

        var needsNewLine = false;
        var pos = start;
        var end = start + count;
        while (pos < end)
        {
            if (needsNewLine)
            {
                sb.AppendLine();
                needsNewLine = false;
            }
            
            var index = Array.BinarySearch(_tokenIndexes, pos);
            
            if (index < 0)
                index = ~index;
            
            var token = _tokens[index];
            var tokenStart = _tokenIndexes[index];
            
            var sliceEnd = Math.Min(tokenStart + token.Length, end);

            var slice = _source[pos, sliceEnd - pos];

            var markup = GetMarkupForToken(token, theme, slice);
            sb.Append(markup);
            
            pos = sliceEnd;
            
            var line = _source.Lines.GetLineFromPosition(sliceEnd);

            needsNewLine = line.End == sliceEnd;
        }

        return sb.ToString();
    }

    private static string GetMarkupForToken(IToken token, Theme theme, string text)
    {
        var foreground = -1;
        var background = -1;
        var fontStyle = FontStyle.NotSet;
            
        foreach (var themeRule in theme.Match(token.Scopes))
        {
            if (foreground == -1 && themeRule.foreground > 0)
                foreground = themeRule.foreground;
            if (background == -1 && themeRule.background > 0)
                background = themeRule.background;
            if (fontStyle == FontStyle.NotSet && themeRule.fontStyle > 0)
                fontStyle = themeRule.fontStyle;
        }
            
        var decoration = GetDecoration(fontStyle);

        var backgroundColor = GetColor(background, theme);
        var foregroundColor = GetColor(foreground, theme);

        var escaped = Markup.Escape(text);

        if (decoration is Decoration.None && backgroundColor == Color.Default && foregroundColor == Color.Default)
            return escaped;

        var macro = new StringBuilder();

        if (decoration is not Decoration.None)
            macro.Append(decoration.ToString().ToLower());

        if (foregroundColor != Color.Default)
        {
            if (macro.Length > 0) macro.Append(' ');

            macro.Append(foregroundColor.ToMarkup());
        }
        
        if (backgroundColor != Color.Default)
        {
            if (macro.Length > 0) macro.Append(' ');

            macro.Append("on ").Append(backgroundColor.ToMarkup());
        }

        return $"[{macro}]{escaped}[/]";

        // var style = new Style(foregroundColor, backgroundColor, decoration);
        // return new Markup(text.Replace("[", "[[").Replace("]", "]]"), style);
    }
    
    private static Color GetColor(int colorId, Theme theme)
    {
        if (colorId == -1)
            return Color.Default;

        return HexToColor(theme.GetColor(colorId));
    }

    private static Decoration GetDecoration(FontStyle fontStyle)
    {
        var result = Decoration.None;

        if (fontStyle == FontStyle.NotSet)
            return result;

        if ((fontStyle & FontStyle.Italic) != 0)
            result |= Decoration.Italic;

        if ((fontStyle & FontStyle.Underline) != 0)
            result |= Decoration.Underline;

        if ((fontStyle & FontStyle.Bold) != 0)
            result |= Decoration.Bold;

        return result;
    }

    private  static Color HexToColor(string hexString)
    {
        //replace # occurences
        if (hexString.IndexOf('#') != -1)
            hexString = hexString.Replace("#", "");

        byte r, g, b = 0;

        r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
        g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
        b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

        return new Color(r, g, b);
    }
}