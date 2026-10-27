using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Configuration;

/// <summary>
/// Reads the live plugin configuration on every access (Jellyfin replaces the object on save),
/// falling back to the defaults while the plugin is not loaded (tests, early startup).
/// </summary>
internal sealed class PluginOptions : IKinopoiskOptions
{
    private static readonly PluginConfiguration _defaults = new();

    /// <inheritdoc />
    public int CrewLimit => Current.CrewLimit;

    /// <inheritdoc />
    public bool CapitalizeGenres => Current.CapitalizeGenres;

    /// <inheritdoc />
    public int MinRequestIntervalMs => Current.MinRequestIntervalMs;

    /// <inheritdoc />
    public bool PreferIpv4 => Current.PreferIpv4;

    /// <inheritdoc />
    public bool IncludeVoiceActors => Current.IncludeVoiceActors;

    /// <inheritdoc />
    public RatingSource CommunityRating => Current.CommunityRating;

    /// <inheritdoc />
    public AgeRatingSystem AgeRating => Current.AgeRating;

    /// <inheritdoc />
    public bool TagTop250 => Current.TagTop250;

    /// <inheritdoc />
    public bool IncludePersonBiography => Current.IncludePersonBiography;

    private static PluginConfiguration Current => Plugin.Instance?.Configuration ?? _defaults;
}
