using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

/// <summary>
/// T011: Preservation tests — resetting Theme must not alter any other field.
/// </summary>
public class VimiumConfigPreservationTests
{
    [Fact]
    public void FromJson_LegacySkadiTheme_PreservesFontSize()
    {
        var json = "{\"theme\": \"Skadi\", \"fontSize\": \"18\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);       // reset
        Assert.Equal("18", cfg.FontSize);       // preserved
    }

    [Fact]
    public void FromJson_LegacySkadiTheme_PreservesLanguage()
    {
        var json = "{\"theme\": \"Skadi\", \"language\": \"en\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
        Assert.Equal("en", cfg.Language);
    }

    [Fact]
    public void FromJson_LegacySkadiTheme_PreservesMultipleFields()
    {
        var json = "{\"theme\": \"Skadi\", \"fontSize\": \"20\", \"language\": \"en\", \"hintActiveBackground\": \"#FF0000\", \"hintInactiveBackground\": \"#00FF00\", \"hintTextColor\": \"#0000FF\", \"overlayModifier\": \"Alt+X\", \"taskbarModifier\": \"Ctrl+Shift+T\", \"lineNavigationModifier\": \"Ctrl+/\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);                // reset
        Assert.Equal("20", cfg.FontSize);                // preserved
        Assert.Equal("en", cfg.Language);                // preserved
        Assert.Equal("#FF0000", cfg.HintActiveBackground);   // preserved
        Assert.Equal("#00FF00", cfg.HintInactiveBackground); // preserved
        Assert.Equal("#0000FF", cfg.HintTextColor);          // preserved
        Assert.Equal("Alt+X", cfg.OverlayModifier);          // preserved
        Assert.Equal("Ctrl+Shift+T", cfg.TaskbarModifier);   // preserved
        Assert.Equal("Ctrl+/", cfg.LineNavigationModifier);  // preserved
    }

    [Fact]
    public void FromJson_LegacySkadiTheme_PreservesAnimationEnabled()
    {
        var json = "{\"theme\": \"Skadi\", \"hintAnimationEnabled\": false}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
        Assert.False(cfg.HintAnimationEnabled);
    }

    [Fact]
    public void FromJson_UnknownTheme_PreservesAllOtherFields()
    {
        var json = "{\"theme\": \"Neon\", \"fontSize\": \"12\", \"language\": \"en\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
        Assert.Equal("12", cfg.FontSize);
        Assert.Equal("en", cfg.Language);
    }

    [Fact]
    public void FromJson_UnknownTheme_PreservesBooleanAndStringFields()
    {
        var json = "{\"theme\": \"UnknownValue\", \"benchmarkLogEnabled\": false, \"hintFontFamily\": \"Consolas\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
        Assert.False(cfg.BenchmarkLogEnabled);
        Assert.Equal("Consolas", cfg.HintFontFamily);
    }

    [Fact]
    public void FromJson_UnknownTheme_PreservesCopyModifier()
    {
        var json = "{\"theme\": \"Skadi\", \"copyModifier\": \"Alt\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Light", cfg.Theme);
        Assert.Equal("Alt", cfg.CopyModifier);
    }

    [Fact]
    public void FromJson_ValidTheme_DoesNotAlterAnyOtherField()
    {
        // For valid themes, nothing should be touched
        var json = "{\"theme\": \"Dark\", \"fontSize\": \"22\"}";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("Dark", cfg.Theme);
        Assert.Equal("22", cfg.FontSize);
    }
}
