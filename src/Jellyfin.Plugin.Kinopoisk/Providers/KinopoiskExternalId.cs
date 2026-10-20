using Jellyfin.Plugin.Kinopoisk.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Kinopoisk.Providers;

/// <summary>
/// Declares the Kinopoisk id, so Jellyfin shows it in "Edit metadata" and "Identify" of movies, series and persons.
/// </summary>
public sealed class KinopoiskExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => Constants.PluginName;

    /// <inheritdoc />
    public string Key => Constants.KinopoiskIdKey;

    /// <inheritdoc />
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Movie or Series or Person;
}
