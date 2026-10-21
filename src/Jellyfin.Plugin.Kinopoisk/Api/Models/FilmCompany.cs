namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A company in a release (distributor, network or streamer).
/// </summary>
/// <param name="DisplayName">The name.</param>
public sealed record FilmCompany(string? DisplayName = null);
