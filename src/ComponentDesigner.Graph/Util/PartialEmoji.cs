using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ComponentDesigner;

public abstract record PartialEmoji
{
    private static readonly Regex UnicodeEmojiRegex = new Regex(
        @"^(?:"
        + @"(?:\uD83C[\uDDE6-\uDDFF]){2}" // Flag (two regional indicators)
        + @"|(?:"
        + @"[\u2600-\u27BF]" // Misc symbols / Dingbats (BMP)
        + @"|\uD83C[\uDF00-\uDFFF]" // Surrogate pair block 1
        + @"|\uD83D[\uDC00-\uDEFF]" // Surrogate pair block 2
        + @"|\uD83E[\uDD00-\uDDFF]" // Surrogate pair block 3
        + @")"
        + @"(?:\uFE0F)?" // Optional variation selector-16
        + @"(?:\uD83C[\uDFFB-\uDFFF])?" // Optional skin tone modifier
        + @"(?:\u200D" // Optional ZWJ + another emoji (repeated)
        + @"(?:"
        + @"[\u2600-\u27BF]"
        + @"|\uD83C[\uDF00-\uDFFF]"
        + @"|\uD83D[\uDC00-\uDEFF]"
        + @"|\uD83E[\uDD00-\uDDFF]"
        + @")"
        + @"(?:\uFE0F)?"
        + @"(?:\uD83C[\uDFFB-\uDFFF])?"
        + @")*" // Zero or more ZWJ continuations
        + @")$",
        RegexOptions.Compiled
    );

    public sealed record Unicode(string Value) : PartialEmoji
    {
        public override string ToString()
            => Value;
    }

    public sealed record GuildEmote(
        ulong Id,
        string Name,
        bool IsAnimated
    ) : PartialEmoji
    {
        public override string ToString()
        {
            using var _ = StringBuilder.Pooled(out var sb);

            sb.Append('<');
            if (IsAnimated)
                sb.Append('a');

            sb.Append(':');
            sb.Append(Name);
            sb.Append(':');
            sb.Append(Id);
            sb.Append('>');

            return sb.ToString();
        }

        public static bool TryParse(string str, [MaybeNullWhen(false)] out GuildEmote guildEmote)
        {
            guildEmote = null;

            if (
                str.Length >= 4 &&
                str[0] == '<' &&
                (str[1] == ':' || (str[1] == 'a' && str[2] == ':')) &&
                str[str.Length - 1] == '>'
            )
            {
                var animated = str[1] == 'a';
                var startIndex = animated ? 3 : 2;

                var splitIndex = str.IndexOf(':', startIndex);

                if (splitIndex == -1)
                    return false;

                if (
                    !ulong.TryParse(
                        str.Substring(splitIndex + 1, str.Length - splitIndex - 2),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var id)
                ) return false;


                var name = str.Substring(startIndex, splitIndex - startIndex);
                guildEmote = new GuildEmote(id, name, animated);
                return true;
            }

            return false;
        }
    }

    public static bool TryParse(string str, [MaybeNullWhen(false)] out PartialEmoji partialEmoji)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            partialEmoji = null;
            return false;
        }

        if (UnicodeEmojiRegex.IsMatch(str))
        {
            partialEmoji = new Unicode(str);
            return true;
        }

        if (GuildEmote.TryParse(str, out var guildEmote))
        {
            partialEmoji = guildEmote;
            return true;
        }

        partialEmoji = null;
        return false;
    }
}