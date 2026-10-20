namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// A single typed image.
/// </summary>
/// <param name="Type">The category.</param>
/// <param name="Image">The image.</param>
public sealed record FilmImage(FilmImageType Type = FilmImageType.Poster, Image? Image = null);
