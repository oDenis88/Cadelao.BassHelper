using System.Drawing.Drawing2D;
using Cadelao.BassHelper.Models;

namespace Cadelao.BassHelper.UI;

public sealed class PhraseSeekEventArgs(double seconds) : EventArgs
{
    public double Seconds { get; } = seconds;
}

/// <summary>
/// Beat/chord timeline with a movable fixed-time window. Zoom and panning alter
/// ONLY the time-to-pixel mapping; no sample, event timestamp or playback speed
/// is ever modified. The transport's global progress bar stays untouched.
/// </summary>
public sealed class PhraseTimelineControl : Control
{
    private AnalysisResult? _analysis;
    private double _currentTime;
    private double _duration;
    private double _analyzedTime;
    private double _viewStart;
    private double _viewSeconds = 30;
    private bool _followPlayhead = true;

    public event EventHandler<PhraseSeekEventArgs>? SeekRequested;
    public event EventHandler? ViewStartChanged;

    public AnalysisResult? Analysis { get => _analysis; set { _analysis = value; Invalidate(); } }

    public double CurrentTime
    {
        get => _currentTime;
        set
        {
            _currentTime = Math.Clamp(double.IsFinite(value) ? value : 0, 0, Math.Max(0, _duration));
            if (_followPlayhead) KeepPlayheadVisible();
            Invalidate();
        }
    }

    public double Duration
    {
        get => _duration;
        set
        {
            _duration = Math.Max(0, double.IsFinite(value) ? value : 0);
            SetViewStart(_viewStart);
            Invalidate();
        }
    }

    public double AnalyzedTime { get => _analyzedTime; set { _analyzedTime = value; Invalidate(); } }

    /// <summary>Seconds displayed across the whole panel (30 by default).</summary>
    public double ViewSeconds
    {
        get => _viewSeconds;
        set
        {
            if (!double.IsFinite(value)) return;
            double next = Math.Clamp(value, 15, 180);
            if (Math.Abs(next - _viewSeconds) < 0.001) return;
            _viewSeconds = next;
            if (_followPlayhead)
                SetViewStart(_currentTime - Math.Min(_viewSeconds, _duration) * .25);
            else
                SetViewStart(_viewStart);
            Invalidate();
        }
    }

    /// <summary>Beginning of the visible window, in absolute audio seconds.</summary>
    public double ViewStart { get => _viewStart; set => SetViewStart(value); }
    public bool FollowPlayhead
    {
        get => _followPlayhead;
        set
        {
            if (_followPlayhead == value) return;
            _followPlayhead = value;
            if (_followPlayhead) CenterOn(_currentTime);
        }
    }

    public double VisibleDuration => Math.Max(0.01, Math.Min(_viewSeconds, Math.Max(0.01, _duration)));

