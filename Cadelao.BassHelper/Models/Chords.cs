using System.Globalization;

namespace Cadelao.BassHelper.Models;

/// <summary>
/// Evidencias tonais agregadas; a cifra de uma frase nao e a nota individual do baixo.
/// Chroma = instrumento harmonico/medio; BassChroma = regiao grave da mixagem.
/// </summary>
public sealed record ChordFrame(double Time, float[] Chroma, float[] BassChroma, double Rms);

public sealed class ChordDefinition
{
    public int Root { get; }
    public bool IsMinor { get; }
    public string Name => MusicNotes.Names[Root] + (IsMinor ? "m" : "");
    public string Quality => IsMinor ? "MENOR" : "MAIOR";
    public int Third => (Root + (IsMinor ? 3 : 4)) % 12;
    public int Fifth => (Root + 7) % 12;
    public int OppositeThird => (Root + (IsMinor ? 4 : 3)) % 12;
    public string Notes => $"{MusicNotes.Names[Root]}  •  {MusicNotes.Names[Third]}  •  {MusicNotes.Names[Fifth]}";

    public ChordDefinition(int root, bool minor)
    {
        Root = (root % 12 + 12) % 12;
        IsMinor = minor;
    }

    /// <summary>
    /// Evidence is anchored by root and the differentiating THIRD; fifths and passing notes
    /// can be shared among chords. The root on bass is a prior, never sufficient alone.
    /// </summary>
    public double Score(ChordFrame frame)
    {
        double root = frame.Chroma[Root];
        double third = frame.Chroma[Third];
        double fifth = frame.Chroma[Fifth];
        double falseThird = frame.Chroma[OppositeThird];
        double bRoot = frame.BassChroma[Root];
        double bThird = frame.BassChroma[Third];
        double bFifth = frame.BassChroma[Fifth];
        double mean = 1.0 / 12.0;
        double tonal = 0.93 * (root - mean) + 1.42 * (third - mean) +
                       0.67 * (fifth - mean) - 0.55 * (falseThird - mean);
        double low = 0.43 * bRoot + 0.19 * bThird + 0.12 * bFifth;
        return tonal + low;
    }
}

public static class ChordVocabulary
{
    public static IReadOnlyList<ChordDefinition> Automatic { get; } =
        Enumerable.Range(0, 12).SelectMany(r => new[]
        {
            new ChordDefinition(r, false), new ChordDefinition(r, true)
        }).ToArray();

    /// <summary>
    /// Cifra para baixo: extensoes do violao (E7, Am7, Cmaj7) sao projetadas
    /// para TRIADES. Nao ha setimas no detector nem na tela.
    /// </summary>
    public static bool TryParse(string text, out ChordDefinition? definition)
    {
        definition = null;
        string v = text.Trim().Replace("♯", "#", StringComparison.Ordinal)
            .Replace("♭", "b", StringComparison.Ordinal).Replace("°", "dim", StringComparison.Ordinal);
        if (v.Length == 0) return false;
        int root = char.ToUpperInvariant(v[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11, _ => -1
        };
        if (root < 0) return false;
        int i = 1;
        if (i < v.Length && (v[i] == '#' || v[i] == 'b'))
        {
            root += v[i] == '#' ? 1 : -1;
            i++;
        }
        string suffix = v[i..].ToLowerInvariant();
        bool minor;
        switch (suffix)
        {
            case "": case "7": case "maj7": minor = false; break;
            case "m": case "min": case "m7": case "min7": minor = true; break;
            default: return false;
        }
        definition = new ChordDefinition(root, minor);
        return true;
    }

    public static IReadOnlyList<ChordDefinition> Parse(string text)
    {
        var items = new Dictionary<string, ChordDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in text.Split([',', ';', '|', ' ', '\t', '\r', '\n'],
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParse(raw, out ChordDefinition? chord))
                throw new FormatException($"Cifra '{raw}' nao reconhecida. Use Dm, Am, E, F# ou E7 (vira E).");
            items.TryAdd(chord!.Name, chord);
        }
        if (items.Count == 0) throw new FormatException("Informe as triades ou deixe o campo vazio para deteccao automatica.");
        return items.Values.ToArray();
    }

    /// <summary>Auto-descoberta limitada aos acordes que mais se sustentam no audio.</summary>
    public static IReadOnlyList<ChordDefinition> Discover(IReadOnlyList<ChordFrame> frames, int limit = 6)
    {
        if (frames.Count == 0) return Automatic.Take(limit).ToArray();
        var values = new double[Automatic.Count];
        var wins = new int[Automatic.Count];
        for (int i = 0; i < frames.Count; i += 3)
        {
            ChordFrame f = frames[i];
            if (f.Rms < 0.002) continue;
            int best = -1;
            double score = double.NegativeInfinity;
            for (int k = 0; k < Automatic.Count; k++)
            {
                double s = Automatic[k].Score(f);
                values[k] += Math.Max(-0.04, s);
                if (s > score) { score = s; best = k; }
            }
            if (best >= 0) wins[best]++;
        }
        // Include the most frequently evidenced states while avoiding adjacent major/minor
        // on the same root unless there is independent evidence for BOTH.
        var ranked = Enumerable.Range(0, Automatic.Count)
            .OrderByDescending(k => wins[k] * 0.60 + values[k] * 8.0)
            .ToArray();
        var selected = new List<ChordDefinition>();
        foreach (int index in ranked)
        {
            if (selected.Count >= limit) break;
            var chord = Automatic[index];
            if (wins[index] <= 0) continue;
            if (selected.Any(c => c.Root == chord.Root)) continue;
            selected.Add(chord);
        }
        return selected.Count == 0 ? Automatic.ToArray() : selected;
    }
}

