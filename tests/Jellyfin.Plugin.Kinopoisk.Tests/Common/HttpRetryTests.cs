using System;
using System.Net;
using System.Net.Http.Headers;
using Jellyfin.Plugin.Kinopoisk.Common;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Common;

/// <summary>
/// Backoff rules of the retry helper (KP-06).
/// </summary>
public sealed class HttpRetryTests
{
    [Fact]
    public void GetDelay_HonoursRetryAfter()
        => Assert.Equal(TimeSpan.FromSeconds(3), HttpRetry.GetDelay(1, new RetryConditionHeaderValue(TimeSpan.FromSeconds(3))));

    [Fact]
    public void GetDelay_CapsRetryAfterAtTenSeconds()
        => Assert.Equal(TimeSpan.FromSeconds(10), HttpRetry.GetDelay(1, new RetryConditionHeaderValue(TimeSpan.FromMinutes(1))));

    [Theory]
    [InlineData(1, 0.4, 0.6)]
    [InlineData(2, 0.8, 1.2)]
    [InlineData(3, 1.6, 2.4)]
    public void GetDelay_BacksOffExponentiallyWithJitter(int attempt, double minSeconds, double maxSeconds)
        => Assert.InRange(HttpRetry.GetDelay(attempt, null).TotalSeconds, minSeconds, maxSeconds);

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.OK, false)]
    public void IsTransient_RetriesOnlyThrottlingAndServerErrors(HttpStatusCode status, bool expected)
        => Assert.Equal(expected, HttpRetry.IsTransient(status));
}
