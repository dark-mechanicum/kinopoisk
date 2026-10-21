namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Kinopoisk image categories the plugin requests; the GraphQL literal is SCREAMING_SNAKE_CASE (e.g. <c>FAN_ART</c>).
/// Responses carry the category as text (<see cref="FilmImage.Type"/>), because Kinopoisk adds new ones.
/// </summary>
public enum FilmImageType
{
    /// <summary>Poster.</summary>
    Poster,

    /// <summary>Cover.</summary>
    Cover,

    /// <summary>Film still.</summary>
    Still,

    /// <summary>Wallpaper.</summary>
    Wallpaper,

    /// <summary>Screenshot.</summary>
    Screenshot,

    /// <summary>Fan art.</summary>
    FanArt,
}
