using Microsoft.Win32;

namespace ZLab.Ui;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public static class ThemeManager
{
    private static ThemeMode _mode = ThemeMode.System;
    private static Palette _current = Resolve(ThemeMode.System);

    public static Palette Current => _current;

    public static ThemeMode Mode => _mode;

    public static event EventHandler? ThemeChanged;

    public static void Apply(ThemeMode mode)
    {
        _mode = mode;
        _current = Resolve(mode);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ToggleLightDark()
    {
        var dark = Current == Palette.Dark;
        Apply(dark ? ThemeMode.Light : ThemeMode.Dark);
    }

    public static void ApplyTo(Form form)
    {
        form.BackColor = Current.Bg;
        form.ForeColor = Current.Text;
        Native.TrySetImmersiveDarkMode(form.Handle, Current == Palette.Dark);
    }

    private static Palette Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => Palette.Light,
        ThemeMode.Dark => Palette.Dark,
        _ => SystemPrefersLight() ? Palette.Light : Palette.Dark
    };

    private static bool SystemPrefersLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int i)
                return i != 0;
        }
        catch
        {
            // fall through
        }

        return false;
    }
}
