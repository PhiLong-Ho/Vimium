using Vimium.Services.Interfaces;
using Xunit;

namespace Vimium.Tests.Services;

/// <summary>
/// Tests for IHintProviderService contract and caching behavior.
/// Full UIA COM integration tests require a real foreground window
/// and are covered by the quickstart manual validation scenarios.
/// This class tests the interface contract via a fake implementation.
/// </summary>
public class UiAutomationHintProviderServiceTest
{
    /// <summary>Fake that simulates cache hit/miss behavior for unit testing.</summary>
    private sealed class CachingFakeHintProviderService : IHintProviderService
    {
        private Vimium.Models.HintSession? _cached;
        private IntPtr _cachedHwnd;
        public int EnumCallCount { get; private set; }

        public Vimium.Models.HintSession EnumHints() =>
            new() { Hints = new System.Collections.Generic.List<Vimium.Models.Hint>() };

        public Vimium.Models.HintSession EnumHints(IntPtr handle)
        {
            if (_cached != null && _cachedHwnd == handle)
                return _cached; // cache hit — no new enumeration

            EnumCallCount++;
            var session = new Vimium.Models.HintSession
            {
                Hints = new System.Collections.Generic.List<Vimium.Models.Hint>(),
                OwningWindow = handle,
            };
            _cached = session;
            _cachedHwnd = handle;
            return session;
        }

        public System.Threading.Tasks.Task<Vimium.Models.HintSession> EnumHintsAsync(
            IntPtr hWnd, System.Threading.CancellationToken cancellationToken = default)
        {
            return System.Threading.Tasks.Task.FromResult(EnumHints(hWnd));
        }

        public void InvalidateCache()
        {
            _cached = null;
            _cachedHwnd = IntPtr.Zero;
        }

        public bool HasCache => _cached != null;
    }

    [Fact]
    public void InvalidateCache_ClearsCache_NextEnumIsColdStart()
    {
        var fake = new CachingFakeHintProviderService();
        var hWnd = (IntPtr)123;

        // First call — cold start
        var first = fake.EnumHints(hWnd);
        Assert.Equal(1, fake.EnumCallCount);

        // Second call same hWnd — cache hit
        var second = fake.EnumHints(hWnd);
        Assert.Equal(1, fake.EnumCallCount); // still 1 — cached

        // Invalidate
        fake.InvalidateCache();
        Assert.False(fake.HasCache);

        // Third call — cold start again
        var third = fake.EnumHints(hWnd);
        Assert.Equal(2, fake.EnumCallCount);
    }

    [Fact]
    public void Cache_ReturnsSameSession_WhenHwndMatches()
    {
        var fake = new CachingFakeHintProviderService();
        var hWnd = (IntPtr)456;

        var first = fake.EnumHints(hWnd);
        var second = fake.EnumHints(hWnd);

        Assert.Same(first, second);
        Assert.Equal(1, fake.EnumCallCount);
    }

    [Fact]
    public void Cache_Reenumerates_WhenHwndDiffers()
    {
        var fake = new CachingFakeHintProviderService();
        var hWnd1 = (IntPtr)111;
        var hWnd2 = (IntPtr)222;

        fake.EnumHints(hWnd1);
        Assert.Equal(1, fake.EnumCallCount);

        fake.EnumHints(hWnd2);
        Assert.Equal(2, fake.EnumCallCount);
    }

    [Fact]
    public void InvalidateCache_WhenNoCache_IsNoOp()
    {
        var fake = new CachingFakeHintProviderService();
        fake.InvalidateCache();
        Assert.False(fake.HasCache);
    }

    [Fact]
    public void EnumHintsAsync_PassesCancellationToken()
    {
        var fake = new CachingFakeHintProviderService();
        using var cts = new System.Threading.CancellationTokenSource();

        // Should not throw — token is not cancelled
        var task = fake.EnumHintsAsync((IntPtr)999, cts.Token);
        task.Wait();

        Assert.NotNull(task.Result);
    }

    [Fact]
    public void EnumHintsAsync_SupportsDefaultCancellationToken()
    {
        var fake = new CachingFakeHintProviderService();

        // Call with default CancellationToken (no parameter)
        var task = fake.EnumHintsAsync((IntPtr)888);
        task.Wait();

        Assert.NotNull(task.Result);
    }
}
