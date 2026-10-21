using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Providers;
using Jellyfin.Plugin.Kinopoisk.Tests.Fakes;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Expected failures are one warning line; only real bugs get an error with a stack trace (KP-02).
/// </summary>
public sealed class LookupFailureTests
{
    private readonly CapturingLogger _logger = new();

    [Fact]
    public void Log_NotFound_IsWarningWithoutStackTrace()
    {
        LookupFailure.Log(_logger, new KinopoiskApiException("404", KinopoiskErrorKind.NotFound), "Robert Graf");

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("Robert Graf", entry.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Log_Transient_IsWarningWithoutStackTrace()
    {
        LookupFailure.Log(_logger, new KinopoiskApiException("503", KinopoiskErrorKind.Transient), "Матрица");

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Null(entry.Exception);
    }

    [Fact]
    public void Log_Contract_IsErrorWithException()
    {
        var exception = new KinopoiskApiException("unexpected shape");

        LookupFailure.Log(_logger, exception, "Матрица");

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
    }
}
