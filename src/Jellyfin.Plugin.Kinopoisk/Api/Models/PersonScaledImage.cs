namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// An image in 1x and 2x density (person REST API).
/// </summary>
/// <param name="X1">The 1x url.</param>
/// <param name="X2">The 2x url.</param>
public sealed record PersonScaledImage(string? X1 = null, string? X2 = null);
