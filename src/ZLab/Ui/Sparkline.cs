using System.Drawing.Drawing2D;

namespace ZLab.Ui;

public enum SparkMaxMode
{
    Fixed100,
    Auto
}

public sealed class Sparkline : Control
{
    private readonly double[] _samples = new double[60];
    private int _count;
    private int _head;
    private SparkMaxMode _maxMode = SparkMaxMode.Fixed100;

    public Sparkline()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor,
            true);
        ThemeManager.ThemeChanged += (_, _) => Invalidate();
    }

    public SparkMaxMode MaxMode
    {
        get => _maxMode;
        set
        {
            _maxMode = value;
            Invalidate();
        }
    }

    public void Push(double value)
    {
        _samples[_head] = value;
        _head = (_head + 1) % _samples.Length;
        if (_count < _samples.Length)
            _count++;
        Invalidate();
    }

    public void Clear()
    {
        _count = 0;
        _head = 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_count < 2 || Width < 4 || Height < 4)
            return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var p = ThemeManager.Current;
        var values = new double[_count];
        var start = (_head - _count + _samples.Length) % _samples.Length;
        for (var i = 0; i < _count; i++)
            values[i] = _samples[(start + i) % _samples.Length];

        var max = _maxMode == SparkMaxMode.Fixed100
            ? 100.0
            : Math.Max(1.0, values.Max());
        var min = 0.0;

        var points = new PointF[_count];
        for (var i = 0; i < _count; i++)
        {
            var x = i * (Width - 1f) / (_count - 1);
            var t = (values[i] - min) / (max - min);
            var y = (Height - 1) - (float)(t * (Height - 1));
            points[i] = new PointF(x, y);
        }

        using (var fillPath = new GraphicsPath())
        {
            fillPath.AddLines(points);
            fillPath.AddLine(points[^1], new PointF(points[^1].X, Height - 1));
            fillPath.AddLine(new PointF(points[^1].X, Height - 1), new PointF(points[0].X, Height - 1));
            fillPath.CloseFigure();
            using var brush = new SolidBrush(p.AccentSoft);
            g.FillPath(brush, fillPath);
        }

        using var pen = new Pen(p.Accent, 1.6f);
        g.DrawLines(pen, points);
    }
}
