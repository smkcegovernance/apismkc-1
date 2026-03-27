using System;
using Com.Cdacindia.Gist.NetISMConverter;

namespace SmkcApi.Utils
{
    /// <summary>
    /// Wrapper for NetISMConverter.dll to convert ISM/ISFOC encoded Marathi text to Unicode
    /// Uses Com.Cdacindia.Gist.NetISMConverter library
    /// </summary>
    public static class NetIsmConverter
    {
        private static readonly Com.Cdacindia.Gist.NetISMConverter.Converter _converter = new Com.Cdacindia.Gist.NetISMConverter.Converter();
        private const string DvbwFont = "DVBW";
        private const string DvbnFont = "DVBN";

        /// <summary>
        /// Convert ISM/ISFOC encoded text to Unicode using NetISMConverter.dll
        /// </summary>
        /// <param name="isfocText">ISM/ISFOC encoded text</param>
        /// <returns>Unicode Marathi text</returns>
        public static string ConvertToUnicode(string isfocText)
        {
            if (isfocText == null)
                return null;

            if (string.IsNullOrEmpty(isfocText))
                return isfocText;

            try
            {
                // Check if text is already Unicode
                bool isUnicode = System.Text.Encoding.GetEncoding(0).GetString(System.Text.Encoding.GetEncoding(0).GetBytes(isfocText)) != isfocText;
                
                if (isUnicode)
                {
                    // Text is already Unicode, return as-is
                    return isfocText;
                }
                else
                {
                    // Text is ISFOC, convert to Unicode
                    return _converter.ISFOC_To_Unicode(isfocText, "DVBN");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[NetIsmConverter] Failed to convert: {ex.Message}");
                // Return original text if conversion fails
                return isfocText;
            }
        }

        /// <summary>
        /// Convert text if it appears to be ISM/ISFOC encoded
        /// Based on your existing implementation pattern
        /// </summary>
        /// <param name="text">Text to check and convert</param>
        /// <returns>Converted Unicode text or original if already Unicode</returns>
        public static string ConvertIfNeeded(string text)
        {
            if (text == null)
                return null;

            if (string.IsNullOrEmpty(text))
                return text;

            try
            {
                // If the value already contains Devanagari Unicode characters, keep it unchanged.
                if (ContainsDevanagari(text))
                    return text;

                // 1) Pure .NET DVBW lookup table — works without ismapi.dll dependency.
                var dvbwDirect = DvbwConverter.Convert(text);
                if (LooksLikeUnicodeMarathi(dvbwDirect))
                    return dvbwDirect;

                // 2) NetISM DLL path — only works when ismapi.dll is deployed alongside NetISMConverter.dll.
                try
                {
                    var dvbw = _converter.ISFOC_To_Unicode(text, DvbwFont);
                    if (LooksLikeUnicodeMarathi(dvbw))
                        return dvbw;
                }
                catch { /* ismapi.dll not present on this machine — expected in dev */ }

                try
                {
                    var dvbn = _converter.ISFOC_To_Unicode(text, DvbnFont);
                    if (LooksLikeUnicodeMarathi(dvbn))
                        return dvbn;
                }
                catch { /* ismapi.dll not present — expected in dev */ }

                // Conversion did not produce a reliable Unicode result.
                return text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[NetIsmConverter] Failed to convert: {ex.Message}");
                // Return original text if conversion fails
                return text;
            }
        }

        private static bool ContainsDevanagari(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch >= '\u0900' && ch <= '\u097F')
                    return true;
            }

            return false;
        }

        private static bool LooksLikeUnicodeMarathi(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            int devCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch >= '\u0900' && ch <= '\u097F')
                    devCount++;
            }

            return devCount >= 2;
        }

        /// <summary>
        /// Get ISM string from Unicode (reverse conversion)
        /// </summary>
        /// <param name="unicodeText">Unicode Marathi text</param>
        /// <returns>ISM/ISFOC encoded text</returns>
        public static string GetISMString(string unicodeText)
        {
            if (unicodeText == null)
                return null;

            if (string.IsNullOrEmpty(unicodeText))
                return unicodeText;

            try
            {
                bool isUnicode = System.Text.Encoding.GetEncoding(0).GetString(System.Text.Encoding.GetEncoding(0).GetBytes(unicodeText)) != unicodeText;
                
                if (isUnicode)
                {
                    return _converter.Unicode_To_ISFOC(unicodeText, "DVBN");
                }
                else
                {
                    return _converter.ISFOC_To_Unicode(unicodeText, "DVBN");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError($"[NetIsmConverter] GetISMString failed: {ex.Message}");
                return unicodeText;
            }
        }
    }
}
