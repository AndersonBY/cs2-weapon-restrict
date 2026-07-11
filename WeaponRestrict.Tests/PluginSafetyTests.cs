using Xunit;

namespace WeaponRestrict.Tests;

public class PluginSafetyTests
{
    [Fact]
    public void TryCreateMapRegex_AnchorsTheWholeMapName()
    {
        Assert.True(PluginSafety.TryCreateMapRegex("de_(inferno|mirage)", out var matcher, out var error), error);
        Assert.Matches(matcher!, "de_inferno");
        Assert.DoesNotMatch(matcher!, "workshop_de_inferno");
    }

    [Fact]
    public void TryCreateMapRegex_RejectsInvalidAndOversizedPatterns()
    {
        Assert.False(PluginSafety.TryCreateMapRegex("[", out _, out _));
        Assert.False(PluginSafety.TryCreateMapRegex(new string('a', PluginSafety.MaxMapPatternLength + 1), out _, out _));
    }

    [Fact]
    public void SafeFormat_FallsBackForInvalidTenantTemplate()
    {
        Assert.Equal(
            "AWP is restricted.",
            PluginSafety.SafeFormat("{2}", "AWP is restricted.", "AWP", 1));
    }

    [Fact]
    public void NormalizeMapConfig_RemovesInvalidNumericRules()
    {
        var config = new MapConfig
        {
            WeaponQuotas = new Dictionary<string, float>
            {
                ["weapon_awp"] = 0.2f,
                ["weapon_ssg08"] = float.NaN
            },
            WeaponLimits = new Dictionary<string, int>
            {
                ["weapon_awp"] = 1,
                ["weapon_ssg08"] = -1
            }
        };

        PluginSafety.NormalizeMapConfig(config);

        Assert.Single(config.WeaponQuotas);
        Assert.Single(config.WeaponLimits);
        Assert.True(config.WeaponQuotas.ContainsKey("weapon_awp"));
        Assert.True(config.WeaponLimits.ContainsKey("weapon_awp"));
    }
}
