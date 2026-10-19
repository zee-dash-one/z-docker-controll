namespace ZLab.Services;

public enum SeverityKind
{
    Ok,
    Warn,
    Bad
}

/// <summary>
/// Pure UI math shared by WinForms controls and unit tests.
/// </summary>
public static class UiMath
{
    public static SeverityKind Severity(double percent)
    {
        if (percent < 70)
            return SeverityKind.Ok;
        if (percent <= 90)
            return SeverityKind.Warn;
        return SeverityKind.Bad;
    }

    public static int ColumnsFor(int width, int minItem, int gap, int max)
    {
        if (width <= 0 || minItem <= 0)
            return 1;
        var cols = (width + gap) / (minItem + gap);
        if (cols < 1)
            cols = 1;
        if (cols > max)
            cols = max;
        return cols;
    }
}
