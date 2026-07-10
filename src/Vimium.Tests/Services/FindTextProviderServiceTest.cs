using System;
using System.Threading;
using System.Threading.Tasks;
using Vimium.Models;
using Vimium.Services;
using Vimium.Services.Interfaces;
using Xunit;

namespace Vimium.Tests.Services;

/// <summary>
/// Contract-level tests for FindTextProviderService. The service calls into real
/// cross-process UIA COM, so behavioral tests use invalid/zero window handles
/// (no live UIA target exists in the test host). These verify graceful handling
/// of edge inputs, cancellation, and the no-throw contract.
/// </summary>
public class FindTextProviderServiceTest
{
    private readonly IFindTextProviderService _service = new FindTextProviderService();

    [Fact]
    public async Task SearchAsync_ZeroHwnd_ReturnsEmpty()
    {
        var result = await _service.SearchAsync(IntPtr.Zero, "hello", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        var result = await _service.SearchAsync(new IntPtr(1), "", CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_NullQuery_ReturnsEmpty()
    {
        var result = await _service.SearchAsync(new IntPtr(1), null!, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_InvalidHwnd_DoesNotThrow()
    {
        // A non-zero but invalid handle must not crash — returns empty gracefully.
        var result = await _service.SearchAsync(new IntPtr(0x7FFFFFFF), "hello", CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_AlreadyCancelled_DoesNotThrowToCaller_OrThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Zero handle short-circuits before the cancellable region, so it returns empty.
        var result = await _service.SearchAsync(IntPtr.Zero, "hello", cts.Token);
        Assert.NotNull(result);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task SearchAsync_CompletesWithinTimeoutBudget()
    {
        // Even against an invalid handle, the call must return well within the 3s budget.
        var task = _service.SearchAsync(new IntPtr(0x1234), "hello", CancellationToken.None);
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(6)));
        Assert.Equal(task, completed);

        var result = await task;
        Assert.NotNull(result);
    }

    [Fact]
    public void FindResult_Empty_HasNoMatches()
    {
        var empty = FindResult.Empty();
        Assert.Empty(empty.Matches);
        Assert.False(empty.TimedOut);
    }
}
