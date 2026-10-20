using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Common;

/// <summary>
/// The request gate that keeps Kinopoisk calls sequential.
/// </summary>
public sealed class SerialGateTests : IDisposable
{
    private readonly SerialGate _gate = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Fact]
    public async Task RunAsync_NeverRunsJobsInParallel()
    {
        var running = 0;
        var maxRunning = 0;

        async Task<int> JobAsync(int value)
        {
            var now = Interlocked.Increment(ref running);
            InterlockedMax(ref maxRunning, now);
            await Task.Delay(5, _token);
            Interlocked.Decrement(ref running);
            return value;
        }

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(n => _gate.RunAsync(() => JobAsync(n), _token)));

        Assert.Equal(1, maxRunning);
        Assert.Equal(Enumerable.Range(0, 20), results);
    }

    [Fact]
    public async Task RunAsync_PropagatesJobException()
        => await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gate.RunAsync<int>(() => throw new InvalidOperationException("boom"), _token));

    [Fact]
    public async Task RunAsync_KeepsWorkingAfterFailedJob()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gate.RunAsync<int>(() => throw new InvalidOperationException("boom"), _token));

        Assert.Equal(42, await _gate.RunAsync(() => Task.FromResult(42), _token));
    }

    [Fact]
    public async Task RunAsync_HonoursCancellationWhileWaiting()
    {
        var release = new TaskCompletionSource<int>();
        var blocker = _gate.RunAsync(() => release.Task, _token);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _gate.RunAsync(() => Task.FromResult(1), cancellation.Token));

        release.SetResult(0);
        await blocker;
    }

    public void Dispose() => _gate.Dispose();

    private static void InterlockedMax(ref int target, int value)
    {
        int current;
        while (value > (current = target) && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }
}
