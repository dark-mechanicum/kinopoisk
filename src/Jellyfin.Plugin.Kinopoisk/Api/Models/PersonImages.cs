namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Photo set of a person (person REST API).
/// </summary>
/// <param name="PosterMedium">The medium poster.</param>
public sealed record PersonImages(PersonScaledImage? PosterMedium = null);