public sealed class ChordSegment
{
    public double Start { get; init; }
    public double End { get; init; }
    public string Name { get; init; } = "--";
    public string RawName { get; init; } = "";
    public string? OverrideChord { get; set; }
    public string EffectiveName => OverrideChord ?? Name;
    public double Confidence { get; init; }
    public double Duration => End - Start;
}

/// <summary>
/// Decode chord *phrases*; sustained low notes, passing tones and ornamentation
/// do NOT create a new chord every 100 ms. Uses weighted smoothing, Viterbi and
/// minimum stable phrase duration. Not a source separator nor guaranteed ground truth.
/// </summary>
public static class ChordSegmenter
{
    public static List<ChordSegment> Build(IReadOnlyList<ChordFrame> frames,
        double duration, IReadOnlyList<ChordDefinition> vocabulary)
    {
        int n = frames.Count, m = vocabulary.Count;
        var output = new List<ChordSegment>();
        if (n == 0 || m == 0) return output;
        double hop = n > 1 ? Math.Max(0.04, (frames[^1].Time - frames[0].Time) / (n - 1)) : 0.10;
        int radius = Math.Max(2, (int)Math.Round(0.38 / hop));
        var energies = new double[n, m];
        var smooth = new double[n, m];
        for (int i = 0; i < n; i++)
        {
            for (int s = 0; s < m; s++) energies[i, s] = vocabulary[s].Score(frames[i]);
        }
        for (int i = 0; i < n; i++)
        {
            double total = 0;
            for (int k = Math.Max(0, i - radius); k <= Math.Min(n - 1, i + radius); k++)
            {
                double w = Math.Exp(-0.5 * Math.Pow((k - i) / Math.Max(1.2, radius * 0.63), 2));
                // mute low-energy windows so silence cannot force harmonic changes
                w *= frames[k].Rms < 0.0015 ? 0.25 : 1.0;
                total += w;
                for (int s = 0; s < m; s++) smooth[i, s] += w * energies[k, s];
            }
            for (int s = 0; s < m; s++) smooth[i, s] /= Math.Max(1e-9, total);
        }

        double changePenalty = m <= 4 ? 0.15 : 0.26;
        // Major and minor on the same root need more than transient harmonic overtones.
        var back = new ushort[n, m];
        double[] previous = new double[m];
        for (int s = 0; s < m; s++) previous[s] = smooth[0, s];
        for (int i = 1; i < n; i++)
        {
            double[] next = new double[m];
            for (int s = 0; s < m; s++)
            {
                double best = previous[s];
                int bestPrev = s;
                for (int t = 0; t < m; t++)
                {
                    if (s == t) continue;
                    double penalty = changePenalty + (vocabulary[s].Root == vocabulary[t].Root ? 0.20 : 0.0);
                    double value = previous[t] - penalty;
                    if (value > best) { best = value; bestPrev = t; }
                }
                next[s] = best + smooth[i, s];
                back[i, s] = (ushort)bestPrev;
            }
            previous = next;
        }
        int last = 0;
        for (int s = 1; s < m; s++) if (previous[s] > previous[last]) last = s;
        int[] states = new int[n];
        states[^1] = last;
        for (int i = n - 1; i > 0; i--) states[i - 1] = back[i, states[i]];

        // Short fragments are generally bass walk-ups/ornamentation or noisy frames,
        // rather than a new harmonic phrase. Remove under ~0.85 second.
        int minFrames = Math.Max(5, (int)Math.Ceiling(0.85 / hop));
        for (int pass = 0; pass < 3; pass++)
        {
            for (int a = 0; a < n;)
            {
                int b = a + 1;
                while (b < n && states[b] == states[a]) b++;
                if (b - a < minFrames && (a > 0 || b < n))
                {
                    int l = a > 0 ? states[a - 1] : -1;
                    int r = b < n ? states[b] : -1;
                    int chosen = l < 0 ? r : r < 0 ? l : l;
                    if (l >= 0 && r >= 0 && l != r)
                    {
                        double scoreL = 0, scoreR = 0;
                        for (int k = a; k < b; k++)
                        {
                            scoreL += smooth[k, l]; scoreR += smooth[k, r];
                        }
                        chosen = scoreR > scoreL ? r : l;
                    }
                    if (chosen >= 0) Array.Fill(states, chosen, a, b - a);
                }
                a = b;
            }
        }

        for (int a = 0; a < n;)
        {
            int b = a + 1;
            while (b < n && states[b] == states[a]) b++;
            double start = a == 0 ? 0 : 0.5 * (frames[a - 1].Time + frames[a].Time);
            double end = b == n ? duration : 0.5 * (frames[b - 1].Time + frames[b].Time);
            double margin = 0;
            int counted = 0;
            for (int k = a; k < b; k++)
            {
                if (frames[k].Rms < 0.002) continue;
                double runner = double.NegativeInfinity;
                for (int s = 0; s < m; s++) if (s != states[a]) runner = Math.Max(runner, smooth[k, s]);
                margin += m == 1 ? 0.12 : Math.Max(0, smooth[k, states[a]] - runner);
                counted++;
            }
            double confidence = counted == 0 ? 0.25 : Math.Clamp(0.32 + 2.8 * margin / counted, 0.25, 0.97);
            if (end - start >= 0.20)
                output.Add(new ChordSegment
                {
                    Start = Math.Max(0, start), End = Math.Min(duration, end),
                    Name = vocabulary[states[a]].Name, Confidence = confidence
                });
            a = b;
        }
        return output;
    }
}
