using System.Collections.Generic;
using System.Globalization;

// checks the finished terrain against the preset's targets
// empty list = ok, otherwise each line says what was off (the site then retries with seed + 1)
public static class Quality
{
    public static List<string> Check(TerrainConfig c, RunMetrics m)
    {
        var fails = new List<string>();

        if (m.rawHeightRange < c.minHeightRange)
            fails.Add($"terrain is flat (range {F(m.rawHeightRange, "0.0000")} < {F(c.minHeightRange, "0.0000")})");

        var t = c.targets;
        m.targetsChecked = t.enabled;
        if (!t.enabled) return fails;

        float under5 = m.slopeBands[0];
        float over30 = m.slopeBands[3] + m.slopeBands[4];

        InRange(fails, "mean slope", m.meanSlopeDeg, t.meanSlopeMin, t.meanSlopeMax, "0.0", " deg");
        InRange(fails, "under 5 deg", under5 * 100f, t.under5Min * 100f, t.under5Max * 100f, "0", "%");
        InRange(fails, "over 30 deg", over30 * 100f, t.over30Min * 100f, t.over30Max * 100f, "0", "%");
        return fails;
    }

    static void InRange(List<string> fails, string name, float v, float min, float max, string fmt, string unit)
    {
        if (v < min || v > max)
            fails.Add($"{name} {F(v, fmt)}{unit}, expected {F(min, fmt)}-{F(max, fmt)}{unit}");
    }

    // dot as decimal separator no matter the windows language
    static string F(float v, string fmt) => v.ToString(fmt, CultureInfo.InvariantCulture);
}