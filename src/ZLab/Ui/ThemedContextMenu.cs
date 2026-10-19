namespace ZLab.Ui;

internal sealed class ThemedColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => ThemeManager.Current.Stroke;
    public override Color MenuItemBorder => ThemeManager.Current.Stroke;
    public override Color MenuItemSelected => ThemeManager.Current.Surface3;
    public override Color MenuItemSelectedGradientBegin => ThemeManager.Current.Surface3;
    public override Color MenuItemSelectedGradientEnd => ThemeManager.Current.Surface3;
    public override Color MenuItemPressedGradientBegin => ThemeManager.Current.Surface3;
    public override Color MenuItemPressedGradientEnd => ThemeManager.Current.Surface3;
    public override Color ToolStripDropDownBackground => ThemeManager.Current.Surface;
    public override Color ImageMarginGradientBegin => ThemeManager.Current.Surface;
    public override Color ImageMarginGradientMiddle => ThemeManager.Current.Surface;
    public override Color ImageMarginGradientEnd => ThemeManager.Current.Surface;
    public override Color SeparatorDark => ThemeManager.Current.Stroke;
    public override Color SeparatorLight => ThemeManager.Current.Stroke;
}

internal static class ThemedContextMenu
{
    public static ContextMenuStrip Create()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable()),
            ShowImageMargin = false,
            Font = Tokens.Body()
        };
        ThemeManager.ThemeChanged += (_, _) =>
        {
            menu.BackColor = ThemeManager.Current.Surface;
            menu.ForeColor = ThemeManager.Current.Text;
            foreach (ToolStripItem item in menu.Items)
            {
                item.ForeColor = ThemeManager.Current.Text;
                item.BackColor = ThemeManager.Current.Surface;
            }
        };
        menu.BackColor = ThemeManager.Current.Surface;
        menu.ForeColor = ThemeManager.Current.Text;
        return menu;
    }
}
