using Cadelao.BassHelper.Analysis;
using Cadelao.BassHelper.Models;

int failures = 0;
void Check(bool success, string name)
{
    Console.WriteLine($"{(success ? "PASS" : "FAIL")}  {name}");
    if (!success) failures++;
}

Check(ChordVocabulary.TryParse("E7", out var e) && e!.Name == "E" && !e.IsMinor,
    "E7 da cifra do violao deve virar E (tríade maior)");
Check(ChordVocabulary.TryParse("Am7", out var am) && am!.Name == "Am" && am.IsMinor,
    "Am7 deve virar Am (tríade menor)");
Check(ChordVocabulary.TryParse("Bb", out var bb) && bb!.Name == "A#",
    "Bemol na cifra: Bb -> A#");
Check(!ChordVocabulary.TryParse("Gdim", out _), "Nao inventar acordes diminutos, foco apenas maior/menor");
Check(ChordVocabulary.Parse("Dm, Am, E7, E").Count == 3, "Deduplicar tríades equivalentes");
Check(ChordVocabulary.Parse("Dm, Am, E7").Select(c => c.Name).SequenceEqual(["Dm", "Am", "E"]),
    "Repertorio Ronaldo Adriano Dm Am E");

foreach (var triad in ChordVocabulary.Automatic)
{
    TriadFingering fingering = FourStringBass.Suggest(triad);
    Check(fingering.Root.Midi % 12 == triad.Root &&
          fingering.Third.Midi % 12 == triad.Third &&
          fingering.Fifth.Midi % 12 == triad.Fifth &&
          fingering.Root.Fret <= 12 && fingering.Third.Fret <= 12 && fingering.Fifth.Fret <= 12,
        $"Posicoes no baixo 4 cordas: {triad.Name}");
}

// Brega 70s: the note must be an anchor for a playable root-third-fifth
// fingering. Open strings must NOT be the highlighted option.
// Fretted shapes reflect the player's explicit preference.
foreach (var (name, rootString, rootFret, thirdString, thirdFret, fifthString, fifthFret) in new[]
{
    ("Dm", "A", 5, "D", 3, "G", 2),
    ("D",  "A", 5, "D", 4, "G", 2),
    ("Am", "E", 5, "A", 3, "D", 2),
    ("A",  "E", 5, "A", 4, "D", 2),
    ("E",  "A", 7, "D", 6, "G", 4),
    ("Em", "A", 7, "D", 5, "G", 4)
})
{
    Check(ChordVocabulary.TryParse(name, out var chord), $"Tríade {name} interpretada");
    var suggestion = FourStringBass.Suggest(chord!);
    Check(suggestion.Root.StringName == rootString && suggestion.Root.Fret == rootFret,
        $"Raiz de {name}: {rootString}{rootFret} (sem corda solta)");
    Check(suggestion.Third.StringName == thirdString && suggestion.Third.Fret == thirdFret &&
          suggestion.Fifth.StringName == fifthString && suggestion.Fifth.Fret == fifthFret,
        $"Shape de {name}: {thirdString}{thirdFret} + {fifthString}{fifthFret}");
}
foreach (var chord in ChordVocabulary.Automatic)
{
    var shape = FourStringBass.Suggest(chord);
    Check((shape.Root.StringName is "E" or "A") && shape.Root.Fret is >= 1 and <= 7 &&
          shape.Third.Fret > 0 && shape.Fifth.Fret > 0 &&
          shape.Third.Midi - shape.Root.Midi == (chord.IsMinor ? 3 : 4) &&
          shape.Fifth.Midi - shape.Root.Midi == 7,
        $"Apoio na região do brega, sem corda solta: {chord.Name}");
}

// The on-screen neck must display roots only, with no fictitious bass notes.
foreach (var (root, expected) in new[]
{
    ("D", new[] {"E10", "A5", "D0", "D12", "G7"}),
    ("A", new[] {"E5", "A0", "A12", "D7", "G2"}),
    ("E", new[] {"E0", "E12", "A7", "D2", "G9"})
})
{
    int pitch = Array.IndexOf(MusicNotes.Names, root);
    var actual = FourStringBass.RootPositions(pitch)
        .Select(p => p.StringName + p.Fret)
        .OrderBy(s => s, StringComparer.Ordinal)
        .ToArray();
    Check(actual.SequenceEqual(expected.OrderBy(s => s, StringComparer.Ordinal)),
        $"Braço E-A-D-G: apenas fundamental {root} nas casas corretas");
}

