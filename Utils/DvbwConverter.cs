using System;
using System.Collections.Generic;
using System.Text;

namespace SmkcApi.Utils
{
    /// <summary>
    /// Pure .NET 4.5 converter for DVBW (C-DAC ISM) font-encoded Devanagari text to Unicode.
    /// Implements the DVBW glyph-byte → Unicode character lookup without ismapi.dll.
    ///
    /// DVBW encoding structure (empirically determined from 13 Oracle DB samples):
    ///
    ///   CONSONANT GROUPS:
    ///     head + 0xD6  → consonant (straight-stem group: क ख ग घ … ह)
    ///     head + 0xFC  → consonant (curved-base group:   ट ठ ड ढ र ह)
    ///     head + 0xFA  → consonant (FA group:            क उ ऊ इ)
    ///     head + 0xFD  → consonant+matra (FD group:      रु)
    ///     head + 0xFB  → consonant (FB group:            ळ)
    ///
    ///   THREE-BYTE PATTERNS (consonant with embedded matra):
    ///     head + 0xE3 + suffix  → consonant + ु  (e.g. DB E3 FA = कु)
    ///     head + 0xE9 + suffix  → consonant + ृ  (e.g. DB E9 FA = कृ)
    ///     head + 0xEA + 0xFC    → consonant + े  (e.g. A4 EA FC = दे)
    ///
    ///   TWO-BYTE MATRAS / VOWELS:
    ///     D6 + EA = ो,  D6 + EF = ों
    ///
    ///   SINGLE-BYTE MATRAS:
    ///     D6=ा  DF=ी  E0=ि  E1=ु  E3=ु(alt)  E6=ू  DE=ू  E7=ौ  E8=्
    ///     EA=े  E4=े   E5=ै  D3=ं  C4=ं  C5=ः  A1=ँ
    ///     D7=ि (pre-consonant form — treated same as E0)
    ///     CF=्र (halant+ra ligature, pre-positioned before consonant)
    ///     EB=े (e-matra variant, post-positioned)
    ///     8C=ं (anusvara variant, pre-positioned before next consonant)
    ///     EF=ों (o+anusvara, pre-positioned — same as D6 EF two-byte)
    ///
    ///   STANDALONE VOWELS (single byte):
    ///     86=अ  87=इ  89=ऊ
    ///
    /// The input string is expected to arrive already decoded as Windows-1252
    /// (Oracle.ManagedDataAccess returns raw ISM bytes read through Windows-1252).
    /// </summary>
    public static class DvbwConverter
    {
        private static readonly Encoding Win1252 = Encoding.GetEncoding(1252);

