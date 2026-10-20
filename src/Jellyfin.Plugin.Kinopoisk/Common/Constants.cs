namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Plugin-wide constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// The plugin GUID.
    /// </summary>
    public const string PluginGuid = "9b3b77c3-61a9-43a0-bfea-0be2c473e8bd";

    /// <summary>
    /// The plugin display name.
    /// </summary>
    public const string PluginName = "КиноПоиск";

    /// <summary>
    /// The plugin description.
    /// </summary>
    public const string PluginDescription = "Информация о фильмах, сериалах и персонах с КиноПоиска";

    /// <summary>
    /// The provider name used for metadata and image providers.
    /// </summary>
    public const string ProviderName = PluginName;

    /// <summary>
    /// The provider id key holding the numeric Kinopoisk id.
    /// </summary>
    public const string KinopoiskIdKey = "kinopoisk";

    /// <summary>
    /// The provider id key holding the Kinopoisk content UUID.
    /// </summary>
    public const string ContentIdKey = "kinopoisk-content";

    /// <summary>
    /// The language of fetched metadata.
    /// </summary>
    public const string MetadataLanguage = "ru";

    /// <summary>
    /// The public Kinopoisk site.
    /// </summary>
    public const string SiteUrl = "https://www.kinopoisk.ru";
}
