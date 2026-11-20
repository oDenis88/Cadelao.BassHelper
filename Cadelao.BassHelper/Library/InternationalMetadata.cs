using System.Globalization;
using System.Text;

namespace Cadelao.BassHelper.Library;

/// <summary>
/// Display-only repair for *recognizable* legacy ID3 text whose bytes were
/// incorrectly declared Latin-1/Windows-1252. Never writes or converts tags.
/// Correct Unicode (Cyrillic, CJK, Arabic, etc.) passes through unchanged.
/// </summary>
internal static class InternationalMetadata
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Encoding? Windows1251;
    private static readonly Encoding? Windows1252;
    private static readonly Encoding? ShiftJis;
    private static readonly Encoding? Gb18030;

    static InternationalMetadata()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Windows1251 = Encoding.GetEncoding(1251);
            Windows1252 = Encoding.GetEncoding(1252,
                EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            ShiftJis = Encoding.GetEncoding(932,
                EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            Gb18030 = Encoding.GetEncoding(54936,
                EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
        catch (ArgumentException) { /* Original text remains untouched. */ }
        catch (NotSupportedException) { /* Original text remains untouched. */ }
    }

    public static string Display(string? original)
    {
        if (string.IsNullOrWhiteSpace(original)) return original ?? "";
        // Correct Russian, Ukrainian, Japanese, Chinese, Korean, Greek etc.
        // are never candidates for legacy Latin-codepage reinterpretation.
        if (ContainsNonLatinScript(original)) return original;

        if (TryUtf8FromLatin1(original, out var fixedUtf8)) return fixedUtf8;
        if (TryLegacyCyrillic(original, out var fixedCyrillic)) return fixedCyrillic;
        if (TryLegacyJapanese(original, out var fixedJapanese)) return fixedJapanese;
        if (TryLegacyChinese(original, out var fixedChinese)) return fixedChinese;
        return original;
    }

    private static bool TryUtf8FromLatin1(string input, out string corrected)
    {
        corrected = "";
        if (input.Length < 3 || input.Count(c => c > 127) < 2) return false;
        try
        {
            // ID3 Latin-1 often contains the literal UTF-8 bytes; some taggers
            // instead interpret those bytes as Windows-1252 punctuation.
            if (TryDecodeUtf8(Encoding.Latin1, input, out corrected)) return true;
            if (Windows1252 is not null && TryDecodeUtf8(Windows1252, input, out corrected)) return true;
        }
        catch (EncoderFallbackException) { }
        return false;
    }

    private static bool TryDecodeUtf8(Encoding source, string input, out string corrected)
    {
        corrected = "";
        byte[] bytes = source.GetBytes(input);
        // Never replace a character that cannot round-trip in the source codepage.
        if (!string.Equals(source.GetString(bytes), input, StringComparison.Ordinal)) return false;
        try
        {
            string candidate = StrictUtf8.GetString(bytes);
            if (candidate == input || candidate.Contains('\uFFFD')) return false;
            int genuineScriptLetters = candidate.Count(IsNonLatinScript);
            bool characteristicMarker = input.Contains('Ã') || input.Contains('Â') ||
                                        input.Contains('Ð') || input.Contains('Ñ') ||
                                        input.Contains('ã') || input.Contains('â');
            // A recognizable non-Latin word or the characteristic UTF-8 ->
            // Latin-1 digraphs. This deliberately rejects uncertain guesses.
            if ((genuineScriptLetters >= 2 && CharacterRatio(candidate) >= 0.45) ||
                (characteristicMarker && CharacterRatio(candidate) >= 0.60 &&
                 candidate.Count(c => c > 127) < input.Count(c => c > 127)))
            {
                corrected = candidate;
                return true;
            }
        }
        catch (DecoderFallbackException) { }
        return false;
    }

    private static bool TryLegacyCyrillic(string input, out string corrected)
    {
        corrected = "";
        if (Windows1251 is null || input.Length < 3) return false;
        // E.g. 'ÏÐÎÑÒÈ' -> 'ПРОСТИ'. Do not convert normal accented Latin
        // words or arbitrary East Asian legacy byte streams.
        int extended = input.Count(c => c >= '\u00C0' && c <= '\u00FF');
        int nonAscii = input.Count(c => c > 127);
        if (extended < 3 || nonAscii == 0 || extended * 10 < nonAscii * 8) return false;
        if (input.Any(c => c is >= '\u0080' and <= '\u009F' || c > '\u00FF')) return false;
        string candidate = Windows1251.GetString(Encoding.Latin1.GetBytes(input));
        int cyrillic = candidate.Count(IsCyrillic);
        if (cyrillic < 3 || cyrillic * 10 < extended * 7) return false;
        if (!candidate.Any(c => "АЕЁИОУЫЭЮЯаеёиоуыэюяІіЇїЄє".Contains(c))) return false;
        corrected = candidate;
        return true;
    }

    private static bool TryLegacyJapanese(string input, out string corrected)
    {
        corrected = "";
        // Shift-JIS frequently exposes 0x82/0x83 as C1 controls or cp1252
        // '‚'/'ƒ' when a legacy tag is mislabeled as Latin-1.
        if (ShiftJis is null || !input.Any(c =>
                c is >= '\u0081' and <= '\u009F' || c is 'ƒ' or '‚')) return false;
        return TryEastAsianCodePage(input, ShiftJis, candidate =>
            candidate.Count(c => c is >= '\u3040' and <= '\u30FF' ||
                                  c is >= '\u4E00' and <= '\u9FFF') >= 2,
            out corrected);
    }

    private static bool TryLegacyChinese(string input, out string corrected)
    {
        corrected = "";
        if (Gb18030 is null || input.Length < 4 ||
            input.Count(c => c is >= '\u00B0' and <= '\u00FF') < 4 ||
            input.Any(c => c is >= '\u0080' and <= '\u009F' || c > '\u00FF'))
            return false;
        // Only complete, dense legacy high-byte sequences, never ordinary
        // one- or two-accent Latin names. No alteration to the source file.
        return TryEastAsianCodePage(input, Gb18030, candidate =>
            candidate.Count(c => c is >= '\u4E00' and <= '\u9FFF') >= 2,
            out corrected);
    }

    private static bool TryEastAsianCodePage(string input, Encoding target,
        Func<string, bool> acceptable, out string corrected)
    {
        corrected = "";
        foreach (var source in new Encoding?[] { Encoding.Latin1, Windows1252 })
        {
            if (source is null) continue;
            try
            {
                var bytes = source.GetBytes(input);
                if (source.GetString(bytes) != input) continue;
                string candidate = target.GetString(bytes);
                if (!candidate.Contains('\uFFFD') && acceptable(candidate) &&
                    CharacterRatio(candidate) >= 0.50)
                {
                    corrected = candidate;
                    return true;
                }
            }
            catch (DecoderFallbackException) { }
            catch (EncoderFallbackException) { }
        }
        return false;
    }

    private static double CharacterRatio(string text)
    {
        int letters = text.Count(c => char.IsLetterOrDigit(c));
        return letters / (double)Math.Max(1, text.Length);
    }

    private static bool IsCyrillic(char c) => c is >= '\u0400' and <= '\u052F';

    private static bool ContainsNonLatinScript(string value) => value.Any(IsNonLatinScript);

    private static bool IsNonLatinScript(char c) =>
        IsCyrillic(c) || c is >= '\u0370' and <= '\u03FF' ||
        c is >= '\u0590' and <= '\u08FF' ||
        c is >= '\u0900' and <= '\u0FFF' ||
        c is >= '\u2E80' and <= '\uA7FF' ||
        c is >= '\uAC00' and <= '\uD7AF';
}
