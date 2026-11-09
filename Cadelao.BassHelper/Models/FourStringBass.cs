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
    { Root = root; Third = third; Fifth = fifth; }
    public string Describe(ChordDefinition chord) =>
        $"{MusicNotes.Names[chord.Root]}: {Root.Describe()}    •    " +
        $"{MusicNotes.Names[chord.Third]}: {Third.Describe()}    •    " +
        $"{MusicNotes.Names[chord.Fifth]}: {Fifth.Describe()}";
}

/// <summary>
/// Standard 4-string electric bass, E1 A1 D2 G2. Proposes a convenient compact
/// root-third-fifth exercise shape; NOT a transcription of the original recording.
/// </summary>
public static class FourStringBass
{
    private static readonly (string Name, int Midi)[] Tunings =
    [ ("E", 28), ("A", 33), ("D", 38), ("G", 43) ];

    public static TriadFingering Suggest(ChordDefinition chord)
    {
        var rootCandidates = Available(chord.Root)
            .Where(p => p.Midi <= 43 && p.StringName != "G" && p.Fret <= 10)
            .ToArray();
        if (rootCandidates.Length == 0) rootCandidates = Available(chord.Root).ToArray();
        TriadFingering? best = null;
        double bestScore = double.PositiveInfinity;
        foreach (var root in rootCandidates)
        {
            // first arpeggio rising to the third and fifth, within four strings
            var thirds = Available(chord.Third).Where(p => p.Midi == root.Midi + (chord.IsMinor ? 3 : 4));
            var fifths = Available(chord.Fifth).Where(p => p.Midi == root.Midi + 7);
            foreach (var third in thirds)
            foreach (var fifth in fifths)
            {
                int span = Math.Max(root.Fret, Math.Max(third.Fret, fifth.Fret)) -
                           Math.Min(root.Fret, Math.Min(third.Fret, fifth.Fret));
                double fretPenalty = 0.18 * (root.Fret + third.Fret + fifth.Fret) +
                                     0.12 * span + 0.18 * Math.Max(0, span - 5);
                double stringPenalty = 0.35 * (Math.Abs(StringIndex(root) - StringIndex(third)) +
                                              Math.Abs(StringIndex(root) - StringIndex(fifth)));
                double score = fretPenalty + stringPenalty + 0.11 * (root.Midi - 28);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new TriadFingering(root, third, fifth);
                }
            }
        }
        // The open-string range always provides at least one playable inversion.
        return best ?? throw new InvalidOperationException("Tríade fora da extensão do baixo de 4 cordas.");
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
