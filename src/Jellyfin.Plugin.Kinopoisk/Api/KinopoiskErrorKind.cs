namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// What kind of failure a <see cref="KinopoiskApiException"/> reports.
/// </summary>
public enum KinopoiskErrorKind
{
    /// <summary>Kinopoisk answered with something the plugin does not understand.</summary>
    Contract,

    /// <summary>Kinopoisk has no entry with the requested id.</summary>
    NotFound,

    /// <summary>Kinopoisk or the network is temporarily unavailable (429, 5xx, timeouts), even after retries.</summary>
    Transient,
}
