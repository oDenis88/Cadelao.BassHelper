using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Cadelao.BassHelper.UI;

public enum TransportSymbol { Play, Pause, Stop }

/// <summary>Owner-drawn, keyboard-accessible WinForms button with vector icon.</summary>
public sealed class TransportIconButton : Button
{
    private bool _hover;
    private TransportSymbol _symbol;
    // The Designer must preserve Play/Pause/Stop for each transport button.
    [DefaultValue(TransportSymbol.Play)]
    public TransportSymbol Symbol
    {
        get => _symbol;
        set { _symbol = value; Invalidate(); }
    }

    public TransportIconButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        ForeColor = Color.White;
        BackColor = Color.FromArgb(43, 67, 87);
        Size = new Size(110, 43);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        Color bg = Enabled ? (_hover ? ControlPaint.Light(BackColor, .12f) : BackColor)
                           : Color.FromArgb(40, 48, 59);
        g.Clear(bg);
        using (var edge = new Pen(Focused ? Color.FromArgb(160, 224, 250) : Color.FromArgb(40, 94, 124)))
            g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
        float centerX = 18f, centerY = Height / 2f;
        Color fg = Enabled ? ForeColor : Color.FromArgb(124, 135, 149);
        using var brush = new SolidBrush(fg);
        switch (Symbol)
        {
            case TransportSymbol.Play:
                g.FillPolygon(brush, new PointF[] { new PointF(centerX - 5, centerY - 9),
                    new PointF(centerX + 8, centerY), new PointF(centerX - 5, centerY + 9) });
                break;
            case TransportSymbol.Pause:
                g.FillRectangle(brush, centerX - 7, centerY - 8, 5, 16);
                g.FillRectangle(brush, centerX + 1, centerY - 8, 5, 16);
                break;
            case TransportSymbol.Stop:
                g.FillRectangle(brush, centerX - 7, centerY - 7, 14, 14);
                break;
        }
        var rect = new Rectangle(33, 0, Width - 36, Height);
        TextRenderer.DrawText(g, Text, Font, rect, fg,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(3, 3, Width - 6, Height - 6));
    }
}
