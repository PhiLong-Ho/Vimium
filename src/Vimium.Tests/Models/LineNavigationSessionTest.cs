using Vimium.Models;
using Xunit;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Vimium.Tests.Models;

public class LineNavigationSessionTest
{
    [Fact]
    public void Constructor_Default_EmptyHints()
    {
        var session = new LineNavigationSession();

        Assert.Null(session.Hints);
        Assert.Equal(IntPtr.Zero, session.OwningWindow);
        Assert.Equal(0, session.OwningWindowBounds.Width);
        Assert.Equal(0, session.OwningWindowBounds.Height);
    }

    [Fact]
    public void HintCollection_CanBeAssigned()
    {
        var hints = new List<TextLineHint>
        {
            new TextLineHint(new IntPtr(1), new Rect(0, 0, 100, 20), "Line 1"),
            new TextLineHint(new IntPtr(1), new Rect(0, 20, 100, 20), "Line 2"),
        };

        var session = new LineNavigationSession
        {
            Hints = hints,
            OwningWindow = new IntPtr(1),
            OwningWindowBounds = new Rect(0, 0, 800, 600)
        };

        Assert.Equal(2, session.Hints.Count);
        Assert.Equal(new IntPtr(1), session.OwningWindow);
        Assert.Equal(800, session.OwningWindowBounds.Width);
        Assert.Equal(600, session.OwningWindowBounds.Height);
    }

    [Fact]
    public void OwningWindowBounds_StoredAsLogicalCoords()
    {
        var bounds = new Rect(100, 200, 1024, 768);
        var session = new LineNavigationSession
        {
            OwningWindowBounds = bounds
        };

        Assert.Equal(bounds, session.OwningWindowBounds);
    }
}
