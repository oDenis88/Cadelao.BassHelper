using System.Text.RegularExpressions;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.Analysis;

public sealed record ChordinoEvent(double Time, string Label);
public sealed record ChordinoCache(string Engine, double Duration, List<ChordinoEvent> Events, List<double> Beats);

/// <summary>
/// Keep EVERY native Chordino transition. No 0.85s pruning, fake confidence,
/// or limiting vocabulary to Dm/Am/E. Seventh extensions are removed only on
/// the display layer. This preserves the source transcription for auditing.
/// </summary>
public static class ChordinoTimeline
{
    public static List<ChordSegment> Build(IReadOnlyList<ChordinoEvent> source,
        double duration, IReadOnlyList<double>? beatTimes = null)
    {
        var output = new List<ChordSegment>();
        var events = source.Where(x => x.Time >= 0 && x.Time < duration + 0.01)
            .OrderBy(x => x.Time).ToList();
        if (events.Count == 0) return output;
        for (int i = 0; i < events.Count; i++)
        {
            var e = events[i];
            double start = Math.Clamp(e.Time, 0, duration);
            double end = i + 1 < events.Count ? Math.Clamp(events[i + 1].Time, start, duration) : duration;
            if (end <= start + 1e-5) continue;
            bool valid = TryReduceToBassTriad(e.Label, out string name);
            if (!valid) name = "--";
            output.Add(new ChordSegment { Start = start, End = end, Name = name,
                RawName = e.Label, Confidence = -1 });
        }
        // Chordino outputs change points. Fill the interval before the first estimate
        // with N (rather than incorrectly backdating a detected chord to time zero).
        if (output.Count > 0 && output[0].Start > 0.001)
            output.Insert(0, new ChordSegment { Start = 0, End = output[0].Start,
                Name = "--", RawName = "N", Confidence = -1 });
        return output;
    }

    public static bool TryReduceToBassTriad(string raw, out string triad)
    {
        triad = "--";
        string s = raw.Trim().Replace('♭', 'b').Replace('♯', '#');
        if (string.IsNullOrEmpty(s) || s is "N" or "X" or "no_chord") return false;
        string main = s.Split('/')[0];
        // Harte syntax, e.g. E:7, A:min, F:maj7, D:min7.
        int colon = main.IndexOf(':');
        if (colon >= 0)
        {
            string root = main[..colon];
            string quality = main[(colon + 1)..].ToLowerInvariant();
            if (quality.StartsWith("min", StringComparison.Ordinal) || quality == "m" || quality.StartsWith("m7", StringComparison.Ordinal))
                main = root + "m";
            else if (quality is "maj" or "7" or "maj7" or "6" or "9" or "dom7") main = root;
            else return false; // diminished/aug/sus should not be misrepresented as majors
        }
        else
        {
            // No 7ths or extensions in our four-string bass teaching display.
            // A diminished triad is NOT a minor triad (different fifth).
            if (Regex.IsMatch(main, "dim|aug|sus|ø", RegexOptions.IgnoreCase)) return false;
            main = Regex.Replace(main, @"(?i)(maj7|m7|7|6|9)$", m =>
                m.Value.StartsWith('m') && !m.Value.StartsWith("maj", StringComparison.OrdinalIgnoreCase) ? "m" : "");
        }
        if (!ChordVocabulary.TryParse(main, out var chord)) return false;
        triad = chord!.Name;
        return true;
    }
}
