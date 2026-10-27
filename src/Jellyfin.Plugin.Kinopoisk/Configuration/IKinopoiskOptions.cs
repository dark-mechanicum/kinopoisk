using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Configuration;

/// <summary>
/// The plugin settings as the code reads them.
/// </summary>
public interface IKinopoiskOptions
{
    /// <summary>Gets the number of actors (and voice actors) to fetch, 1-200.</summary>
    int CrewLimit { get; }

    /// <summary>Gets a value indicating whether genres get a capital first letter.</summary>
    bool CapitalizeGenres { get; }

    /// <summary>Gets the minimum pause between two Kinopoisk requests, in milliseconds.</summary>
    int MinRequestIntervalMs { get; }

    /// <summary>Gets a value indicating whether IPv4 addresses are tried before IPv6 ones.</summary>
    bool PreferIpv4 { get; }

    /// <summary>Gets a value indicating whether dubbing actors are added to the cast.</summary>
    bool IncludeVoiceActors { get; }

    /// <summary>Gets the preferred source of the community rating; the other one is the fallback.</summary>
    RatingSource CommunityRating { get; }

    /// <summary>Gets the preferred age rating system of the official rating; the other one is the fallback.</summary>
    AgeRatingSystem AgeRating { get; }

    /// <summary>Gets a value indicating whether titles in a Kinopoisk Top-250 get a tag.</summary>
    bool TagTop250 { get; }

    /// <summary>Gets a value indicating whether a person's overview is built from the Kinopoisk facts.</summary>
    bool IncludePersonBiography { get; }
}
