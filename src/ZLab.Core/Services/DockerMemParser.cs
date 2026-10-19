using System.Globalization;
using System.Text.RegularExpressions;

namespace ZLab.Services;

public static class DockerMemParser
{
    private static readonly Regex Pair = new(
        @"^\s*([0-9]*\.?[0-9]+)\s*([A-Za-z]+)\s*/\s*([0-9]*\.?[0-9]+)\s*([A-Za-z]+)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static bool TryParsePair(string? text, out long usedBytes, out long limitBytes)
    {
        usedBytes = 0;
        limitBytes = 0;
        if (string.IsNullOrWhiteSpace(text) || text.Trim() == "-")
            return false;

        var m = Pair.Match(text);
        if (!m.Success)
            return false;
        if (!TryOne(m.Groups[1].Value, m.Groups[2].Value, out usedBytes))
            return false;
        if (!TryOne(m.Groups[3].Value, m.Groups[4].Value, out limitBytes))
            return false;
        return true;
    }

    public static bool TryOne(string number, string unit, out long bytes)
    {
        bytes = 0;
        if (!double.TryParse(number, NumberStyles.Float, Inv, out var value))
            return false;
        var factor = UnitFactor(unit);
        if (factor <= 0)
            return false;
        bytes = (long)Math.Round(value * factor, MidpointRounding.AwayFromZero);
        return true;
    }

    private static double UnitFactor(string unit) => unit.Trim() switch
    {
        "B" => 1,
        "KiB" => 1024d,
        "MiB" => 1024d * 1024,
        "GiB" => 1024d * 1024 * 1024,
        "TiB" => 1024d * 1024 * 1024 * 1024,
        "kB" => 1000d,
        "MB" => 1000d * 1000,
        "GB" => 1000d * 1000 * 1000,
        "TB" => 1000d * 1000 * 1000 * 1000,
        _ => 0
    };
}
