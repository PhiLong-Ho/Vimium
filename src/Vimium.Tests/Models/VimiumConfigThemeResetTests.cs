using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

/// <summary>
/// T010: Theme reset / validation tests for VimiumConfig.FromJson().
/// </summary>
public class VimiumConfigThemeResetTests
{
    [Fact]
    public void FromJson_LegacySkadiTheme_ResetsToLight()
    {
        var json = "{\"theme\": \"Skadi\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void FromJson_UnknownTheme_ResetsToLight()
    {
        var json = "{\"theme\": \"Neon\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void FromJson_UnknownThemeWithDifferentCase_ResetsToLight()
    {
        // Case-sensitive: "skadi" (lowercase) is also unrecognized
        var json = "{\"theme\": \"skadi\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void FromJson_LightTheme_PreservedUnchanged()
    {
        var json = "{\"theme\": \"Light\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void FromJson_DarkTheme_PreservedUnchanged()
    {
        var json = "{\"theme\": \"Dark\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Dark", cfg.Theme);
    }

    [Fact]
    public void FromJson_ArknightsTheme_PreservedUnchanged()
    {
        var json = "{\"theme\": \"Arknights\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Arknights", cfg.Theme);
    }

    [Fact]
    public void FromJson_EmptyTheme_ResetsToLight()
    {
        // Empty string is not a valid theme
        var json = "{\"theme\": \"\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void Default_ThemeIsLight()
    {
        var cfg = VimiumConfig.Default;
        Assert.Equal("Light", cfg.Theme);
    }
}
