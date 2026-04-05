using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmkcApi.Utils
{
    /// <summary>
    /// Converts legacy ISM/GIST Devnagari (Marathi) mojibake like "¯ÖÏê´Ö…" to proper Unicode.
    /// Strategy:
    ///  1) Interpret the visible chars as bytes using candidate 8-bit encodings (Latin1/1252/437/850).
    ///  2) Decode those bytes using ISCII-Devanagari (code page 57002).
    ///  3) Pick the candidate with highest Devanagari density.
    ///  4) Normalize matras/halant (ि pre-base, Reph, nukta).
    /// </summary>
    public static class IsmMarathiConverter
    {
        // .NET Framework supports these code pages.
        private static readonly Encoding EncLatin1 = Encoding.GetEncoding(28591);
        private static readonly Encoding Enc1252 = Encoding.GetEncoding(1252);
        private static readonly Encoding Enc437 = Encoding.GetEncoding(437);
        private static readonly Encoding Enc850 = Encoding.GetEncoding(850);
        private static readonly Encoding IsciiDev = Encoding.GetEncoding(57002); // ISCII-Devnagari

        public static string ToUnicode(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // If already Unicode Devanagari, leave it.
            if (ContainsDevanagari(input)) return input;

            // Try decoding via several source encodings -> ISCII
            var cands = new[]
            {
                DecodeViaIscii(input, EncLatin1),
                DecodeViaIscii(input, Enc1252),
                DecodeViaIscii(input, Enc437),
                DecodeViaIscii(input, Enc850),
            };

            // Score by Devanagari density and choose best
            var best = cands.OrderByDescending(c => ScoreDevanagari(c)).FirstOrDefault();
            if (string.IsNullOrEmpty(best)) return input;

            // Post-normalizations for Devanagari shaping quirks
            best = NormalizeDevanagari(best);

            // If still looks wrong (very low Marathi), return original
            return ScoreDevanagari(best) >= 0.25 ? best : input;
        }

        /// <summary>Convenience wrapper with simple legacy detection.</summary>
        public static string ConvertIfLegacy(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (ContainsDevanagari(s)) return s;
            if (!LooksLegacyIsmGlyphs(s)) return s;
            return ToUnicode(s);
        }

        // ---- internals ----

        private static string DecodeViaIscii(string src, Encoding byteSource)
        {
            try
            {
                var bytes = byteSource.GetBytes(src);
                var u = IsciiDev.GetString(bytes);
                return u;
            }
            catch { return null; }
        }

        private static bool ContainsDevanagari(string s)
        {
            return s.Any(ch => ch >= '\u0900' && ch <= '\u097F');
        }

        // Heuristic: ISM mojibake typically shows lots of extended Latin like ¯ Ö Ï ê ´ Þ Û ú ¸ ü ¾ etc.
        private static bool LooksLegacyIsmGlyphs(string s)
        {
            return s.Any(ch => ch > 0x7F) && !ContainsDevanagari(s);
        }

        // Rough density: fraction of chars in U+0900..U+097F
        private static double ScoreDevanagari(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int dev = s.Count(ch => ch >= '\u0900' && ch <= '\u097F');
            return (double)dev / s.Length;
        }

        private static string NormalizeDevanagari(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            // 1) NFC normalization (helps compose vowels)
            s = s.Normalize(NormalizationForm.FormC);

            // 2) Fix misplaced short-i (ि U+093F) that sometimes appears after consonant
            // Move U+093F (ि) before the base consonant of the cluster: (C[HalantC]*)ि -> ि(C[HalantC]*)
            // This is a conservative regex; adjust if needed for complex clusters.
            s = Regex.Replace(s,
                pattern: "([\\u0915-\\u0939\\u0958-\\u095F](?:\\u094D[\\u0915-\\u0939\\u0958-\\u095F])*)\\u093F",
                replacement: "\u093F$1");

            // 3) Reph handling: ISCII sometimes yields "र्C…" in visual order but some sources give "C…र्"
            // If a word starts with "र्" followed by consonant cluster, it is already fine.
            // If you detect trailing "र्" after a cluster, rotate to front.
            s = Regex.Replace(s,
                pattern: "([\\u0915-\\u0939\\u0958-\\u095F](?:\\u094D[\\u0915-\\u0939\\u0958-\\u095F])+)\u0930\u094D",
                replacement: "\u0930\u094D$1");

            // 4) Nukta normalize: compose base+dot if decomposed (rare but safe)
            s = s.Normalize(NormalizationForm.FormC);

            return s;
        }
    }
}
