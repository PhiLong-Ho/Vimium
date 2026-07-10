using Vimium.Models;
using Vimium.Services;
using Vimium.Services.Interfaces;
using Vimium.ViewModels;
using Xunit;
using System.Windows;

namespace Vimium.Tests.ViewModels;

/// <summary>
/// Tests for OverlayViewModel multi-slot action resolution and hint population.
/// </summary>
public class OverlayViewModelTest
{
    [Fact]
    public void ActionSlots_DefaultConfiguration_IsFourSlots()
    {
        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));

        Assert.NotNull(vm.ActionSlots);
        Assert.Equal(4, vm.ActionSlots.Length);
    }

    [Fact]
    public void ActionSlots_Slot0_IsDefaultInvoke()
    {
        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));

        Assert.Equal(0, vm.ActionSlots[0].SlotIndex);
        Assert.Equal("", vm.ActionSlots[0].Modifier);
        Assert.Equal(HintAction.Invoke, vm.ActionSlots[0].Action);
    }

    [Fact]
    public void ActionSlots_Slot1_DefaultIsShiftLeftClick()
    {
        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));

        Assert.Equal(1, vm.ActionSlots[1].SlotIndex);
        Assert.Equal("Shift", vm.ActionSlots[1].Modifier);
        Assert.Equal(HintAction.LeftClick, vm.ActionSlots[1].Action);
    }

    [Fact]
    public void ActionSlots_CanBeOverridden()
    {
        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600))
        {
            ActionSlots = new[]
            {
                new ActionSlot { SlotIndex = 0, Modifier = "", Action = HintAction.Invoke },
                new ActionSlot { SlotIndex = 1, Modifier = "Ctrl+Shift", Action = HintAction.MoveMouse },
                new ActionSlot { SlotIndex = 2, Modifier = "Alt", Action = HintAction.MoveMouse },
                new ActionSlot { SlotIndex = 3, Modifier = "Win", Action = HintAction.LeftClick },
            }
        };

        Assert.Equal(4, vm.ActionSlots.Length);
        Assert.Equal("Ctrl+Shift", vm.ActionSlots[1].Modifier);
        Assert.Equal(HintAction.MoveMouse, vm.ActionSlots[1].Action);
        Assert.Equal("Alt", vm.ActionSlots[2].Modifier);
        Assert.Equal(HintAction.MoveMouse, vm.ActionSlots[2].Action);
        Assert.Equal("Win", vm.ActionSlots[3].Modifier);
        Assert.Equal(HintAction.LeftClick, vm.ActionSlots[3].Action);
    }

    [Fact]
    public void PopulateHints_AppliesBufferedPendingInput()
    {
        var session = new HintSession
        {
            Hints = new System.Collections.Generic.List<Hint>
            {
                new TestHint((IntPtr)1, new Rect(0, 0, 100, 20)),
                new TestHint((IntPtr)1, new Rect(0, 30, 100, 20)),
            },
            OwningWindow = (IntPtr)123,
        };

        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));
        vm.PendingInput = "S";

        var labelService = new HintLabelService();
        vm.PopulateHints(session, labelService);

        Assert.False(vm.IsLoading);
        Assert.Equal(2, vm.Hints.Count);
        // After PopulateHints, pending input is consumed and MatchString was called
        Assert.Equal("", vm.PendingInput);
    }

    [Fact]
    public void PopulateHints_ClearsLoadingState()
    {
        var session = new HintSession
        {
            Hints = new System.Collections.Generic.List<Hint>(),
            OwningWindow = (IntPtr)456,
        };

        var vm = new OverlayViewModel(new Rect(0, 0, 1024, 768));
        var labelService = new HintLabelService();
        vm.PopulateHints(session, labelService);

        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void IsLoading_InitialState_IsTrue()
    {
        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));
        Assert.True(vm.IsLoading);
    }

    [Fact]
    public void MatchString_SingleMatch_DoesNotThrow()
    {
        var session = new HintSession
        {
            Hints = new System.Collections.Generic.List<Hint>
            {
                new TestHint((IntPtr)1, new Rect(0, 0, 100, 20)),
            },
            OwningWindow = (IntPtr)123,
        };

        var vm = new OverlayViewModel(new Rect(0, 0, 800, 600));
        var labelService = new HintLabelService();
        vm.PopulateHints(session, labelService);

        // Setting MatchString with a full match should not throw
        var label = vm.Hints[0].Label;
        vm.CloseOverlay = () => { }; // no-op close
        vm.MatchString = label;
    }

    /// <summary>Test hint that doesn't require real UIA COM.</summary>
    private sealed class TestHint : Hint
    {
        public TestHint(IntPtr owningWindow, Rect boundingRect)
            : base(owningWindow, boundingRect) { }

        public override void Invoke() { }
    }
}
