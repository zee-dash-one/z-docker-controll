namespace ZLab.Services;

/// <summary>
/// Derives CPU % from successive /proc/stat totals (idle includes iowait).
/// </summary>
public sealed class CpuUsageCalculator
{
    private ulong? _prevTotal;
    private ulong? _prevIdle;

    public double? Percent(ulong total, ulong idle)
    {
        if (_prevTotal is null || _prevIdle is null)
        {
            _prevTotal = total;
            _prevIdle = idle;
            return null;
        }

        if (total < _prevTotal.Value || idle < _prevIdle.Value)
        {
            _prevTotal = total;
            _prevIdle = idle;
            return null;
        }

        var dTotal = total - _prevTotal.Value;
        var dIdle = idle - _prevIdle.Value;
        _prevTotal = total;
        _prevIdle = idle;

        if (dTotal == 0)
            return null;

        var used = dTotal - dIdle;
        var pct = 100.0 * used / dTotal;
        if (pct < 0)
            pct = 0;
        if (pct > 100)
            pct = 100;
        return pct;
    }

    public void Reset()
    {
        _prevTotal = null;
        _prevIdle = null;
    }
}
