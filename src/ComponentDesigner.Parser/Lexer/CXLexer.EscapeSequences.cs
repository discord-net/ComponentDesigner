using System.Globalization;

namespace ComponentDesigner.Parser;

partial class CXLexer
{
    /// <summary>
    ///     Attempts to parse an escape sequence name into its respective character.
    /// </summary>
    /// <param name="sequence">
    ///     The escape sequence name (ex <c>lt</c>) to parse.
    /// </param>
    /// <param name="value">
    ///     The character value the given escape sequence represents if found; otherwise <see cref="NULL_CHAR"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the given sequence is recognized; otherwise <see langword="false"/>. 
    /// </returns>
    private static bool TryParseEscapeSequence(string sequence, out char value)
    {
        if (sequence.Length is 0)
        {
            value = NULL_CHAR;
            return false;
        }

        if (sequence[0] is HASHTAG_CHAR)
        {
            var style = NumberStyles.Integer;
            var startsAt = 1;

            if (sequence.Length > 1 && sequence[1] is 'x')
            {
                startsAt++;
                style = NumberStyles.HexNumber;
            }
            
            // attempt to parse a digit
            if (!ushort.TryParse(sequence.Substring(startsAt), style, null, out var digit))
            {
                value = NULL_CHAR;
                return false;
            }

            value = (char)digit;
            return true;
        }

        value = sequence switch
        {
            "Tab" => TAB_CHAR,
            "NewLine" => NEWLINE_CHAR,
            "nbsp" => SPACE_CHAR,
            "quot" => DOUBLE_QUOTE_CHAR,
            "amp" => AMPERSAND_CHAR,
            "lt" => LESS_THAN_CHAR,
            "gt" => GREATER_THAN_CHAR,

            // Inverted exclamation mark
            "iexcl" => '\u00a1', // Inverted exclamation mark
            "cent" => '\u00a2', // Cent
            "pound" => '\u00a3', // Pound
            "curren" => '\u00a4', // Currency
            "yen" => '\u00a5', // Yen
            "brvbar" => '\u00a6', // Broken vertical bar
            "sect" => '\u00a7', // Section
            "uml" => '\u00a8', // Spacing diaeresis
            "copy" => '\u00a9', // Copyright
            "ordf" => '\u00aa', // Feminine ordinal indicator
            "laquo" => '\u00ab', // Opening/Left angle quotation mark
            "not" => '\u00ac', // Negation
            "shy" => '\u00ad', // Soft hyphen
            "reg" => '\u00ae', // Registered trademark
            "macr" => '\u00af', // Spacing macron
            "deg" => '\u00b1', // Degree
            "plusmn" => '\u00b2', // Plus or minus
            "sup2" => '\u00b2', // Superscript 2
            "sup3" => '\u00b3', // Superscript 3
            "acute" => '\u00b4', // Spacing acute
            "micro" => '\u00b5', // Micro
            "para" => '\u00b6', // Paragraph
            "dot" => '\u00b7', // Dot
            "cedil" => '\u00b8', // Spacing cedilla
            "sup1" => '\u00b9', // Superscript 1
            "ordm" => '\u00ba', // Masculine ordinal indicator
            "raquo" => '\u00bb', // Closing/Right angle quotation mark
            "frac14" => '\u00bc', // Fraction 1/4
            "frac12" => '\u00bd', // Fraction 1/2
            "frac34" => '\u00be', // Fraction 3/4
            "iquest" => '\u00bf', // Inverted question mark
            "Agrave" => '\u00c0', // Capital a with grave accent
            "Aacute" => '\u00c1', // Capital a with acute accent
            "Acirc" => '\u00c2', // Capital a with circumflex accent
            "Atilde" => '\u00c3', // Capital a with tilde
            "Auml" => '\u00c4', // Capital a with umlaut
            "Aring" => '\u00c5', // Capital a with ring
            "AElig" => '\u00c6', // Capital ae
            "Ccedil" => '\u00c7', // Capital c with cedilla
            "Egrave" => '\u00c8', // Capital e with grave accent
            "Eacute" => '\u00c9', // Capital e with acute accent
            "Ecirc" => '\u00ca', // Capital e with circumflex accent
            "Euml" => '\u00cb', // Capital e with umlaut
            "Igrave" => '\u00cc', // Capital i with grave accent
            "Iacute" => '\u00cd', // Capital i with accute accent
            "Icirc" => '\u00ce', // Capital i with circumflex accent
            "Iuml" => '\u00cf', // Capital i with umlaut
            "ETH" => '\u00d0', // Capital eth (Icelandic)
            "Ntilde" => '\u00d1', // Capital n with tilde
            "Ograve" => '\u00d2', // Capital o with grave accent
            "Oacute" => '\u00d3', // Capital o with accute accent
            "Ocirc" => '\u00d4', // Capital o with circumflex accent
            "Otilde" => '\u00d5', // Capital o with tilde
            "Ouml" => '\u00d6', // Capital o with umlaut
            "times" => '\u00d7', // Multiplication
            "Oslash" => '\u00d8', // Capital o with slash
            "Ugrave" => '\u00d9', // Capital u with grave accent
            "Uacute" => '\u00da', // Capital u with acute accent
            "Ucirc" => '\u00db', // Capital u with circumflex accent
            "Uuml" => '\u00dc', // Capital u with umlaut
            "Yacute" => '\u00dd', // Capital y with acute accent
            "THORN" => '\u00de', // Capital thorn (Icelandic)
            "szlig" => '\u00df', // Lowercase sharp s (German)
            "agrave" => '\u00e0', // Lowercase a with grave accent
            "aacute" => '\u00e1', // Lowercase a with acute accent
            "acirc" => '\u00e2', // Lowercase a with circumflex accent
            "atilde" => '\u00e3', // Lowercase a with tilde
            "auml" => '\u00e4', // Lowercase a with umlaut
            "aring" => '\u00e5', // Lowercase a with ring
            "aelig" => '\u00e6', // Lowercase ae
            "ccedil" => '\u00e7', // Lowercase c with cedilla
            "egrave" => '\u00e8', // Lowercase e with grave accent
            "eacute" => '\u00e9', // Lowercase e with acute accent
            "ecirc" => '\u00ea', // Lowercase e with circumflex accent
            "euml" => '\u00eb', // Lowercase e with umlaut
            "igrave" => '\u00ec', // Lowercase i with grave accent
            "iacute" => '\u00ed', // Lowercase i with acute accent
            "icirc" => '\u00ee', // Lowercase i with circumflex accent
            "iuml" => '\u00ef', // Lowercase i with umlaut
            "eth" => '\u00f0', // Lowercase eth (Icelandic)
            "ntilde" => '\u00f1', // Lowercase n with tilde
            "ograve" => '\u00f2', // Lowercase o with grave accent
            "oacute" => '\u00f3', // Lowercase o with acute accent
            "ocirc" => '\u00f4', // Lowercase o with circumflex accent
            "otilde" => '\u00f5', // Lowercase o with tilde
            "ouml" => '\u00f6', // Lowercase o with umlaut
            "divide" => '\u00f7', // Divide
            "oslash" => '\u00f8', // Lowercase o with slash
            "ugrave" => '\u00f9', // Lowercase u with grave accent
            "uacute" => '\u00fa', // Lowercase u with acute accent
            "ucirc" => '\u00fb', // Lowercase u with circumflex accent
            "uuml" => '\u00fc', // Lowercase u with umlaut
            "yacute" => '\u00fd', // Lowercase y with acute accent
            "thorn" => '\u00fe', // Lowercase thorn (Icelandic)
            "yuml" => '\u00ff', // Lowercase y with umlaut
            "Amacr" => '\u0100', // Latin capital letter a with macron
            "amacr" => '\u0101', // Latin small letter a with macron
            "Abreve" => '\u0102', // Latin capital letter a with breve
            "abreve" => '\u0103', // Latin small letter a with breve
            "Aogon" => '\u0104', // Latin capital letter a with ogonek
            "aogon" => '\u0105', // Latin small letter a with ogonek
            "Cacute" => '\u0106', // Latin capital letter c with acute
            "cacute" => '\u0107', // Latin small letter c with acute
            "Ccirc" => '\u0108', // Latin capital letter c with circumflex
            "ccirc" => '\u0109', // Latin small letter c with circumflex
            "Cdot" => '\u010a', // Latin capital letter c with dot above
            "cdot" => '\u010b', // Latin small letter c with dot above
            "Ccaron" => '\u010c', // Latin capital letter c with caron
            "ccaron" => '\u010d', // Latin small letter c with caron
            "Dcaron" => '\u010e', // Latin capital letter d with caron
            "dcaron" => '\u010f', // Latin small letter d with caron
            "Dstrok" => '\u0110', // Latin capital letter d with stroke
            "dstrok" => '\u0111', // Latin small letter d with stroke
            "Emacr" => '\u0112', // Latin capital letter e with macron
            "emacr" => '\u0113', // Latin small letter e with macron
            "Ebreve" => '\u0114', // Latin capital letter e with breve
            "ebreve" => '\u0115', // Latin small letter e with breve
            "Edot" => '\u0116', // Latin capital letter e with dot above
            "edot" => '\u0117', // Latin small letter e with dot above
            "Eogon" => '\u0118', // Latin capital letter e with ogonek
            "eogon" => '\u0119', // Latin small letter e with ogonek
            "Ecaron" => '\u011a', // Latin capital letter e with caron
            "ecaron" => '\u011b', // Latin small letter e with caron
            "Gcirc" => '\u011c', // Latin capital letter g with circumflex
            "gcirc" => '\u011d', // Latin small letter g with circumflex
            "Gbreve" => '\u011e', // Latin capital letter g with breve
            "gbreve" => '\u011f', // Latin small letter g with breve
            "Gdot" => '\u0120', // Latin capital letter g with dot above
            "gdot" => '\u0121', // Latin small letter g with dot above
            "Gcedil" => '\u0122', // Latin capital letter g with cedilla
            "gcedil" => '\u0123', // Latin small letter g with cedilla
            "Hcirc" => '\u0124', // Latin capital letter h with circumflex
            "hcirc" => '\u0125', // Latin small letter h with circumflex
            "Hstrok" => '\u0126', // Latin capital letter h with stroke
            "hstrok" => '\u0127', // Latin small letter h with stroke
            "Itilde" => '\u0128', // Latin capital letter I with tilde
            "itilde" => '\u0129', // Latin small letter I with tilde
            "Imacr" => '\u012a', // Latin capital letter I with macron
            "imacr" => '\u012b', // Latin small letter I with macron
            "Ibreve" => '\u012c', // Latin capital letter I with breve
            "ibreve" => '\u012d', // Latin small letter I with breve
            "Iogon" => '\u012e', // Latin capital letter I with ogonek
            "iogon" => '\u012f', // Latin small letter I with ogonek
            "Idot" => '\u0130', // Latin capital letter I with dot above
            "imath" or "inodot" => '\u0131', //Latin small letter dotless I
            "IJlig" => '\u0132', // Latin capital ligature ij
            "ijlig" => '\u0133', // Latin small ligature ij
            "Jcirc" => '\u0134', // Latin capital letter j with circumflex
            "jcirc" => '\u0135', // Latin small letter j with circumflex
            "Kcedil" => '\u0136', // Latin capital letter k with cedilla
            "kcedil" => '\u0137', // Latin small letter k with cedilla
            "kgreen" => '\u0138', // Latin small letter kra
            "Lacute" => '\u0139', // Latin capital letter l with acute
            "lacute" => '\u013a', // Latin small letter l with acute
            "Lcedil" => '\u013b', // Latin capital letter l with cedilla
            "lcedil" => '\u013c', // Latin small letter l with cedilla
            "Lcaron" => '\u013d', // Latin capital letter l with caron
            "lcaron" => '\u013e', // Latin small letter l with caron
            "Lmidot" => '\u013f', // Latin capital letter l with middle dot
            "lmidot" => '\u0140', // Latin small letter l with middle dot
            "Lstrok" => '\u0141', // Latin capital letter l with stroke
            "lstrok" => '\u0142', // Latin small letter l with stroke
            "Nacute" => '\u0143', // Latin capital letter n with acute
            "nacute" => '\u0144', // Latin small letter n with acute
            "Ncedil" => '\u0145', // Latin capital letter n with cedilla
            "ncedil" => '\u0146', // Latin small letter n with cedilla
            "Ncaron" => '\u0147', // Latin capital letter n with caron
            "ncaron" => '\u0148', // Latin small letter n with caron
            "napos" => '\u0149', // Latin small letter n preceded by apostrophe
            "ENG" => '\u014a', // Latin capital letter eng
            "eng" => '\u014b', // Latin small letter eng
            "Omacr" => '\u014c', // Latin capital letter o with macron
            "omacr" => '\u014d', // Latin small letter o with macron
            "Obreve" => '\u014e', // Latin capital letter o with breve
            "obreve" => '\u014f', // Latin small letter o with breve
            "Odblac" => '\u0150', // Latin capital letter o with double acute
            "odblac" => '\u0151', // Latin small letter o with double acute
            "OElig" => '\u0152', // Uppercase ligature OE
            "oelig" => '\u0153', // Lowercase ligature OE
            "Racute" => '\u0154', // Latin capital letter r with acute
            "racute" => '\u0155', // Latin small letter r with acute
            "Rcedil" => '\u0156', // Latin capital letter r with cedilla
            "rcedil" => '\u0157', // Latin small letter r with cedilla
            "Rcaron" => '\u0158', // Latin capital letter r with caron
            "rcaron" => '\u0159', // Latin small letter r with caron
            "Sacute" => '\u015a', // Latin capital letter s with acute
            "sacute" => '\u015b', // Latin small letter s with acute
            "Scirc" => '\u015c', // Latin capital letter s with circumflex
            "scirc" => '\u015d', // Latin small letter s with circumflex
            "Scedil" => '\u015e', // Latin capital letter s with cedilla
            "scedil" => '\u015f', // Latin small letter s with cedilla
            "Scaron" => '\u0160', // Uppercase S with caron
            "scaron" => '\u0161', // Lowercase S with caron
            "Tcedil" => '\u0162', // Latin capital letter t with cedilla
            "tcedil" => '\u0163', // Latin small letter t with cedilla
            "Tcaron" => '\u0164', // Latin capital letter t with caron
            "tcaron" => '\u0165', // Latin small letter t with caron
            "Tstrok" => '\u0166', // Latin capital letter t with stroke
            "tstrok" => '\u0167', // Latin small letter t with stroke
            "Utilde" => '\u0168', // Latin capital letter u with tilde
            "utilde" => '\u0169', // Latin small letter u with tilde
            "Umacr" => '\u016a', // Latin capital letter u with macron
            "umacr" => '\u016b', // Latin small letter u with macron
            "Ubreve" => '\u016c', // Latin capital letter u with breve
            "ubreve" => '\u016d', // Latin small letter u with breve
            "Uring" => '\u016e', // Latin capital letter u with ring above
            "uring" => '\u016f', // Latin small letter u with ring above
            "Udblac" => '\u0170', // Latin capital letter u with double acute
            "udblac" => '\u0171', // Latin small letter u with double acute
            "Uogon" => '\u0172', // Latin capital letter u with ogonek
            "uogon" => '\u0173', // Latin small letter u with ogonek
            "Wcirc" => '\u0174', // Latin capital letter w with circumflex
            "wcirc" => '\u0175', // Latin small letter w with circumflex
            "Ycirc" => '\u0176', // Latin capital letter y with circumflex
            "ycirc" => '\u0177', // Latin small letter y with circumflex
            "Yuml" => '\u0178', // Capital Y with diaeres
            "fnof" => '\u0192', // Lowercase with hook
            "circ" => '\u02c6', // Circumflex accent
            "tilde" => '\u02dc', // Tilde
            "Alpha" => '\u0391', // Alpha
            "Beta" => '\u0392', // Beta
            "Gamma" => '\u0393', // Gamma
            "Delta" => '\u0394', // Delta
            "Epsilon" => '\u0395', // Epsilon
            "Zeta" => '\u0396', // Zeta
            "Eta" => '\u0397', // Eta
            "Theta" => '\u0398', // Theta
            "Iota" => '\u0399', // Iota
            "Kappa" => '\u039a', // Kappa
            "Lambda" => '\u039b', // Lambda
            "Mu" => '\u039c', // Mu
            "Nu" => '\u039d', // Nu
            "Xi" => '\u039e', // Xi
            "Omicron" => '\u039f', // Omicron
            "Pi" => '\u03a0', // Pi
            "Rho" => '\u03a1', // Rho
            "Sigma" => '\u03a3', // Sigma
            "Tau" => '\u03a4', // Tau
            "Upsilon" => '\u03a5', // Upsilon
            "Phi" => '\u03a6', // Phi
            "Chi" => '\u03a7', // Chi
            "Psi" => '\u03a8', // Psi
            "Omega" => '\u03a9', // Omega
            "alpha" => '\u03b1', // alpha
            "beta" => '\u03b2', // beta
            "gamma" => '\u03b3', // gamma
            "delta" => '\u03b4', // delta
            "epsilon" => '\u03b5', // epsilon
            "zeta" => '\u03b6', // zeta
            "eta" => '\u03b7', // eta
            "theta" => '\u03b8', // theta
            "iota" => '\u03b9', // iota
            "kappa" => '\u03ba', // kappa
            "lambda" => '\u03bb', // lambda
            "mu" => '\u03bc', // mu
            "nu" => '\u03bd', // nu
            "xi" => '\u03be', // xi
            "omicron" => '\u03bf', // omicron
            "pi" => '\u03c0', // pi
            "rho" => '\u03c1', // rho
            "sigmaf" => '\u03c2', // sigmaf
            "sigma" => '\u03c3', // sigma
            "tau" => '\u03c4', // tau
            "upsilon" => '\u03c5', // upsilon
            "phi" => '\u03c6', // phi
            "chi" => '\u03c7', // chi
            "psi" => '\u03c8', // psi
            "omega" => '\u03c9', // omega
            "thetasym" => '\u03d1', // Theta symbol
            "upsih" => '\u03d2', // Upsilon symbol
            "piv" => '\u03d6', // Pi symbol
            "ensp" => '\u2002', //	En space
            "emsp" => '\u2003', //	Em space
            "thinsp" => '\u2009', //	Thin space
            "zwnj" => '\u200c', //	Zero width non-joiner
            "zwj" => '\u200d', //	Zero width joiner
            "lrm" => '\u200e', //	Left-to-right mark
            "rlm" => '\u200f', //	Right-to-left mark
            "ndash" => '\u2013', //	En dash
            "mdash" => '\u2014', //	Em dash
            "lsquo" => '\u2018', //	Left single quotation mark
            "rsquo" => '\u2019', //	Right single quotation mark
            "sbquo" => '\u201a', //	Single low-9 quotation mark
            "ldquo" => '\u201c', //	Left double quotation mark
            "rdquo" => '\u201d', //	Right double quotation mark
            "bdquo" => '\u201e', //	Double low-9 quotation mark
            "dagger" => '\u2020', //	Dagger
            "Dagger" => '\u2021', //	Double dagger
            "bull" => '\u2022', //	Bullet
            "hellip" => '\u2026', //	Horizontal ellipsis
            "permil" => '\u2030', //	Per mille
            "prime" => '\u2032', //	Minutes (Degrees)
            "Prime" => '\u2033', //	Seconds (Degrees)
            "lsaquo" => '\u2039', //	Single left angle quotation
            "rsaquo" => '\u203a', //	Single right angle quotation
            "oline" => '\u203e', //	Overline
            "euro" => '\u20ac', //	Euro
            "trade" => '\u2122', //	Trademark
            "larr" => '\u2190', //	Left arrow
            "uarr" => '\u2191', //	Up arrow
            "rarr" => '\u2192', //	Right arrow
            "darr" => '\u2193', //	Down arrow
            "harr" => '\u2194', //	Left right arrow
            "crarr" => '\u21b5', //	Carriage return arrow
            "forall" => '\u2200', //	For all
            "part" => '\u2202', //	Part
            "exist" => '\u2203', //	Exist
            "empty" => '\u2205', //	Empty
            "nabla" => '\u2207', //	Nabla
            "isin" => '\u2208', //	Is in
            "notin" => '\u2209', //	Not in
            "ni" => '\u220b', //	Ni
            "prod" => '\u220f', //	Product
            "sum" => '\u2211', //	Sum
            "minus" => '\u2212', //	Minus
            "lowast" => '\u2217', //	Asterisk (Lowast)
            "radic" => '\u221a', //	Square root
            "prop" => '\u221d', //	Proportional to
            "infin" => '\u221e', //	Infinity
            "ang" => '\u2220', //	Angle
            "and" => '\u2227', //	And
            "or" => '\u2228', //	Or
            "cap" => '\u2229', //	Cap
            "cup" => '\u222a', //	Cup
            "int" => '\u222b', //	Integral
            "there4" => '\u2234', //	Therefore
            "sim" => '\u223c', //	Similar to
            "cong" => '\u2245', //	Congurent to
            "asymp" => '\u2248', //	Almost equal
            "ne" => '\u2260', //	Not equal
            "equiv" => '\u2261', //	Equivalent
            "le" => '\u2264', //	Less or equal
            "ge" => '\u2265', //	Greater or equal
            "sub" => '\u2282', //	Subset of
            "sup" => '\u2283', //	Superset of
            "nsub" => '\u2284', //	Not subset of
            "sube" => '\u2286', //	Subset or equal
            "supe" => '\u2287', //	Superset or equal
            "oplus" => '\u2295', //	Circled plus
            "otimes" => '\u2297', //	Circled times
            "perp" => '\u22a5', //	Perpendicular
            "sdot" => '\u22c5', //	Dot operator
            "lceil" => '\u2308', //	Left ceiling
            "rceil" => '\u2309', //	Right ceiling
            "lfloor" => '\u230a', //	Left floor
            "rfloor" => '\u230b', //	Right floor
            "loz" => '\u25ca', //	Lozenge
            "spades" => '\u2660', //	Spade
            "clubs" => '\u2663', //	Club
            "hearts" => '\u2665', //	Heart
            "diams" => '\u2666', //	Diamond
            _ => NULL_CHAR
        };

        return value is not NULL_CHAR;
    }
}