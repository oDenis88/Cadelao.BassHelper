using System.Globalization;

namespace Cadelao.BassHelper.Models;

public static class MusicNotes
{
    public static readonly string[] Names =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };

    public static string Name(int midi) => midi < 0 ? "--" : Names[(midi % 12 + 12) % 12];
    public static string NameOctave(int midi) => midi < 0 ? "--" : $"{Name(midi)}{midi / 12 - 1}";
    public static double Frequency(int midi) => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);

    public static string FormatTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.TotalHours >= 1 ? t.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                                 : t.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
    public static string FormatDetailed(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return t.ToString(@"mm\:ss\.fff", CultureInfo.InvariantCulture);
    }
}

public sealed record NoteFrame(double Time, int Midi, double FrequencyHz, double Confidence, double Rms);

public sealed class NoteSegment
{
    public double Start { get; init; }
    public double End { get; init; }
    public int Midi { get; init; }
    public int? OverrideMidi { get; set; }
    public double Confidence { get; init; }
    public double FrequencyHz { get; init; }
    public int EffectiveMidi => OverrideMidi ?? Midi;
    public double Duration => End - Start;
}

public sealed class AnalysisResult
{
    public IReadOnlyList<NoteFrame> Frames { get; }
    public IReadOnlyList<ChordFrame> ChordFrames { get; }
    public List<ChordSegment> ChordSegments { get; private set; }
    public List<NoteSegment> Segments { get; }
    public double Duration { get; }
    public bool IsComplete { get; }
    public List<double> Beats { get; set; } = new();
    public string Source { get; set; } = "native";

    public AnalysisResult(IReadOnlyList<NoteFrame> frames, double duration, bool complete,
        IReadOnlyList<ChordFrame>? chordFrames = null)
    {
        Frames = frames;
        ChordFrames = chordFrames ?? Array.Empty<ChordFrame>();
        ChordSegments = new List<ChordSegment>();
        Duration = duration;
        IsComplete = complete;
        Segments = NoteSegmenter.Build(frames, duration);
        // Frases reconstruidas UMA vez no worker/UI apos leitura completa do audio.
    }

    public void SetExternalChords(List<ChordSegment> chords) => ChordSegments = chords;

    public void RebuildChords(IReadOnlyList<ChordDefinition> vocabulary)
    {
        ChordSegments = ChordSegmenter.Build(ChordFrames, Duration, vocabulary);
    }

    public ChordSegment? AtChord(double seconds)
    {
        int lo = 0, hi = ChordSegments.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            ChordSegment seg = ChordSegments[mid];
            if (seconds < seg.Start) hi = mid - 1;
            else if (seconds >= seg.End) lo = mid + 1;
            else return seg;
        }
        return null;
    }

    public NoteSegment? At(double seconds)
    {
        int lo = 0, hi = Segments.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            NoteSegment seg = Segments[mid];
            if (seconds < seg.Start) hi = mid - 1;
            else if (seconds >= seg.End) lo = mid + 1;
            else return seg;
        }
        return null;
    }
}

public sealed record AnalysisProgress(double SecondsAnalyzed, double TotalSeconds, int FramesProcessed);

public static class NoteSegmenter
{
    public static List<NoteSegment> Build(IReadOnlyList<NoteFrame> frames, double duration)
    {
        var result = new List<NoteSegment>();
        int n = frames.Count;
        if (n == 0) return result;
        var smoothed = new int[n];
        for (int i = 0; i < n; i++)
        {
            var votes = new Dictionary<int, double>();
            for (int k = Math.Max(0, i - 2); k <= Math.Min(n - 1, i + 2); k++)
            {
                int midi = frames[k].Midi;
                double weight = (k == i ? 1.50 : Math.Abs(k - i) == 1 ? 1.0 : 0.55)
                    * (0.45 + frames[k].Confidence);
                votes[midi] = votes.GetValueOrDefault(midi) + weight;
            }
            smoothed[i] = votes.OrderByDescending(pair => pair.Value).First().Key;
        }
        // A single isolated detection is not a note: allow the surrounding note to bridge it.
        for (int i = 1; i < n - 1; i++)
            if (smoothed[i - 1] == smoothed[i + 1] && smoothed[i] != smoothed[i - 1])
                smoothed[i] = smoothed[i - 1];

        int from = 0;
        while (from < n)
        {
            int to = from + 1;
            while (to < n && smoothed[to] == smoothed[from]) to++;
            if (smoothed[from] >= 0)
            {
                double start = from == 0 ? 0 : (frames[from - 1].Time + frames[from].Time) / 2;
                double end = to == n ? duration : (frames[to - 1].Time + frames[to].Time) / 2;
                double confidence = 0, frequency = 0;
                int freqCount = 0;
                for (int j = from; j < to; j++)
                {
                    confidence += frames[j].Confidence;
                    if (frames[j].FrequencyHz > 0) { frequency += frames[j].FrequencyHz; freqCount++; }
                }
                confidence /= (to - from);
                if (end - start >= 0.14)
                    result.Add(new NoteSegment
                    {
                        Start = Math.Max(0, start), End = Math.Min(duration, end), Midi = smoothed[from],
                        Confidence = confidence, FrequencyHz = freqCount > 0 ? frequency / freqCount : MusicNotes.Frequency(smoothed[from])
                    });
            }
            from = to;
        }
        return result;
    }
}
