namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A gallery photo of a person (person REST API).
/// </summary>
/// <param name="BaseUrl">The avatars base url.</param>
public sealed record PersonGalleryItem(string? BaseUrl = null);