        // Two-byte patterns: key = (byte1 << 8) | byte2, value = Unicode string.
        // All entries confirmed from empirical analysis of Oracle DB name samples.
        private static readonly Dictionary<int, string> TwoByteMap = new Dictionary<int, string>
        {
            // ── D6-group consonants (head + 0xD6) ─────────────────────────────
            { 0x9AD6, "\u0915" },  // क  ka
            { 0x96D6, "\u0916" },  // ख  kha
            { 0x97D6, "\u0917" },  // ग  ga
            { 0x98D6, "\u0918" },  // घ  gha
            { 0x95D6, "\u091C" },  // ज  ja  (alt code — confirmed from विजय)
            { 0xA2D6, "\u0924\u094D\u0924" },  // त्त  tta-ligature (confirmed: दत्तात्रय in 48851)
            { 0xA3D6, "\u0925" },  // थ  tha  (confirmed: नाथ in 48855)
            { 0xA6D6, "\u091C" },  // ज  ja
            { 0xA7D6, "\u091D" },  // झ  jha
            { 0xA8D6, "\u091E" },  // ञ  nya
            { 0xC1D6, "\u0923" },  // ण  ṇa
            { 0x9FD6, "\u0924" },  // त  ta   (confirmed: ताराबाई)
            { 0xA0D6, "\u0925" },  // थ  tha
            { 0xA4D6, "\u0926" },  // द  da
            { 0xA5D6, "\u0927" },  // ध  dha
            { 0xA9D6, "\u0928" },  // न  na
            { 0xAED6, "\u0928" },  // न  na  (alt — confirmed: भगवान, नायकवडी)
            { 0xAFD6, "\u092A" },  // प  pa   (confirmed: पाटील)
            { 0xB0D6, "\u092B" },  // फ  pha
            { 0xB2D6, "\u092C" },  // ब  ba   (confirmed: ताराबाई)
            { 0xB3D6, "\u092D" },  // भ  bha  (confirmed: भाऊराव)
            { 0xB4D6, "\u092E" },  // म  ma
            { 0xB5D6, "\u092F" },  // य  ya
            { 0xBBD6, "\u0932" },  // ल  la   (confirmed: पाटील)
            { 0xBCD6, "\u0933" },  // ळ  ḷa   (Marathi)
            { 0xBED6, "\u0935" },  // व  va   (confirmed: भाऊराव)
            { 0xBFD6, "\u0936" },  // श  sha
            { 0xC0D6, "\u0937" },  // ष  ṣha
            { 0xC2D6, "\u0938" },  // स  sa
            { 0xC3D6, "\u0938" },  // स  sa  (alt — confirmed: वसंत in 48848)
            { 0xC4D6, "\u0939" },  // ह  ha  (alt D6-group)
            { 0xDDD6, "\u0917" },  // ग  ga  (alt — confirmed: भगवान)
            // Special ligatures (D6-group)
            { 0xA1D6, "\u0924\u094D\u0930" },  // त्र  tra-ligature (confirmed: पत्रकार in 48855)

            // ── Additional D6-group consonants (confirmed from samples) ────────
            { 0xDED6, "\u0923" },  // ण  ṇa  (confirmed: रेवणकर in 48850)
            { 0xDCD6, "\u0916" },  // ख  kha  (confirmed: शेख in 48845)

            // ── FC-group consonants (head + 0xFC) ─────────────────────────────
            { 0x99FC, "\u091F" },  // ट  ṭa   (confirmed: पाटील)
            { 0x9AFC, "\u0920" },  // ठ  ṭha
            { 0x9BFC, "\u0921" },  // ड  ḍa   (confirmed: नायकवडी — CORRECTED from ठ)
            { 0x9CFC, "\u0922" },  // ढ  ḍha
            { 0xB8FC, "\u0930" },  // र  ra   (confirmed: ताराबाई)
            { 0xC6FC, "\u0939" },  // ह  ha   (confirmed: महादेव in 48852)
            { 0xA4FC, "\u0926" },  // द  da  (FC-group — confirmed: दत्तात्रय, दुरुस्ती)

            // ── FA-group consonants (head + 0xFA) ─────────────────────────────
            { 0xDBFA, "\u0915" },  // क  ka  (FA-group — confirmed: तुकाराम, इसाक, शिराळकर)
            { 0x88FA, "\u0907" },  // इ  i   (vowel)
            { 0x89FA, "\u090A" },  // ऊ  ū   (vowel — confirmed: भाऊराव)
            { 0x8AFA, "\u0909" },  // उ  u   (vowel — confirmed: उर्फ)

            // ── FB-group (head + 0xFB) ─────────────────────────────────────────
            { 0xF4FB, "\u0933" },  // ळ  ḷa  (FB-group — confirmed: शिराळकर, माळी)

            // ── FD-group (head + 0xFD) ─────────────────────────────────────────
            { 0xB9FD, "\u0930\u0941" },  // रु  ru  (confirmed: दुरुस्ती in 48852)

            // ── D4-group (head + 0xD4) ─────────────────────────────────────────
            { 0x87D4, "\u0908" },  // ई  ī   (confirmed: ताराबाई)
            { 0xB1D4, "\u0930\u094D" },  // र्  r-halant (confirmed: उर्फ in 48855)

            // ── Two-byte standalone vowels ─────────────────────────────────────
            { 0x85A5, "\u0905" },  // अ  a
            { 0x86D6, "\u0906" },  // आ  ā   (confirmed: 48840)

            // ── Two-byte matra patterns ────────────────────────────────────────
            { 0xD6EA, "\u094B" },  // ो  o-matra  (confirmed: मोहन in 48850)
            { 0xD6EF, "\u094B\u0902" },  // ों  o+anusvara  (confirmed: गोंडा in 48855)
        };

