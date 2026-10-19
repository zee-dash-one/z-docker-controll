using System.Runtime.InteropServices;

namespace ZLab.Ui;

internal static class Native
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);

    public static void TrySetImmersiveDarkMode(IntPtr hwnd, bool enabled)
    {
        if (hwnd == IntPtr.Zero)
            return;
        try
        {
            var value = enabled ? 1 : 0;
            _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
        }
        catch
        {
            // ignore on older Windows
        }
    }
}
