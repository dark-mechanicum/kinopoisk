namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Curated gallery of a film or series.
/// </summary>
/// <param name="Covers">The covers.</param>
/// <param name="Logos">The logos.</param>
/// <param name="Posters">The posters.</param>
public sealed record FilmGallery(FilmGalleryImages? Covers = null, FilmGalleryImages? Logos = null, FilmGalleryImages? Posters = null);