        // Single-byte patterns.
        private static readonly Dictionary<byte, string> OneByteMap = new Dictionary<byte, string>
        {
            // ── Vowel matras ───────────────────────────────────────────────────
            { 0xD6, "\u093E" },  // ा  ā-matra           (confirmed: ताराबाई)
            { 0xDF, "\u0940" },  // ी  ī-matra            (confirmed: ताराबाई)
            { 0xE0, "\u093F" },  // ि  i-matra
            { 0xE1, "\u0941" },  // ु  u-matra
            { 0xE3, "\u0941" },  // ु  u-matra (alt — confirmed: E3=ु from तुकाराम, गुलाब)
            { 0xE9, "\u0943" },  // ृ  ṛ-matra (appears inside three-byte: HEAD E9 SUFFIX)
            { 0xDE, "\u0942" },  // ू  ū-matra
            { 0xE2, "\u0942" },  // ू  ū-matra (alt)
            { 0xE6, "\u0942" },  // ू  ū-matra (alt2 — confirmed: बाबूराव in 48850)
            { 0xE4, "\u0947" },  // े  e-matra
            { 0xEA, "\u0947" },  // े  e-matra (alt)
            { 0xEB, "\u0947" },  // े  e-matra (alt2 — confirmed: नेमगेंडा, कलगांडा)
            { 0xE5, "\u0948" },  // ै  ai-matra
            { 0xE7, "\u094C" },  // ौ  au-matra
            { 0xE8, "\u094D" },  // ्  halant/virama
            // ── Anusvara / chandrabindu ────────────────────────────────────────
            { 0xC4, "\u0902" },  // ं  anusvara
            { 0xD3, "\u0902" },  // ं  anusvara (alt — confirmed: शांत, वसंत)
            { 0x8C, "\u0902" },  // ं  anusvara (pre-positioned variant — confirmed: आकांताई)
            { 0xC5, "\u0903" },  // ः  visarga
            { 0xA1, "\u0901" },  // ँ  chandrabindu
            // ── Pre-consonant i-matra (stored before the consonant in font) ───
            { 0xD7, "\u093F" },  // ि  i-matra pre-form (confirmed: विजय, शिराळकर)
            // ── Pre-consonant conjunct forms ───────────────────────────────────
            { 0xCF, "\u094D\u0930" },  // ्र  halant+ra (confirmed: प्रकाश in 48846)
            // ── Single-byte standalone vowels ───────────────────────────────
            { 0x86, "\u0905" },  // अ  a  (standalone — confirmed: अली in 48843)
            { 0x87, "\u0907" },  // इ  i  (standalone — confirmed: इसाक in 48845)
            // ── Half-form consonants (no D6/FC suffix; two-byte takes priority when followed by D6) ──
            { 0xB2, "\u092C\u094D" },  // ब्  ba-halant (confirmed: अब्दुल in 48843)
            { 0xC2, "\u0937\u094D" },  // ष्  sha-halant half-form (C2 alone; C2D6=स takes priority — does not exist)
            { 0xC3, "\u0938\u094D" },  // स्  sa-halant half-form (C3 alone; C3D6=स two-byte takes priority)
            { 0x95, "\u091C\u094D" },  // ज्  ja-halant half-form (95 alone; 95D6=ज two-byte takes priority)
            // ── Single-byte ligatures ────────────────────────────────────────
            { 0xF5, "\u0915\u094D\u0937\u094D" },  // क्ष्  ksha-halant (confirmed: लक्ष्मण in 48849)
            { 0xFA, "\u092B" },  // फ  pha standalone (confirmed: उर्फ in 48855: B1D4+FA = र्फ)
        };

        // Head bytes that form valid consonants with a suffix (for 3-byte detection)
        private static readonly HashSet<byte> ConsonantHeads = new HashSet<byte>
        {
            0x9A,0x96,0x97,0x98,0xA2,0xA3,0xA6,0xA7,0xA8,0xC1,
            0x9F,0xA0,0xA4,0xA5,0xA9,0xAE,0xAF,0xB0,0xB2,0xB3,
            0xB4,0xB5,0xBB,0xBC,0xBE,0xBF,0xC0,0xC2,0xC3,0xC4,
            0xDD,0x99,0x9B,0x9C,0xB8,0xC6,0xDB,0x95,0xA1
        };

