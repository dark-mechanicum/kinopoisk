namespace Jellyfin.Plugin.Kinopoisk.Api.Models;

/// <summary>
/// Kinopoisk image categories; serialized as SCREAMING_SNAKE_CASE (e.g. <c>FAN_ART</c>).
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
