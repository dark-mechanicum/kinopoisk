namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Wrapper of the best <c>SuggestSearch</c> match.
/// </summary>
/// <param name="Global">The match.</param>
internal sealed record SuggestTopResult(SuggestGlobal? Global = null);
