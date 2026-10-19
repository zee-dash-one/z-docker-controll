namespace ZLab.Ui;

/// <summary>
/// Panel with WS_EX_COMPOSITED to reduce flicker while resizing.
/// </summary>
public class CompositedPanel : Panel
{
    public CompositedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
            return cp;
        }
    }
}
