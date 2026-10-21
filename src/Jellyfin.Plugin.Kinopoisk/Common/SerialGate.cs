using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Lets one job run at a time, in arrival order, so Kinopoisk never sees parallel bursts;
/// optionally keeps a minimum pause between the end of one job and the start of the next.
/// </summary>
public sealed class SerialGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Func<TimeSpan> _minimumInterval;
    private long _lastFinished;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerialGate"/> class without a pause between jobs.
    /// </summary>
    public SerialGate()
        : this(static () => TimeSpan.Zero)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerialGate"/> class.
    /// </summary>
    /// <param name="minimumInterval">Gets the pause between jobs; read for every job, so it can change at runtime.</param>
    public SerialGate(Func<TimeSpan> minimumInterval)
    {
        _minimumInterval = minimumInterval;
    }

    /// <summary>
    /// Runs a job once all earlier jobs have finished.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="job">The job.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The job result.</returns>
    public async Task<T> RunAsync<T>(Func<Task<T>> job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var wait = _minimumInterval() - Stopwatch.GetElapsedTime(_lastFinished);
            if (_lastFinished != 0 && wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            }

            return await job().ConfigureAwait(false);
        }
        finally
        {
            _lastFinished = Stopwatch.GetTimestamp();
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _semaphore.Dispose();
}
