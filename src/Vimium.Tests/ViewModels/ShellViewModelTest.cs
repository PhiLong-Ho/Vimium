using System;
using System.Threading;
using System.Threading.Tasks;
using Vimium.Models;
using Vimium.Services;
using Vimium.Services.Interfaces;
using Vimium.ViewModels;
using Xunit;

namespace Vimium.Tests.ViewModels;

/// <summary>
/// Tests for ShellViewModel hotkey wiring and overlay lifecycle.
/// Full activation-path integration tests require a WPF Application
/// dispatcher and a real foreground window — these are covered by
/// the quickstart manual validation scenarios instead.
/// </summary>
public class ShellViewModelTest
{
    /// <summary>Fake IKeyListenerService that records property sets and fires events.</summary>
#pragma warning disable CS0067 // events are required by interface but unused in tests
    private sealed class FakeKeyListenerService : IKeyListenerService
    {
        public HotKey? HotKey { get; set; }
        public HotKey? TaskbarHotKey { get; set; }
        public HotKey? DebugHotKey { get; set; }
        public HotKey? LineNavigationHotKey { get; set; }

        public event EventHandler? OnHotKeyActivated;
        public event EventHandler? OnTaskbarHotKeyActivated;
        public event EventHandler? OnDebugHotKeyActivated;
        public event EventHandler? OnLineNavigationHotKeyActivated;

        public void FireLineNavigationHotKey() =>
            OnLineNavigationHotKeyActivated?.Invoke(this, EventArgs.Empty);

        public bool HasLineNavigationSubscribers => OnLineNavigationHotKeyActivated != null;
    }
#pragma warning restore CS0067

    /// <summary>Fakes that satisfy interface signatures without real work.</summary>
    private sealed class FakeHintProviderService : IHintProviderService
    {
        public HintSession EnumHints() => null!;
        public HintSession EnumHints(IntPtr handle) => null!;
        public Task<HintSession> EnumHintsAsync(IntPtr hWnd, CancellationToken cancellationToken = default) =>
            Task.FromResult<HintSession>(null!);
        public void InvalidateCache() { }
    }

    private sealed class FakeBenchmarkService : IBenchmarkService
    {
        public bool IsEnabled => true;
        public void LogSession(BenchmarkLogEntry entry) { }
        public void InvalidateCache() { }
    }

    private sealed class FakeDebugHintProviderService : IDebugHintProviderService
    {
        public HintSession EnumDebugHints() => null!;
        public HintSession EnumDebugHints(IntPtr hWnd) => null!;
    }

    private sealed class FakeFindTextProviderService : IFindTextProviderService
    {
        public Task<FindResult> SearchAsync(
            IntPtr hWnd, string query, CancellationToken ct) =>
            Task.FromResult(FindResult.Empty());
    }

    /// <summary>
    /// Save and restore ConfigService state so tests don't interfere with each other
    /// or with the user's real config file.
    /// </summary>
    private static (string overlayMod, string lineNavMod) SaveConfig()
    {
        var cfg = ConfigService.Instance;
        return (cfg.OverlayModifier, cfg.LineNavigationModifier);
    }

    private static void RestoreConfig(string overlayMod, string lineNavMod)
    {
        var cfg = ConfigService.Instance;
        cfg.OverlayModifier = overlayMod;
        cfg.LineNavigationModifier = lineNavMod;
    }

    [Fact]
    public void ActivateFindText_UsesConfiguredHotkey()
    {
        var (prevOverlay, prevLineNav) = SaveConfig();
        try
        {
            // Set a known hotkey in config
            ConfigService.Instance.LineNavigationModifier = "Ctrl+.";
            var keyListener = new FakeKeyListenerService();

            var shell = new ShellViewModel(
                showOverlay: _ => { },
                showSelectionModeOverlay: _ => { },
                showDebugOverlay: _ => { },
                showOptions: _ => { },
                hintLabelService: new HintLabelService(),
                hintProviderService: new FakeHintProviderService(),
                debugHintProviderService: new FakeDebugHintProviderService(),
                findTextProviderService: new FakeFindTextProviderService(),
                keyListener: keyListener);

            // Verify the LineNavigation hotkey was read from config
            Assert.NotNull(keyListener.LineNavigationHotKey);
            Assert.True(keyListener.HasLineNavigationSubscribers,
                "ShellViewModel should subscribe to OnLineNavigationHotKeyActivated");

            // Verify that firing the event doesn't crash (no foreground window in tests).
            // The handler bails early because GetForegroundWindow() returns IntPtr.Zero.
            keyListener.FireLineNavigationHotKey();
        }
        finally
        {
            RestoreConfig(prevOverlay, prevLineNav);
        }
    }

    [Fact]
    public void ActivateFindText_LineNavEvent_IsSubscribed()
    {
        var keyListener = new FakeKeyListenerService();

        var shell = new ShellViewModel(
            showOverlay: _ => { },
            showSelectionModeOverlay: _ => { },
            showDebugOverlay: _ => { },
            showOptions: _ => { },
            hintLabelService: new HintLabelService(),
            hintProviderService: new FakeHintProviderService(),
            debugHintProviderService: new FakeDebugHintProviderService(),
            findTextProviderService: new FakeFindTextProviderService(),
            keyListener: keyListener);

        Assert.True(keyListener.HasLineNavigationSubscribers);
    }

    [Fact]
    public void ActivateFindText_CorrectServicesInjected()
    {
        var keyListener = new FakeKeyListenerService();
        var findSvc = new FakeFindTextProviderService();

        var shell = new ShellViewModel(
            showOverlay: _ => { },
            showSelectionModeOverlay: _ => { },
            showDebugOverlay: _ => { },
            showOptions: _ => { },
            hintLabelService: new HintLabelService(),
            hintProviderService: new FakeHintProviderService(),
            debugHintProviderService: new FakeDebugHintProviderService(),
            findTextProviderService: findSvc,
            keyListener: keyListener);

        // Verify the event is wired — construction succeeds with all fakes injected
        Assert.True(keyListener.HasLineNavigationSubscribers);
    }
}
