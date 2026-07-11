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
        Assert.Equal("Ctrl+.", cfg.LineNavigationModifier);
        Assert.Equal("Ctrl", cfg.CopyModifier);
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

    [Fact]
    public void LineNavigationModifier_DefaultValue()
    {
        var cfg = VimiumConfig.Default;
        Assert.Equal("Ctrl+.", cfg.LineNavigationModifier);
    }

    [Fact]
    public void CopyModifier_DefaultValue()
    {
        var cfg = VimiumConfig.Default;
        Assert.Equal("Ctrl", cfg.CopyModifier);
    }

    [Fact]
    public void LineNavigationFields_Roundtrip()
    {
        var original = new VimiumConfig
        {
            LineNavigationModifier = "Ctrl+/",
            CopyModifier = "Alt"
        };

        var json = original.ToJson();
        var restored = VimiumConfig.FromJson(json);

        Assert.Equal("Ctrl+/", restored.LineNavigationModifier);
        Assert.Equal("Alt", restored.CopyModifier);
    }

    [Fact]
    public void LineNavigationFields_AbsentKeys_UseDefaults()
    {
        var json = """{"fontSize": "16"}""";
        var cfg = VimiumConfig.FromJson(json);

        // New fields should fall back to defaults when absent from JSON
        Assert.Equal("Ctrl+.", cfg.LineNavigationModifier);
        Assert.Equal("Ctrl", cfg.CopyModifier);
    }

    [Fact]
    public void LineNavigationFields_SerializeAsCamelCase()
    {
        var cfg = new VimiumConfig
        {
            LineNavigationModifier = "Alt+.",
            CopyModifier = "Shift"
        };
        var json = cfg.ToJson();

        Assert.Contains("\"lineNavigationModifier\"", json);
        Assert.Contains("\"copyModifier\"", json);
    }

    // ── RunAsAdministrator (feature 005) ─────────────────────

    [Fact]
    public void RunAsAdministrator_DefaultsToFalse()
    {
        var cfg = VimiumConfig.Default;
        Assert.False(cfg.RunAsAdministrator);
    }

    [Fact]
    public void RunAsAdministrator_Roundtrips_WhenFalse()
    {
        var original = new VimiumConfig { RunAsAdministrator = false };

        var json = original.ToJson();
        var restored = VimiumConfig.FromJson(json);

        Assert.False(restored.RunAsAdministrator);
    }

    [Fact]
    public void RunAsAdministrator_AbsentKey_DefaultsToFalse()
    {
        // A config that predates the key (or any config omitting it) must fall
        // back to the non-elevated default — enterprise-managed machines expect
        // no UAC prompt unless the user explicitly opts in.
        var json = """{"fontSize": "16", "theme": "Dark"}""";
        var cfg = VimiumConfig.FromJson(json);

        Assert.False(cfg.RunAsAdministrator);
    }

    [Fact]
    public void RunAsAdministrator_ExplicitFalse_IsPreserved()
    {
        var json = """{"runAsAdministrator": false}""";
        var cfg = VimiumConfig.FromJson(json);

        Assert.False(cfg.RunAsAdministrator);
    }

    [Fact]
    public void RunAsAdministrator_Default_WrittenAsFalse()
    {
        // Forced to always serialize (JsonIgnoreCondition.Never) so the key is
        // always present in config.json — the non-elevated default is explicit
        // and discoverable, and a chosen value round-trips.
        var cfg = VimiumConfig.Default;
        var json = cfg.ToJson();

        Assert.Contains("\"runAsAdministrator\": false", json);
    }

    [Fact]
    public void RunAsAdministrator_False_WrittenAsCamelCase()
    {
        var cfg = new VimiumConfig { RunAsAdministrator = false };
        var json = cfg.ToJson();

        Assert.Contains("\"runAsAdministrator\"", json);
        Assert.Contains("false", json);
    }
}
