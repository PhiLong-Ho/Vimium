using System.Windows.Forms;
using Vimium.NativeMethods;
using Vimium.Services.Interfaces;
using Xunit;

namespace Vimium.Tests.Services;

public class HotKeyTest
{
    [Fact]
    public void Parse_CtrlSemicolon()
    {
        var hk = HotKey.Parse("Ctrl+;");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.OemSemicolon, hk.Keys);
    }

    [Fact]
    public void Parse_CtrlQuote()
    {
        var hk = HotKey.Parse("Ctrl+'");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.Oem7, hk.Keys);
    }

    [Fact]
    public void Parse_AltShift_Comma()
    {
        var hk = HotKey.Parse("Alt+Shift+,");
        Assert.Equal(KeyModifier.Alt | KeyModifier.Shift, hk.Modifier);
        Assert.Equal(Keys.Oemcomma, hk.Keys);
    }

    [Fact]
    public void Parse_Ctrl_Letter()
    {
        var hk = HotKey.Parse("Ctrl+L");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.L, hk.Keys);
    }

    [Fact]
    public void Parse_LowercaseLetter()
    {
        var hk = HotKey.Parse("Ctrl+a");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.A, hk.Keys);
    }

    [Fact]
    public void Parse_Digit1()
    {
        var hk = HotKey.Parse("Ctrl+1");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.D1, hk.Keys);
    }

    [Fact]
    public void Parse_Digit9()
    {
        var hk = HotKey.Parse("Ctrl+9");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.D9, hk.Keys);
    }

    [Fact]
    public void Parse_Win_Space()
    {
        var hk = HotKey.Parse("Win+Space");
        Assert.Equal(KeyModifier.Windows, hk.Modifier);
        Assert.Equal(Keys.Space, hk.Keys);
    }

    [Fact]
    public void Parse_Alt_Period()
    {
        var hk = HotKey.Parse("Alt+.");
        Assert.Equal(KeyModifier.Alt, hk.Modifier);
        Assert.Equal(Keys.OemPeriod, hk.Keys);
    }

    [Fact]
    public void Parse_NoPlus_ReturnsControlWithNone()
    {
        // Without "+", no key parsing happens — returns default
        var hk = HotKey.Parse("garbage");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.None, hk.Keys);
    }

    [Fact]
    public void Parse_SingleChar_NoPlus_ReturnsControlWithNone()
    {
        var hk = HotKey.Parse("x");
        Assert.Equal(KeyModifier.Control, hk.Modifier);
        Assert.Equal(Keys.None, hk.Keys);
    }
}
