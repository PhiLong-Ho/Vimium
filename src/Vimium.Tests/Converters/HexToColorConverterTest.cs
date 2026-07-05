using System.Windows.Media;
using Vimium.Converters;
using Xunit;

namespace Vimium.Tests.Converters;

public class HexToColorConverterTest
{
    [Fact]
    public void Convert_ValidHex_ReturnsBrush()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert("#FF0000", typeof(Brush), null, null);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Red, brush.Color);
    }

    [Fact]
    public void Convert_Yellow()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert("#FFFF00", typeof(Brush), null, null);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Colors.Yellow, brush.Color);
    }

    [Fact]
    public void Convert_DarkColor()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert("#2A2A2A", typeof(Brush), null, null);
        var brush = (SolidColorBrush)result;
        Assert.Equal(0x2A, brush.Color.R);
        Assert.Equal(0x2A, brush.Color.G);
        Assert.Equal(0x2A, brush.Color.B);
    }

    [Fact]
    public void Convert_Null_ReturnsTransparent()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert(null, typeof(Brush), null, null);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Colors.Transparent, brush.Color);
    }

    [Fact]
    public void Convert_Empty_ReturnsTransparent()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert("", typeof(Brush), null, null);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Colors.Transparent, brush.Color);
    }

    [Fact]
    public void Convert_Invalid_ReturnsTransparent()
    {
        var converter = new HexToColorConverter();
        var result = converter.Convert("notacolor", typeof(Brush), null, null);
        var brush = (SolidColorBrush)result;
        Assert.Equal(Colors.Transparent, brush.Color);
    }

    [Fact]
    public void ConvertBack_RedBrush_ReturnsHex()
    {
        var converter = new HexToColorConverter();
        var brush = new SolidColorBrush(Colors.Red);
        var result = converter.ConvertBack(brush, typeof(string), null, null);
        Assert.Equal("#FF0000", result);
    }

    [Fact]
    public void ConvertBack_Null_ReturnsBlack()
    {
        var converter = new HexToColorConverter();
        var result = converter.ConvertBack(null, typeof(string), null, null);
        Assert.Equal("#000000", result);
    }
}
