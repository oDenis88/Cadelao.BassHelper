namespace Cadelao.BassHelper.Models;

public sealed record BassPosition(string StringName, int Fret, int Midi)
{
    public string Describe() => Fret == 0 ? $"{StringName} solta" : $"{StringName}{Fret}";
}

public sealed class TriadFingering
{
    public BassPosition Root { get; }
    public BassPosition Third { get; }
    public BassPosition Fifth { get; }

    public TriadFingering(BassPosition root, BassPosition third, BassPosition fifth)
    {
        Root = root;
        Third = third;
        Fifth = fifth;
    }

    public string Describe(ChordDefinition chord) =>
        $"{MusicNotes.Names[chord.Root]}: {Root.Describe()}    •    " +
        $"{MusicNotes.Names[chord.Third]}: {Third.Describe()}    •    " +
        $"{MusicNotes.Names[chord.Fifth]}: {Fifth.Describe()}";
}

/// <summary>
/// Bass of four strings in standard E1-A1-D2-G2 tuning.
///
/// Brega fingering: choose a FRETTED root, preferentially on the E/A strings
/// and between frets 1 and 7, as an anchor for root-third-fifth arpeggios.
/// This means D/Dm => A5, A/Am => E5, E/Em => A7 (never E open).
/// Then find the third and fifth ABOVE that exact root with a compact,
/// non-open fingering. The chord quality changes the third, not the root.
/// These are playable SUGGESTIONS, not an audio transcription.
/// </summary>
public static class FourStringBass
{
    private static readonly (string Name, int Midi)[] Tunings =
    [ ("E", 28), ("A", 33), ("D", 38), ("G", 43) ];

    /// <summary>
    /// All note positions for information only. Open strings may be present here;
    /// they are never chosen by SuggestRoot and are not rendered as suggestions.
    /// </summary>
    public static IReadOnlyList<BassPosition> RootPositions(int pitchClass) =>
        Available((pitchClass % 12 + 12) % 12).ToArray();

    /// <summary>
    /// Deterministic brega anchor for any major/minor chord on a 4-string bass.
    /// Prefer a fretted root near the "working area" of the style: strings E/A,
    /// roughly between frets 3 and 9, and avoiding roots that are too low to make
    /// a comfortable triad shape. Example targets: D => A5, A => E5, E => A7.
    /// F/Fm should also avoid E1 and prefer the middle-area A8 anchor.
    /// </summary>
    public static BassPosition SuggestRoot(ChordDefinition chord) =>
        Available(chord.Root)
            .Where(p => (p.StringName == "E" || p.StringName == "A") &&
                        p.Fret >= 1 && p.Fret <= 12)
            .OrderBy(RootPriority)
            .ThenBy(p => p.StringName == "A" ? 0 : 1)
            .ThenBy(p => p.Fret)
            .First();


    private static double RootPriority(BassPosition p)
    {
        // Strongly prefer the practical brega hand area around the middle of the
        // neck, with the A string slightly favored for triad construction. Very
        // low positions (E1/E2, etc.) are heavily penalized because they make the
        // full triad less useful in this repertoire.
        int target = p.StringName == "A" ? 6 : 5;
        double score = Math.Abs(p.Fret - target);

        if (p.StringName == "A") score -= 0.45;
        else score += 0.10;

        // Avoid the extreme low area even if the root exists there.
        if (p.Fret <= 2) score += 4.5;
        else if (p.Fret == 3) score += 1.1;

        // The typical brega region lives mostly in frets 4..8.
        if (p.Fret < 4) score += (4 - p.Fret) * 0.8;
        if (p.Fret > 9) score += (p.Fret - 9) * 0.9;

        return score;
    }

    public static TriadFingering Suggest(ChordDefinition chord)
    {
        BassPosition root = SuggestRoot(chord);
        int interval = chord.IsMinor ? 3 : 4;
        BassPosition[] thirds = Available(chord.Third)
            .Where(p => p.Midi == root.Midi + interval && p.Fret > 0)
            .ToArray();
        BassPosition[] fifths = Available(chord.Fifth)
            .Where(p => p.Midi == root.Midi + 7 && p.Fret > 0)
            .ToArray();

        TriadFingering? best = null;
        double bestScore = double.PositiveInfinity;
        foreach (BassPosition third in thirds)
        foreach (BassPosition fifth in fifths)
        {
            int minFret = Math.Min(root.Fret, Math.Min(third.Fret, fifth.Fret));
            int maxFret = Math.Max(root.Fret, Math.Max(third.Fret, fifth.Fret));
            int span = maxFret - minFret;

            // Prefer tight triad movements near the anchored hand position,
            // using nearby strings and avoiding high-fret travel when possible.
            double score =
                0.35 * span +
                0.28 * (Math.Abs(StringIndex(root) - StringIndex(third)) +
                        Math.Abs(StringIndex(root) - StringIndex(fifth))) +
                0.08 * (root.Fret + third.Fret + fifth.Fret) +
                0.22 * Math.Max(0, maxFret - 7) +
                0.30 * Math.Max(0, span - 4);

            if (score < bestScore)
            {
                bestScore = score;
                best = new TriadFingering(root, third, fifth);
            }
        }

        return best ?? throw new InvalidOperationException(
            "Nao foi possivel encontrar uma triade fretted completa para o baixo de 4 cordas.");
    }

    private static IEnumerable<BassPosition> Available(int pitchClass)
    {
        foreach (var (name, midi) in Tunings)
        for (int fret = 0; fret <= 12; fret++)
            if ((midi + fret) % 12 == pitchClass)
                yield return new BassPosition(name, fret, midi + fret);
    }

    private static int StringIndex(BassPosition p)
    {
        for (int i = 0; i < Tunings.Length; i++)
            if (Tunings[i].Name == p.StringName) return i;
        return 0;
    }
}
