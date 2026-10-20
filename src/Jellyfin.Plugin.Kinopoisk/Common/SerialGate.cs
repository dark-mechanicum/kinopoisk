using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Lets one job run at a time, in arrival order, so Kinopoisk never sees parallel bursts.
/// </summary>
public sealed class SerialGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

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
            return await job().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() => _semaphore.Dispose();
}