static float[] Synth(int[] midi)
{
    var x = new float[BassPitchDetector.WindowSize];
    for (int i = 0; i < x.Length; i++)
    {
        double t = i / (double)BassPitchDetector.SampleRate;
        double v = 0;
        for (int j = 0; j < midi.Length; j++)
        {
            double f = MusicNotes.Frequency(midi[j]);
            double amp = j == 0 ? .21 : j == 1 ? .14 : .12;
            v += amp * Math.Sin(2 * Math.PI * f * t);
            v += .025 * Math.Sin(4 * Math.PI * f * t);
        }
        x[i] = (float)v;
    }
    return x;
}
var detector = new ChordDetector();
var known = ChordVocabulary.Parse("Dm, Am, E");
foreach (var (name, midi) in new[]
{
    ("Dm", new[] {38, 53, 57}), // D2, F3, A3
    ("Am", new[] {33, 48, 52}), // A1, C3, E3
    ("E", new[] {28, 44, 47})   // E1, G#2, B2
})
{
    ChordFrame frame = detector.Detect(Synth(midi), 0.4);
    var chosen = known.OrderByDescending(c => c.Score(frame)).First();
    Check(chosen.Name == name, $"FFT distinguir a tríade sintetica {name}: {chosen.Name}");
}

// Melisma / passing tone: 2 wrong frames in the middle of 5s Am should not
// create a new chord phrase. No NAudio/WinForms needed for this test.
var phraseFrames = new List<ChordFrame>();
for (int i = 0; i < 50; i++)
{
    bool passing = i is 23 or 24;
    float[] mid = new float[12];
    float[] low = new float[12];
    Array.Fill(mid, 0.015f);
    Array.Fill(low, 0.012f);
    if (passing) { mid[2] = 0.39f; mid[5] = 0.31f; mid[9] = 0.24f; low[2] = 0.4f; }
    else { mid[9] = 0.26f; mid[0] = 0.35f; mid[4] = 0.24f; low[9] = 0.5f; }
    phraseFrames.Add(new ChordFrame(i * .1, mid, low, .15));
}
var segments = ChordSegmenter.Build(phraseFrames, 5.0, known);
Check(segments.Count == 1 && segments[0].Name == "Am",
    $"Notas passageiras de 0,2s nao mudam a frase: {segments.Count} segmentos");

var bass = new BassPitchDetector(28, 55);
foreach (int midi in new[] {28, 33, 38, 43, 47, 52, 55})
{
    NoteFrame note = bass.Detect(Synth([midi]), 0.4);
    Check(note.Midi == midi, $"Nota isolada no alcance E1-G3 {MusicNotes.NameOctave(midi)} -> {MusicNotes.NameOctave(note.Midi)}");
}

// The NEW default path is a native Chordino plugin. These unit tests cover the
// adapter's evidence preservation and reduction to major/minor bass triads.
foreach (var (raw, expected) in new[]
{
    ("E7", "E"), ("E:maj7", "E"), ("Dm7", "Dm"),
    ("Am", "Am"), ("A:min7", "Am"), ("Bb7", "A#"),
    ("F#7/C#", "F#"), ("N", "--"), ("Bdim", "--"), ("G:sus4", "--")
})
{
    bool ok = ChordinoTimeline.TryReduceToBassTriad(raw, out string reduced);
    Check(reduced == expected && ok == (expected != "--"),
        $"Chordino {raw} -> {reduced}, esperado {expected}");
}
var external = new List<ChordinoEvent>
{
    new(0.0, "Dm"), new(0.15, "E7"), new(0.34, "Am"),
    new(0.53, "N"), new(0.63, "Am"), new(1.0, "Gdim")
};
var native = ChordinoTimeline.Build(external, 2.0);
Check(native.Count == external.Count, "Nao omitir eventos curtos no Chordino");
Check(native[1].Name == "E" && native[1].RawName == "E7" &&
      native[1].Start == 0.15 && native[1].End == 0.34,
      "Timestamp Chordino e cifra bruta imutaveis");
Check(native[3].Name == "--" && native[3].RawName == "N",
      "Nao-chord preservado em vez de inventar um acorde");
Console.WriteLine($"Falhas detectadas: {failures}");
return failures == 0 ? 0 : 1;
