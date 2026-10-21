using System;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Kinopoisk.Configuration;

/// <summary>
/// Plugin configuration, stored by Jellyfin. The defaults keep the behaviour of releases without settings.
/// </summary>
public sealed class PluginConfiguration : BasePluginConfiguration, IKinopoiskOptions
{
    /// <summary>The smallest crew limit.</summary>
    public const int MinCrewLimit = 1;

    /// <summary>The largest crew limit.</summary>
    public const int MaxCrewLimit = 200;

    /// <summary>The largest pause between requests, in milliseconds.</summary>
    public const int MaxRequestIntervalMs = 5000;

    private int _crewLimit = 30;
    private int _minRequestIntervalMs;

    /// <inheritdoc />
    public int CrewLimit
    {
        get => _crewLimit;
        set => _crewLimit = Math.Clamp(value, MinCrewLimit, MaxCrewLimit);
    }

    /// <inheritdoc />
    public bool CapitalizeGenres { get; set; } = true;

    /// <inheritdoc />
    public int MinRequestIntervalMs
    {
        get => _minRequestIntervalMs;
        set => _minRequestIntervalMs = Math.Clamp(value, 0, MaxRequestIntervalMs);
    }

    /// <inheritdoc />
    public bool PreferIpv4 { get; set; }

    /// <inheritdoc />
    public bool IncludeVoiceActors { get; set; }

    /// <inheritdoc />
    public RatingSource CommunityRating { get; set; } = RatingSource.Kinopoisk;
}
