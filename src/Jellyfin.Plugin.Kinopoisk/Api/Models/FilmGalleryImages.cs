namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Curated gallery images of one kind, by orientation.
/// </summary>
public sealed record FilmGalleryImages
{
    /// <summary>Gets the square image.</summary>
    public Image? Square { get; init; }

    /// <summary>Gets the horizontal image.</summary>
    public Image? Horizontal { get; init; }

    /// <summary>Gets the vertical image.</summary>
    public Image? Vertical { get; init; }

    /// <summary>Gets the marketing vertical image.</summary>
    public Image? MarketingVertical { get; init; }

    /// <summary>Gets the HD vertical image (search results).</summary>
    public Image? HdVertical { get; init; }

    /// <summary>Gets the Kinopoisk vertical image.</summary>
    public Image? KpVertical { get; init; }
}
