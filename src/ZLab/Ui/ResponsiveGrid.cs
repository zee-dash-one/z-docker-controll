using ZLab.Services;

namespace ZLab.Ui;

public sealed class ResponsiveGrid : Panel
{
    private int _gap = Tokens.ItemGap;
    private int _minItemWidth = 180;
    private int _maxColumns = 6;
    private int _itemHeight = 118;

    public ResponsiveGrid()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        ThemeManager.ThemeChanged += (_, _) =>
        {
            BackColor = ThemeManager.Current.Bg;
            Invalidate();
        };
        BackColor = ThemeManager.Current.Bg;
    }

    public int Gap
    {
        get => _gap;
        set { _gap = value; PerformLayout(); }
    }

    public int MinItemWidth
    {
        get => _minItemWidth;
        set { _minItemWidth = Math.Max(40, value); PerformLayout(); }
    }

    public int MaxColumns
    {
        get => _maxColumns;
        set { _maxColumns = Math.Max(1, value); PerformLayout(); }
    }

    public int ItemHeight
    {
        get => _itemHeight;
        set { _itemHeight = Math.Max(20, value); PerformLayout(); }
    }

    public static int ColumnsFor(int width, int minItem, int gap, int max) =>
        UiMath.ColumnsFor(width, minItem, gap, max);

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutChildren();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutChildren();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        LayoutChildren();
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        var children = Controls.Cast<Control>().Where(c => c.Visible).ToList();
        if (children.Count == 0)
        {
            Height = 0;
            return;
        }

        var gap = LogicalToDeviceUnits(_gap);
        var minW = LogicalToDeviceUnits(_minItemWidth);
        var itemH = LogicalToDeviceUnits(_itemHeight);
        var cols = ColumnsFor(ClientSize.Width, minW, gap, _maxColumns);
        var rows = (int)Math.Ceiling(children.Count / (double)cols);
        var cellW = (ClientSize.Width - gap * (cols - 1)) / cols;
        if (cellW < 1)
            cellW = 1;

        for (var i = 0; i < children.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            var x = col * (cellW + gap);
            var y = row * (itemH + gap);
            children[i].SetBounds(x, y, cellW, itemH);
        }

        Height = rows * itemH + Math.Max(0, rows - 1) * gap;
    }
}
