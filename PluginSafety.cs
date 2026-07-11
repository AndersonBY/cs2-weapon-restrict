using System.Text.RegularExpressions;

namespace WeaponRestrict;

public static class PluginSafety
{
    public const int MaxMapPatternLength = 128;
    public static readonly TimeSpan MapPatternTimeout = TimeSpan.FromMilliseconds(50);

    public static bool TryCreateMapRegex(string pattern, out Regex? matcher, out string? error)
    {
        matcher = null;
        error = null;

        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "pattern is empty";
            return false;
        }

        if (pattern.Length > MaxMapPatternLength)
        {
            error = $"pattern exceeds {MaxMapPatternLength} characters";
            return false;
        }

        try
        {
            matcher = new Regex(
                $"^(?:{pattern})$",
                RegexOptions.CultureInvariant,
                MapPatternTimeout);
            return true;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static void NormalizeMapConfig(MapConfig config)
    {
        foreach (var weapon in config.WeaponQuotas
            .Where(entry => entry.Value < 0f || float.IsNaN(entry.Value) || float.IsInfinity(entry.Value))
            .Select(entry => entry.Key)
            .ToArray())
        {
            config.WeaponQuotas.Remove(weapon);
        }

        foreach (var weapon in config.WeaponLimits
            .Where(entry => entry.Value < 0)
            .Select(entry => entry.Key)
            .ToArray())
        {
            config.WeaponLimits.Remove(weapon);
        }
    }

    public static string SafeFormat(string template, string fallback, params object[] arguments)
    {
        try
        {
            return string.Format(template, arguments);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}
