using Jellyfin.Plugin.Kinopoisk.Api;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// How providers report a failed Kinopoisk lookup: expected situations as one warning line,
/// real bugs as an error with the stack trace.
/// </summary>
internal static class LookupFailure
{
    /// <summary>
    /// Logs a failed lookup.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="subject">What was looked up, for the message.</param>
    public static void Log(ILogger logger, KinopoiskApiException exception, string? subject)
    {
        switch (exception.Kind)
        {
            case KinopoiskErrorKind.NotFound:
                logger.LogWarning(
                    "Kinopoisk has no entry for {Subject}; the stored id may be outdated: {Reason}", subject, exception.Message);
                break;
            case KinopoiskErrorKind.Transient:
                logger.LogWarning(
                    "Kinopoisk is unavailable for {Subject}, it will be retried on the next refresh: {Reason}", subject, exception.Message);
                break;
            default:
                logger.LogError(exception, "Kinopoisk lookup failed for {Subject}", subject);
                break;
        }
    }
}
