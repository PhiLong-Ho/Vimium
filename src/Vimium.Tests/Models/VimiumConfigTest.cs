using Vimium.Models;
using Xunit;

namespace Vimium.Tests.Models;

public class VimiumConfigTest
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var cfg = VimiumConfig.Default;

        Assert.Equal("14", cfg.FontSize);
        Assert.Equal("Light", cfg.Theme);
        Assert.Equal("en", cfg.Language);
        Assert.Equal("#FFFFFF", cfg.HintActiveBackground);
        Assert.Equal("#F0F0F0", cfg.HintInactiveBackground);
        Assert.Equal("#000000", cfg.HintTextColor);
        Assert.True(cfg.HintAnimationEnabled);
        Assert.Equal("Ctrl+;", cfg.OverlayModifier);
        Assert.Equal("Ctrl+'", cfg.TaskbarModifier);
    }

    [Fact]
    public void ToJson_ProducesValidJson()
    {
        var cfg = VimiumConfig.Default;
        var json = cfg.ToJson();

        Assert.Contains("\"fontSize\"", json);
        Assert.Contains("\"14\"", json);
        Assert.Contains("\"theme\"", json);
    }

    [Fact]
    public void FromJson_Roundtrips()
    {
        var original = new VimiumConfig
        {
            FontSize = "20",
            Theme = "Dark",
            HintActiveBackground = "#FF0000",
            OverlayModifier = "Alt+X"
        };

        var json = original.ToJson();
        var restored = VimiumConfig.FromJson(json);

        Assert.Equal("20", restored.FontSize);
        Assert.Equal("Dark", restored.Theme);
        Assert.Equal("#FF0000", restored.HintActiveBackground);
        Assert.Equal("Alt+X", restored.OverlayModifier);
    }

    [Fact]
    public void FromJson_EmptyObject_ReturnsDefaults()
    {
        var cfg = VimiumConfig.FromJson("{}");
        Assert.Equal("14", cfg.FontSize);
        Assert.Equal("Light", cfg.Theme);
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsDefaults()
    {
        var cfg = VimiumConfig.FromJson("");
        Assert.NotNull(cfg);
    }

    [Fact]
    public void FromJson_MissingFields_UseDefaults()
    {
        var json = """{"fontSize": "18"}""";
        var cfg = VimiumConfig.FromJson(json);

        Assert.Equal("18", cfg.FontSize);
        Assert.Equal("Light", cfg.Theme);
    }
}
