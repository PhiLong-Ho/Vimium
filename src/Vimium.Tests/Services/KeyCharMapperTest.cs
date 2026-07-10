using Vimium.Services;
using Xunit;

namespace Vimium.Tests.Services;

public class KeyCharMapperTest
{
    [Theory]
    [InlineData('1', '!')]
    [InlineData('2', '@')]
    [InlineData('3', '#')]
    [InlineData('4', '$')]
    [InlineData('5', '%')]
    [InlineData('6', '^')]
    [InlineData('7', '&')]
    [InlineData('8', '*')]
    [InlineData('9', '(')]
    [InlineData('0', ')')]
    public void ShiftedDigit_MapsToUsSymbol(char digit, char expected)
    {
        Assert.Equal(expected, KeyCharMapper.ShiftedDigit(digit));
    }

    [Theory]
    [InlineData('1', '!')]
    [InlineData('2', '@')]
    [InlineData('0', ')')]
    public void MapPrintable_ShiftDigit_ReturnsSymbol(int vk, char expected)
    {
        Assert.Equal(expected, KeyCharMapper.MapPrintable(vk, shift: true));
    }

    [Theory]
    [InlineData('1')]
    [InlineData('5')]
    [InlineData('9')]
    public void MapPrintable_DigitNoShift_ReturnsDigit(int vk)
    {
        Assert.Equal((char)vk, KeyCharMapper.MapPrintable(vk, shift: false));
    }

    [Fact]
    public void MapPrintable_Letter_RespectsShiftCase()
    {
        Assert.Equal('a', KeyCharMapper.MapPrintable('A', shift: false));
        Assert.Equal('A', KeyCharMapper.MapPrintable('A', shift: true));
    }

    [Fact]
    public void MapPrintable_Space_ReturnsSpace()
    {
        Assert.Equal(' ', KeyCharMapper.MapPrintable(0x20, shift: false));
        Assert.Equal(' ', KeyCharMapper.MapPrintable(0x20, shift: true));
    }

    [Fact]
    public void MapPrintable_NonPrintable_ReturnsNull()
    {
        // 0x1B = Escape, not printable
        Assert.Null(KeyCharMapper.MapPrintable(0x1B, shift: false));
    }
}