        /// <summary>
        /// Convert DVBW font-encoded text (as .NET string with Win-1252 codepoints) to Unicode Devanagari.
        /// </summary>
        public static string Convert(string dvbwText)
        {
            if (string.IsNullOrEmpty(dvbwText))
                return dvbwText;

            byte[] bytes = Win1252.GetBytes(dvbwText);
            var sb = new StringBuilder(bytes.Length * 2);
            int i = 0;

            while (i < bytes.Length)
            {
                // 1. Three-byte: HEAD + 0xE9(ृ) + SUFFIX  →  consonant + ृ
                if (i + 2 < bytes.Length && bytes[i + 1] == 0xE9)
                {
                    int key3 = (bytes[i] << 8) | bytes[i + 2];
                    string r3;
                    if (TwoByteMap.TryGetValue(key3, out r3))
                    {
                        sb.Append(r3);
                        sb.Append("\u0943"); // ृ
                        i += 3;
                        continue;
                    }
                }

                // 2. Three-byte: HEAD + 0xE3(ु) + SUFFIX  →  consonant + ु
                if (i + 2 < bytes.Length && bytes[i + 1] == 0xE3)
                {
                    int key3 = (bytes[i] << 8) | bytes[i + 2];
                    string r3;
                    if (TwoByteMap.TryGetValue(key3, out r3))
                    {
                        sb.Append(r3);
                        sb.Append("\u0941"); // ु
                        i += 3;
                        continue;
                    }
                }

                // 3. Three-byte: HEAD + 0xEA + 0xFC  →  consonant (FC-group) + े
                if (i + 2 < bytes.Length && bytes[i + 1] == 0xEA && bytes[i + 2] == 0xFC)
                {
                    int keyFC = (bytes[i] << 8) | 0xFC;
                    string r3;
                    if (TwoByteMap.TryGetValue(keyFC, out r3))
                    {
                        sb.Append(r3);
                        sb.Append("\u0947"); // े
                        i += 3;
                        continue;
                    }
                }

                // 4. Two-byte lookup
                if (i + 1 < bytes.Length)
                {
                    int key2 = (bytes[i] << 8) | bytes[i + 1];
                    string r2;
                    if (TwoByteMap.TryGetValue(key2, out r2))
                    {
                        sb.Append(r2);
                        i += 2;
                        continue;
                    }
                }

                // 5. Single-byte Devanagari mapping
                string r1;
                if (OneByteMap.TryGetValue(bytes[i], out r1))
                {
                    // D7 is a pre-positioned i-matra: must emit ि AFTER the next token
                    if (bytes[i] == 0xD7)
                    {
                        i++; // skip D7
                        if (i < bytes.Length)
                        {
                            bool consumed = false;
                            // Try next two-byte token first
                            if (i + 1 < bytes.Length)
                            {
                                int k2 = (bytes[i] << 8) | bytes[i + 1];
                                string vv;
                                if (TwoByteMap.TryGetValue(k2, out vv))
                                {
                                    sb.Append(vv);
                                    sb.Append("\u093F"); // ि after consonant
                                    i += 2;
                                    consumed = true;
                                }
                            }
                            if (!consumed)
                            {
                                string vv;
                                if (OneByteMap.TryGetValue(bytes[i], out vv))
                                {
                                    sb.Append(vv);
                                    sb.Append("\u093F");
                                    i++;
                                }
                                else
                                {
                                    sb.Append("\u093F");
                                }
                            }
                        }
                        continue;
                    }
                    sb.Append(r1);
                    i++;
                    continue;
                }

                // 6. ASCII passthrough
                if (bytes[i] < 0x80)
                {
                    sb.Append((char)bytes[i]);
                    i++;
                    continue;
                }

                // 7. Unknown high byte — skip silently
                i++;
            }

            return sb.ToString();
        }
    }
}
