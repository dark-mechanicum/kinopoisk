using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// The <c>data.suggest.top</c> node of <c>SuggestSearch</c>.
/// </summary>
internal sealed record SuggestTop
{
    /// <summary>Gets the best match of any kind.</summary>
    [JsonPropertyName("topResult")]
    public SuggestTopResult? TopResult { get; init; }

    /// <summary>Gets matching films and series (each wrapped as <c>{ movie }</c>).</summary>
    public IReadOnlyList<SuggestMovie> Movies { get; init; } = [];

    /// <summary>Gets matching persons (each wrapped as <c>{ person }</c>).</summary>
    public IReadOnlyList<FilmCrewMember> Persons { get; init; } = [];
}
