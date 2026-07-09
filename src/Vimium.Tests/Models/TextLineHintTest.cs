using Vimium.Models;
using Xunit;
using System;
using System.Windows;

namespace Vimium.Tests.Models;

public class TextLineHintTest
{
    [Fact]
    public void Constructor_ValidParameters_CreatesHint()
    {
        var rect = new Rect(10, 20, 100, 18);
        var hint = new TextLineHint(IntPtr.Zero, rect, "Hello World");

        Assert.Equal(rect, hint.BoundingRectangle);
        Assert.Equal(IntPtr.Zero, hint.OwningWindow);
        Assert.Equal("Hello World", hint.TextContent);
    }

    [Fact]
    public void Constructor_EmptyText_Valid()
    {
        var rect = new Rect(0, 0, 50, 14);
        var hint = new TextLineHint(new IntPtr(42), rect, "");

        Assert.Equal("", hint.TextContent);
        Assert.Equal(new IntPtr(42), hint.OwningWindow);
    }

    [Fact]
    public void Constructor_NonNullText_DoesNotThrow()
    {
        var rect = new Rect(5, 5, 200, 20);
        var hint = new TextLineHint(new IntPtr(1), rect, "Some text");
        Assert.NotNull(hint);
    }

    [Fact]
    public void Constructor_PositiveRect_StoresCorrectly()
    {
        var rect = new Rect(100, 200, 300, 400);
        var hint = new TextLineHint(new IntPtr(99), rect, "Line 1");

        Assert.Equal(100, hint.BoundingRectangle.X);
        Assert.Equal(200, hint.BoundingRectangle.Y);
        Assert.Equal(300, hint.BoundingRectangle.Width);
        Assert.Equal(400, hint.BoundingRectangle.Height);
    }
}
