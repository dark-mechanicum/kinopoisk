using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Parses Kinopoisk JSON and extracts the requested data with clear errors.
/// </summary>
internal static class KinopoiskJson
{
    private const int SnippetLength = 300;
    private const string NotFoundErrorCode = "NotFoundError";

    // Kinopoisk adds enum values without notice, so API strings are never deserialized into closed enums.
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly Dictionary<string, FilmImageType> _imageTypesByLiteral =
        Enum.GetValues<FilmImageType>().ToDictionary(ToLiteral, StringComparer.Ordinal);

    /// <summary>
    /// Gets the GraphQL enum literal of an image category, e.g. <c>FAN_ART</c>.
    /// </summary>
    /// <param name="type">The category.</param>
    /// <returns>The literal.</returns>
    public static string ToLiteral(FilmImageType type) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(type.ToString());

    /// <summary>
    /// Reads a GraphQL image category literal.
    /// </summary>
    /// <param name="literal">The literal, e.g. <c>FAN_ART</c>.</param>
    /// <returns>The category, or null for a value the plugin does not know.</returns>
    public static FilmImageType? ParseImageType(string? literal)
        => literal is not null && _imageTypesByLiteral.TryGetValue(literal, out var type) ? type : null;

    /// <summary>
    /// Parses a response body.
    /// </summary>
    /// <param name="text">The body.</param>
    /// <returns>The root element.</returns>
    public static JsonElement Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new KinopoiskApiException("Kinopoisk returned an empty document.");
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            throw new KinopoiskApiException($"Kinopoisk returned invalid JSON: {Snippet(text)}", ex);
        }
    }

    /// <summary>
    /// Walks a dot separated path; the error explains what Kinopoisk said instead and is
    /// <see cref="KinopoiskErrorKind.NotFound"/> when Kinopoisk reports the requested entry as missing.
    /// </summary>
    /// <param name="root">The response root.</param>
    /// <param name="path">The path.</param>
    /// <returns>The element.</returns>
    public static JsonElement Walk(JsonElement root, string path)
    {
        var segments = path.Split('.');
        var current = root;
        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current)
                || current.ValueKind == JsonValueKind.Null)
            {
                throw new KinopoiskApiException(
                    $"Kinopoisk response has no '{path}' (missing '{segment}'). {DescribeErrors(root)}".TrimEnd(),
                    IsNotFound(root, segments) ? KinopoiskErrorKind.NotFound : KinopoiskErrorKind.Contract);
            }
        }

        return current;
    }

    /// <summary>
    /// Deserializes an element.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="element">The element.</param>
    /// <param name="source">Where the element came from, for error messages.</param>
    /// <returns>The value.</returns>
    public static T Deserialize<T>(JsonElement element, string source)
    {
        try
        {
            return element.Deserialize<T>(_serializerOptions)
                ?? throw new KinopoiskApiException($"Kinopoisk returned null for '{source}'.");
        }
        catch (JsonException ex)
        {
            throw new KinopoiskApiException($"Kinopoisk returned an unexpected shape for '{source}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Shortens a body for error messages.
    /// </summary>
    /// <param name="text">The body.</param>
    /// <returns>The snippet.</returns>
    public static string Snippet(string text) => text.Length <= SnippetLength ? text : string.Concat(text.AsSpan(0, SnippetLength), "…");

    private static string DescribeErrors(JsonElement root)
    {
        var messages = GetErrors(root)
            .Select(e => e.TryGetProperty("message", out var m) ? m.GetString() : null)
            .OfType<string>()
            .ToList();
        return messages.Count == 0 ? string.Empty : "Errors: " + string.Join("; ", messages);
    }

    /// <summary>
    /// Checks for a <c>NotFoundError</c> on the requested root field. Other fields of the same document may
    /// legitimately be missing (e.g. <c>tvSeries</c> of a film), so only the requested one counts.
    /// </summary>
    private static bool IsNotFound(JsonElement root, string[] segments)
    {
        var field = segments is ["data", var requested, ..] ? requested : segments[0];
        return GetErrors(root).Any(e =>
            e.TryGetProperty("extensions", out var extensions)
            && extensions.ValueKind == JsonValueKind.Object
            && extensions.TryGetProperty("code", out var code)
            && code.ValueKind == JsonValueKind.String
            && code.GetString() == NotFoundErrorCode
            && e.TryGetProperty("path", out var errorPath)
            && errorPath.ValueKind == JsonValueKind.Array
            && errorPath.GetArrayLength() > 0
            && errorPath[0].ValueKind == JsonValueKind.String
            && errorPath[0].GetString() == field);
    }

    private static IEnumerable<JsonElement> GetErrors(JsonElement root)
        => root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array
            ? errors.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object)
            : [];
}