    public PhraseTimelineControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Color.FromArgb(17, 27, 41);
        ForeColor = Color.FromArgb(180, 204, 218);
        Font = new Font("Segoe UI", 9F);
        Cursor = Cursors.Hand;
        MinimumSize = new Size(120, 70);
    }

    public void CenterOn(double seconds) => SetViewStart(seconds - VisibleDuration * .28);

    private void SetViewStart(double value)
    {
        if (!double.IsFinite(value)) value = 0;
        double max = Math.Max(0, _duration - VisibleDuration);
        double next = Math.Clamp(value, 0, max);
        if (Math.Abs(next - _viewStart) < 0.00001) return;
        _viewStart = next;
        ViewStartChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void KeepPlayheadVisible()
    {
        if (_duration <= _viewSeconds)
        {
            SetViewStart(0);
            return;
        }
        double span = VisibleDuration;
        // Move the viewport in measured seconds, not by seeking the audio.
        // Keep the playhead near 70% of the view during normal playback.
        if (_currentTime > _viewStart + span * .77)
            SetViewStart(_currentTime - span * .27);
        else if (_currentTime < _viewStart + span * .06)
            SetViewStart(_currentTime - span * .27);
    }

    private float PositionX(double seconds, float x0, float x1)
        => x0 + (float)((seconds - _viewStart) / VisibleDuration) * (x1 - x0);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float x0 = 12, x1 = Math.Max(x0 + 1, Width - 12);
        float y0 = 13, y1 = Math.Max(y0 + 5, Height - 30);
        double from = _viewStart;
        double to = Math.Min(Math.Max(0.01, _duration), from + VisibleDuration);
        using var border = new Pen(Color.FromArgb(60, 75, 93));
        g.DrawRectangle(border, x0, y0, x1 - x0, y1 - y0);
        if (_analysis?.Beats is { Count: > 0 } beats)
        {
            using var beatPen = new Pen(Color.FromArgb(55, 170, 190, 210), 1f);
            foreach (double beat in beats)
            {
                if (beat < from || beat > to) continue;
                float bx = PositionX(beat, x0, x1);
                g.DrawLine(beatPen, bx, y0, bx, y1);
            }
        }
        if (_analysis != null)
        {
            foreach (var s in _analysis.ChordSegments)
            {
                if (s.End <= from || s.Start >= to) continue;
                double clippedStart = Math.Max(from, s.Start);
                double clippedEnd = Math.Min(to, s.End);
                float left = Math.Max(x0, PositionX(clippedStart, x0, x1));
                float right = Math.Min(x1, PositionX(clippedEnd, x0, x1));
                if (right - left < 1) continue;
                Color color = ChordColorPalette.Accent(s.EffectiveName);
                using var fill = new SolidBrush(color);
                var rect = new RectangleF(left + 1, y0 + 1,
                    Math.Max(1, right - left - 2), Math.Max(3, y1 - y0 - 2));
                g.FillRectangle(fill, rect);
                if (s.OverrideChord is not null)
                {
                    using var corrected = new Pen(Color.White, 2);
                    g.DrawRectangle(corrected, rect.X, rect.Y, rect.Width, rect.Height);
                }
                if (rect.Width >= 33)
                {
                    using var label = new SolidBrush(ChordColorPalette.ContrastText(color));
                    g.DrawString(s.EffectiveName, Font, label, rect.X + 4, rect.Y + 7);
                }
            }
        }
        if (_analyzedTime > 0 && _analysis is null)
        {
            double progressTo = Math.Min(Math.Min(_analyzedTime, _duration), to);
            if (progressTo > from)
            {
                float px = PositionX(progressTo, x0, x1);
                using var progress = new SolidBrush(Color.FromArgb(70, 88, 174, 163));
                g.FillRectangle(progress, x0 + 1, y0 + 1, Math.Max(0, px - x0), y1 - y0 - 1);
            }
        }
        if (_currentTime >= from && _currentTime <= to)
        {
            float head = PositionX(_currentTime, x0, x1);
            using var marker = new Pen(Color.White, 2);
            g.DrawLine(marker, head, y0 - 3, head, y1 + 3);
        }
        using var text = new SolidBrush(Color.FromArgb(152, 169, 193));
        double tick = _viewSeconds <= 20 ? 2 : _viewSeconds <= 45 ? 5 : _viewSeconds <= 90 ? 10 : 20;
        double first = Math.Ceiling(from / tick) * tick;
        for (double t = first; t <= to + .0001; t += tick)
        {
            float xx = PositionX(t, x0, x1);
            if (xx > x1 - 34) continue;
            g.DrawString(MusicNotes.FormatTime(t), Font, text, xx, y1 + 6);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _duration <= 0) return;
        double ratio = Math.Clamp((e.X - 12.0) / Math.Max(1, Width - 24), 0, 1);
        double target = Math.Clamp(_viewStart + ratio * VisibleDuration, 0, _duration);
        SeekRequested?.Invoke(this, new PhraseSeekEventArgs(target));
    }
}
