using System.Drawing;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.UI;

/// <summary>
/// A different color for each of the 12 pitch classes (C through B).
/// The same pitch class keeps its color across the playhead, the timeline,
/// and the chord table. Minor/major remain explicit in the chord label.
/// Pure presentation: does not touch Chordino, timing or saved edits.
/// </summary>
public static class ChordColorPalette
{
    // Indexed by chromatic pitch class: C, C#, D, D#, E, F, F#, G, G#, A, A#, B.
    // Deliberately non-sequential hues so adjacent musical notes and common
    // brega triads are visually distinct. Am hot pink, Dm blue, E gold.
    private static readonly Color[] RootColors =
    [
        Color.FromArgb(247, 100, 106), // C: coral
        Color.FromArgb(170, 113, 245), // C#: violet
        Color.FromArgb(68, 144, 255),  // D: cobalt blue
        Color.FromArgb(255, 148, 86),  // D#: orange
        Color.FromArgb(255, 216, 74),  // E: golden yellow
        Color.FromArgb(56, 216, 156),  // F: emerald
        Color.FromArgb(140, 222, 60),  // F#: lime
        Color.FromArgb(255, 143, 198), // G: pastel pink
        Color.FromArgb(111, 104, 246), // G#: indigo
        Color.FromArgb(255, 64, 151),  // A: vivid hot pink
        Color.FromArgb(49, 207, 223),  // A#: cyan
        Color.FromArgb(234, 121, 208)  // B: fuchsia
    ];

    public static Color Unknown => Color.FromArgb(79, 96, 116);

    public static Color Accent(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
            ChordVocabulary.TryParse(name, out ChordDefinition? chord) && chord != null
            ? RootColors[chord.Root]
            : Unknown;
    }

    public static Color ContrastText(Color background)
    {
        // WCAG luminance to keep labels readable on bright yellow/green as well
        // as on dark purple/blue. Chord name never relies on color alone.
        static double Channel(byte value)
        {
            double c = value / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        double luminance = 0.2126 * Channel(background.R) +
                           0.7152 * Channel(background.G) +
                           0.0722 * Channel(background.B);
        return luminance >= 0.18 ? Color.FromArgb(5, 9, 15) : Color.White;
    }
}
