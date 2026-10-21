namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// The <c>data.suggest</c> node of <c>SuggestSearch</c>; <c>top</c> is sometimes null, which means "no results".
/// </summary>
/// <param name="Top">The matches.</param>
internal sealed record SuggestResult(SuggestTop? Top = null);
