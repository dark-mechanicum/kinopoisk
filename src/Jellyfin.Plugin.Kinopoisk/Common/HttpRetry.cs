using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Sends a request again after transient failures: 429, 5xx, network errors and timeouts.
/// Never retries other answers, and never retries once the caller cancels.
/// </summary>
internal sealed class HttpRetry
{
    private static readonly TimeSpan _firstDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan _maxRetryAfter = TimeSpan.FromSeconds(10);

    private readonly int _maxAttempts;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRetry"/> class.
    /// </summary>
    /// <param name="maxAttempts">Attempts in total, including the first one.</param>
    /// <param name="logger">The logger.</param>
    public HttpRetry(int maxAttempts, ILogger logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        _maxAttempts = maxAttempts;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether an HTTP status is worth another attempt.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns><c>true</c> for 429 and 5xx.</returns>
    public static bool IsTransient(HttpStatusCode status)
        => status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    /// <summary>
    /// Gets the pause before the next attempt: <c>Retry-After</c> (capped at 10 s), otherwise
    /// exponential backoff from 0.5 s with ±20 % jitter.
    /// </summary>
    /// <param name="attempt">The attempt that just failed, starting at 1.</param>
    /// <param name="retryAfter">The <c>Retry-After</c> header, if any.</param>
    /// <returns>The pause.</returns>
    public static TimeSpan GetDelay(int attempt, RetryConditionHeaderValue? retryAfter)
    {
        var requested = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
        if (requested is { } delay)
        {
            return delay < TimeSpan.Zero ? TimeSpan.Zero : (delay > _maxRetryAfter ? _maxRetryAfter : delay);
        }

        // Jitter only spreads retries; it needs no cryptographic randomness (CA5394 is a suggestion here).
        var jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);
        return _firstDelay * Math.Pow(2, attempt - 1) * jitter;
    }

    /// <summary>
    /// Sends a request, creating a fresh message for every attempt.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="createRequest">Creates the request.</param>
    /// <param name="cancellationToken">The caller's cancellation token.</param>
    /// <returns>The last response; non-success responses are returned, not thrown.</returns>
    public async Task<HttpResponseMessage> SendAsync(
        HttpClient client, Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var isLast = attempt >= _maxAttempts;
            using var request = createRequest();
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (!isLast)
            {
                await WaitAsync(attempt, null, ex.Message, cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (TaskCanceledException) when (!isLast && !cancellationToken.IsCancellationRequested)
            {
                // HttpClient reports its own timeout as a cancellation; the caller did not cancel.
                await WaitAsync(attempt, null, "timeout", cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (isLast || !IsTransient(response.StatusCode))
            {
                return response;
            }

            var retryAfter = response.Headers.RetryAfter;
            var reason = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            response.Dispose();
            await WaitAsync(attempt, retryAfter, reason, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task WaitAsync(int attempt, RetryConditionHeaderValue? retryAfter, string reason, CancellationToken cancellationToken)
    {
        var delay = GetDelay(attempt, retryAfter);
        _logger.LogDebug("Kinopoisk request attempt {Attempt} failed ({Reason}); retrying in {Delay}", attempt, reason, delay);
        return Task.Delay(delay, cancellationToken);
    }
}
