using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cadelao.BassHelper.Models;

/// <summary>Preserve local corrections independently from the immutable Chordino track.</summary>
public static class ChordEditStore
{
    private sealed record OverrideEntry(double Start, string Chord);
    private sealed record EditFile(string Source, List<OverrideEntry> Overrides);

    private static string GetPath(string song)
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cadelao.BassHelper", "Edits");
        Directory.CreateDirectory(dir);
        var info = new FileInfo(song);
        string fingerprint = Path.GetFullPath(song).ToUpperInvariant() + "|" +
            info.Length + "|" + info.LastWriteTimeUtc.Ticks;
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
        return Path.Combine(dir, hash + ".json");
    }

    public static void Save(string song, IReadOnlyList<ChordSegment> chords)
    {
        var edits = chords.Where(x => x.OverrideChord is not null)
            .Select(x => new OverrideEntry(x.Start, x.OverrideChord!)).ToList();
        string target = GetPath(song);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new EditFile(song, edits),
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, target, overwrite: true);
    }

    public static void Restore(string song, IReadOnlyList<ChordSegment> chords)
    {
        string path = GetPath(song);
        if (!File.Exists(path)) return;
        try
        {
            var file = JsonSerializer.Deserialize<EditFile>(File.ReadAllText(path));
            if (file is null) return;
            foreach (var correction in file.Overrides)
            {
                var segment = chords.FirstOrDefault(s => Math.Abs(s.Start - correction.Start) < 0.10);
                if (segment is not null && ChordVocabulary.TryParse(correction.Chord, out var parsed))
                    segment.OverrideChord = parsed!.Name;
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Invalid or inaccessible local sidecar must not break chord recognition.
        }
    }
}
