using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.UI;

/// <summary>
/// The original slim rosewood bass-fretboard look, rotated into a vertical neck.
/// The nut is at the TOP; strings from LEFT to RIGHT are E-A-D-G.
/// Only the chord fundamental is marked. The full dot is a fretted, playable
/// ROOT-THIRD-FIFTH arpeggio anchor; thin rings are optional non-open locations.
/// </summary>
public sealed class BassFretboardControl : Control
{
    private static readonly (string Name, int Midi)[] Strings =
    [ ("E", 28), ("A", 33), ("D", 38), ("G", 43) ];
    private const int LastFret = 12;
    private string _chord = "--";
    private ChordDefinition? _definition;
    private TriadFingering? _suggestedShape;
    private readonly ToolTip _tips = new() { ShowAlways = true, InitialDelay = 220 };
    private string _lastTip = "";
    private readonly List<(RectangleF Area, string Description)> _hitZones = new();

    public BassFretboardControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(17, 26, 39);
        ForeColor = Color.FromArgb(235, 241, 250);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(200, 315);
        AccessibleName = "Braco do contrabaixo vertical, cordas E A D G; fundamental e triade sugerida.";
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string CurrentChord
    {
        get => _chord;
        set
        {
            string next = value?.Trim() ?? "--";
            if (string.Equals(_chord, next, StringComparison.Ordinal)) return;
            _chord = next;
            _definition = ChordVocabulary.TryParse(next, out var parsed) ? parsed : null;
            try { _suggestedShape = _definition is null ? null : FourStringBass.Suggest(_definition); }
            catch (InvalidOperationException) { _suggestedShape = null; }
            Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tips.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        string description = _hitZones.FirstOrDefault(z => z.Area.Contains(e.X, e.Y)).Description ?? "";
        if (description == _lastTip) return;
        _lastTip = description;
        _tips.SetToolTip(this, description);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _lastTip = "";
        _tips.SetToolTip(this, "");
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(BackColor);
        _hitZones.Clear();
        if (ClientSize.Width < 175 || ClientSize.Height < 285) return;

        int width = ClientSize.Width, height = ClientSize.Height;
        Color accent = ChordColorPalette.Accent(_chord);
        using (var textBrush = new SolidBrush(Color.FromArgb(245, 248, 253)))
        using (var font = new Font("Segoe UI", Math.Min(13, Math.Max(10, width / 33f)), FontStyle.Bold))
            g.DrawString("BRAÇO  ·  4 CORDAS", font, textBrush, 18, 15);
        using (var small = new Font("Segoe UI", 9F))
        using (var muted = new SolidBrush(Color.FromArgb(150, 170, 192)))
            g.DrawString("E–A–D–G  |  CASAS 0–12", small, muted, 19, 42);

        // Same thin proportions and palette as the original horizontal drawing;
        // the long and short axes are now exchanged. Never stretch across the
        // whole right-side panel: that was why the previous neck looked fat.
        float neckWidth = Math.Clamp(width * .34f, 116f, 155f);
        float neckLeft = (width - neckWidth) / 2f;
        float neckTop = 81f;
        float neckHeight = Math.Max(165f, height - neckTop - 93f);
        var neck = new RectangleF(neckLeft, neckTop, neckWidth, neckHeight);
        using (var wood = new LinearGradientBrush(neck,
            Color.FromArgb(72, 53, 44), Color.FromArgb(30, 28, 38), LinearGradientMode.Horizontal))
        using (var border = new Pen(Color.FromArgb(135, 124, 127), 1.5f))
        {
            g.FillRoundedRectangle(wood, neck, 9);
            g.DrawRoundedRectangle(border, neck, 9);
        }

        // Original subtle rosewood grain, with the fibers rotated vertically.
        using (var grain = new Pen(Color.FromArgb(24, 228, 183, 133), .65f))
        {
            for (int i = 1; i <= 12; i++)
            {
                float x = neck.Left + i * neck.Width / 13f;
                g.DrawLine(grain, x, neck.Top + 6,
                    x + (i % 3 - 1) * 2, neck.Bottom - 6);
            }
        }

        float yNut = neck.Top + 17f;
        float yEnd = neck.Bottom - 7f;
        float fretStep = (yEnd - yNut) / LastFret;
        float stringStep = neck.Width / 5f;
        float[] stringXs = new float[Strings.Length];
        for (int i = 0; i < Strings.Length; i++)
            stringXs[i] = neck.Left + stringStep * (i + 1);

        for (int fret = 1; fret <= LastFret; fret++)
        {
            float y = yNut + (fret - .05f) * fretStep;
            using var wire = new Pen(Color.FromArgb(165, 182, 193), fret == LastFret ? 2f : 1.15f);
            g.DrawLine(wire, neck.Left + 4, y, neck.Right - 4, y);
        }
        using (var nut = new Pen(Color.FromArgb(241, 237, 215), 5f))
            g.DrawLine(nut, neck.Left + 4, yNut, neck.Right - 4, yNut);

        // Original inlay dots (3,5,7,9,12), with small fret indices for practice.
        using (var pearl = new SolidBrush(Color.FromArgb(86, 199, 205, 211)))
        using (var indexBrush = new SolidBrush(Color.FromArgb(138, 159, 176)))
        using (var indexFont = new Font("Segoe UI", 8F))
        {
            foreach (int fret in new[] { 3, 5, 7, 9, 12 })
            {
                float y = yNut + (fret - .55f) * fretStep;
                float center = neck.Left + neck.Width / 2f;
                if (fret == 12)
                {
                    g.FillEllipse(pearl, center - 12, y - 3, 6, 6);
                    g.FillEllipse(pearl, center + 6, y - 3, 6, 6);
                }
                else g.FillEllipse(pearl, center - 3, y - 3, 6, 6);
                g.DrawString(fret.ToString(), indexFont, indexBrush,
                    neck.Right + 7, y - 8);
            }
        }

        using (var labels = new SolidBrush(Color.FromArgb(224, 231, 241)))
        using (var font = new Font("Segoe UI", 10F, FontStyle.Bold))
        {
            for (int s = 0; s < Strings.Length; s++)
            {
                float x = stringXs[s];
                // E is the leftmost string, intentionally the thickest.
                using var stringPen = new Pen(Color.FromArgb(
                    202 - 28 * s, 214 - 17 * s, 220 - 11 * s), 3.0f - .49f * s);
                g.DrawLine(stringPen, x, yNut, x, yEnd);
                var glyph = g.MeasureString(Strings[s].Name, font);
                g.DrawString(Strings[s].Name, font, labels,
                    x - glyph.Width / 2f, neck.Top - 14f);
            }
        }

        int alternatives = 0;
        if (_definition is not null)
        {
            foreach (BassPosition position in FourStringBass.RootPositions(_definition.Root))
            {
                // Open strings are available on the instrument, but NOT a
                // suggested brega triad anchor; do not display them as circles.
                if (position.Fret == 0) continue;
                int s = Array.FindIndex(Strings, item => item.Name == position.StringName);
                if (s < 0) continue;

                bool preferred = _suggestedShape is not null &&
                    _suggestedShape.Root.StringName == position.StringName &&
                    _suggestedShape.Root.Fret == position.Fret;
                if (!preferred) alternatives++;
                float x = stringXs[s];
                float y = yNut + (position.Fret - .53f) * fretStep;
                float diameter = preferred ? 25f : 16f;
                diameter = Math.Min(diameter, Math.Max(13f, stringStep - 4f));
                var circle = new RectangleF(x - diameter / 2f, y - diameter / 2f,
                    diameter, diameter);
                using (var glow = new SolidBrush(Color.FromArgb(preferred ? 55 : 20, accent)))
                    g.FillEllipse(glow, circle.X - 5, circle.Y - 5,
                        diameter + 10, diameter + 10);
                using (var fill = new SolidBrush(preferred ? accent : Color.FromArgb(36, 26, 39, 54)))
                    g.FillEllipse(fill, circle);
                using (var rim = new Pen(accent, preferred ? 2.6f : 2f))
                    g.DrawEllipse(rim, circle);
                if (preferred)
                {
                    using var font = new Font("Segoe UI", 10F, FontStyle.Bold);
                    using var ink = new SolidBrush(ChordColorPalette.ContrastText(accent));
                    string label = MusicNotes.Names[_definition.Root];
                    var glyph = g.MeasureString(label, font);
                    g.DrawString(label, font, ink,
                        x - glyph.Width / 2f, y - glyph.Height / 2f - 1f);
                }
                _hitZones.Add((RectangleF.Inflate(circle, 5, 5),
                    $"{MusicNotes.Names[_definition.Root]} • corda {Strings[s].Name} • casa {position.Fret}" +
                    (preferred ? " • raiz sugerida da tríade" : " • alternativa")));
            }
        }

        // Reserve the bottom of the control for the same compact summary
        // used by the original version, not a giant neck illustration.
        string rootText = _suggestedShape is null ? "Aguardando cifra..." :
            $"{_chord}  ·  FUNDAMENTAL: {_suggestedShape.Root.Describe()}";
        using (var font = new Font("Segoe UI", 11F, FontStyle.Bold))
        using (var brush = new SolidBrush(_suggestedShape is null ?
            Color.FromArgb(148, 167, 189) : accent))
            g.DrawString(rootText, font, brush,
                14, Math.Min(height - 49, neck.Bottom + 13));

        string helper = _suggestedShape is null ?
            "A fundamental aparece quando a música tocar." :
            $"Tríade: {_suggestedShape.Third.Describe()}  +  {_suggestedShape.Fifth.Describe()}";
        using (var font = new Font("Segoe UI", 8.5F))
        using (var brush = new SolidBrush(Color.FromArgb(159, 181, 199)))
            g.DrawString(helper, font, brush,
                new RectangleF(15, Math.Min(height - 23, neck.Bottom + 40), width - 30, 25));
    }
}

internal static class RoundedGdiExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = Round(rect, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using var path = Round(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath Round(RectangleF r, float rad)
    {
        float d = Math.Min(Math.Min(r.Width, r.Height), rad * 2);
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
