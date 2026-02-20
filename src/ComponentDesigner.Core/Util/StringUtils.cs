using System.Text;
using ComponentDesigner;

namespace ComponentDesigner.Util;

public static class StringUtils
{
    public static string CollapseAndTrimNewlines(this string str)
    {
        var parts = str.Split('\n');

        if (parts.Length is 1) return str;

        using var _ = StringBuilder.Pooled(out var sb);

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var partEnd = part.Length - 1;
            var partStart = 0;

            if (string.IsNullOrEmpty(part)) continue;

            // check for '\r'
            if (part[part.Length - 1] is '\r')
                partEnd--;

            // trim start
            for (; partStart < partEnd; partStart++)
            {
                var ch = part[partStart];
                if (!char.IsWhiteSpace(ch))
                    break;
            }

            // trim end
            for (; partEnd > partStart; partEnd--)
            {
                var ch = part[partEnd];
                if (!char.IsWhiteSpace(ch))
                    break;
            }

            if (partStart > partEnd) continue;

            if (sb.Length is not 0)
                sb.Append(' ');

            sb.Append(part, partStart, (partEnd + 1) - partStart);
        }

        return sb.ToString();
    }

    public static string Indent(this string value, int size)
    {
        if (size is 0) return value;

        var padStr = new string(' ', size);

        var split = value.Split('\n');

        if (split.Length is 1) return $"{padStr}{value}";

        return string.Join(
            "\n",
            split.Select(x => $"{padStr}{x}")
        );
    }

    public static string Prefix(this string str, int count, char prefixChar = ' ')
        => count > 0 ? $"{new string(prefixChar, count)}{str}" : str;

    public static string Postfix(this string str, int count, char prefixChar = ' ')
        => count > 0 ? $"{str}{new string(prefixChar, count)}" : str;

    public static string WithNewlinePadding(this string str, int pad)
        => str.Replace("\n", "\n".Postfix(pad));

    public static string WrapIfSome(this string str, string wrapping)
        => string.IsNullOrWhiteSpace(str) ? str : $"{wrapping}{str}{wrapping}";

    public static string PrefixIfSome(this string str, int count, char prefixChar = ' ')
        => string.IsNullOrWhiteSpace(str) ? str : $"{new string(prefixChar, count)}{str}";

    public static string PrefixIfSome(this string str, string prefix)
        => string.IsNullOrWhiteSpace(str) ? str : $"{prefix}{str}";

    public static string PostfixIfSome(this string str, int count, char prefixChar = ' ')
        => string.IsNullOrWhiteSpace(str) ? str : $"{str}{new string(prefixChar, count)}";

    public static string PostfixIfSome(this string str, string postfix)
        => string.IsNullOrWhiteSpace(str) ? str : $"{str}{postfix}";

    public static string Map(this string str, Func<string, string> mapper)
        => string.IsNullOrWhiteSpace(str) ? str : mapper(str);

    public static string NormalizeIndentation(this string str)
    {
        var rawLines = str.Split('\n');
        var lines = new List<string>(rawLines);

        var leadingNewLineCount = 0;
        var trailingNewLineCount = 0;

        // remove leading empty lines
        foreach (var line in rawLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                lines.Remove(line);
                leadingNewLineCount++;
            }
            else break;
        }

        // remove trailing empty lines
        for (var i = rawLines.Length - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(rawLines[i]))
            {
                lines.Remove(rawLines[i]);
                trailingNewLineCount++;
            }
            else break;
        }

        if (lines.Count is 0) return str;

        var minSpacing = lines.Min(x =>
            x.TakeWhile(char.IsWhiteSpace).Count()
        );

        if (minSpacing is 0 or int.MaxValue) return str;

        using var _ = StringBuilder.Pooled(out var sb);

        for (var i = 0; i < leadingNewLineCount; i++)
            sb.AppendLine();

        for (var i = 0; i < lines.Count; i++)
        {
            if (i is not 0) sb.AppendLine();

            var line = lines[i];

            // for windows, remove the carriage return character which can linger due to the split only
            // splitting by the newline character
            if (line.EndsWith("\r")) line = line.Substring(0, line.Length - 1);

            sb.Append(
                line.Length > minSpacing ? line.Substring(minSpacing) : string.Empty
            );
        }

        for (var i = 0; i < trailingNewLineCount; i++)
            sb.AppendLine();

        return sb.ToString();
    }

    public static void NormalizeIndentation(this StringBuilder str)
    {
        var normal = str.ToString().NormalizeIndentation();
        str.Clear().Append(normal);
    }
}