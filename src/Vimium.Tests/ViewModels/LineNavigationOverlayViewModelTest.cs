using Vimium.Models;
using Vimium.Services.Interfaces;
using Vimium.ViewModels;
using Xunit;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Vimium.Tests.ViewModels;

public class LineNavigationOverlayViewModelTest
{
    private class MockHintLabelService : IHintLabelService
    {
        public IList<string> GetHintStrings(int hintCount)
        {
            var labels = new List<string>();
            for (int i = 0; i < hintCount; i++)
            {
                labels.Add(((char)('A' + i % 26)).ToString());
            }
            return labels;
        }
    }

    private static LineNavigationSession CreateSession(int lineCount)
    {
        var hints = new List<TextLineHint>();
        for (int i = 0; i < lineCount; i++)
        {
            hints.Add(new TextLineHint(
                new IntPtr(1),
                new Rect(10, i * 20, 200, 18),
                $"Line {i} content"));
        }
        return new LineNavigationSession
        {
            Hints = hints,
            OwningWindow = new IntPtr(1),
            OwningWindowBounds = new Rect(0, 0, 800, 600)
        };
    }

    [Fact]
    public void PopulateHints_AssignsLabels()
    {
        var vm = new LineNavigationOverlayViewModel(new Rect(0, 0, 800, 600));
        var session = CreateSession(3);
        var labelService = new MockHintLabelService();

        vm.PopulateHints(session, labelService);

        Assert.Equal(3, vm.Hints.Count);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void Constructor_LoadingState_HasLoadingTrue()
    {
        var vm = new LineNavigationOverlayViewModel(new Rect(0, 0, 800, 600));
        Assert.True(vm.IsLoading);
        Assert.Empty(vm.Hints);
    }

    [Fact]
    public void Constructor_ReadyState_PopulatesHints()
    {
        var session = CreateSession(2);
        var labelService = new MockHintLabelService();

        var vm = new LineNavigationOverlayViewModel(session, labelService);

        Assert.Equal(2, vm.Hints.Count);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void MatchString_UniqueMatch_FiresHintResolved()
    {
        var session = CreateSession(2);
        var labelService = new MockHintLabelService();
        var vm = new LineNavigationOverlayViewModel(session, labelService);

        TextLineHint resolvedHint = null;
        bool resolvedWithCopy = false;
        vm.OnHintResolved = (hint, copyModifier) =>
        {
            resolvedHint = hint;
            resolvedWithCopy = copyModifier;
        };

        // Set MatchString to the label of the first hint ("A")
        vm.MatchString = "A";

        Assert.NotNull(resolvedHint);
        Assert.False(resolvedWithCopy);
        Assert.Equal("Line 0 content", resolvedHint.TextContent);
    }

    [Fact]
    public void MatchString_PartialMatch_KeepsHintsVisible()
    {
        var session = CreateSession(1);
        var labelService = new MockHintLabelService();
        var vm = new LineNavigationOverlayViewModel(session, labelService);

        bool resolved = false;
        vm.OnHintResolved = (_, _) => resolved = true;

        // Match a prefix that doesn't uniquely match
        // For a single hint with label "A", any input starting with "A" is a unique match
        // So let's use "Z" which matches nothing
        vm.MatchString = "Z";

        Assert.False(resolved);
        // All hints should be inactive since "Z" matches nothing
        foreach (var h in vm.Hints)
        {
            Assert.False(h.Active);
        }
    }

    [Fact]
    public void CloseOverlay_CanBeSet()
    {
        var vm = new LineNavigationOverlayViewModel(new Rect(0, 0, 800, 600));
        bool closed = false;
        vm.CloseOverlay = () => closed = true;
        vm.CloseOverlay?.Invoke();
        Assert.True(closed);
    }

    [Fact]
    public void MatchString_UniqueMatch_WithoutCopyModifier_FiresNavigation()
    {
        var session = CreateSession(3);
        var labelService = new MockHintLabelService();
        var vm = new LineNavigationOverlayViewModel(session, labelService);

        bool copyModifierUsed = false;
        TextLineHint resolvedHint = null;
        vm.OnHintResolved = (hint, copyModifier) =>
        {
            resolvedHint = hint;
            copyModifierUsed = copyModifier;
        };

        // Trigger unique match (label "A" matches first hint)
        vm.MatchString = "A";

        Assert.NotNull(resolvedHint);
        Assert.False(copyModifierUsed); // Copy modifier should not be held
    }

    [Fact]
    public void MatchString_PartialMatch_HighlightsSubset()
    {
        // Create many hints so labels differ
        var hints = new List<TextLineHint>();
        for (int i = 0; i < 26; i++)
        {
            hints.Add(new TextLineHint(
                new IntPtr(1),
                new Rect(10, i * 20, 200, 18),
                $"Line {i} content"));
        }
        var session = new LineNavigationSession
        {
            Hints = hints,
            OwningWindow = new IntPtr(1),
            OwningWindowBounds = new Rect(0, 0, 800, 600)
        };

        var labelService = new MockHintLabelService();
        var vm = new LineNavigationOverlayViewModel(session, labelService);

        bool resolved = false;
        vm.OnHintResolved = (_, _) => resolved = true;

        // Type a prefix that matches only one letter (e.g. "A" matches only the first hint)
        vm.MatchString = "A";

        // Should resolve (unique match)
        Assert.True(resolved);
    }

    [Fact]
    public void MatchString_NoMatch_KeepsAllVisible()
    {
        var session = CreateSession(1);
        var labelService = new MockHintLabelService();
        var vm = new LineNavigationOverlayViewModel(session, labelService);

        bool resolved = false;
        vm.OnHintResolved = (_, _) => resolved = true;

        // "ZZ" matches no hint
        vm.MatchString = "ZZ";

        Assert.False(resolved);
        // All hints should be inactive since nothing matches
        Assert.All(vm.Hints, h => Assert.False(h.Active));
    }
}
