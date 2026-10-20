using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Kinopoisk.Api.Models;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Parses Kinopoisk JSON and extracts the requested data with clear errors.
/// </summary>
internal static class KinopoiskJson
{
    private const int SnippetLength = 300;

    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter<FilmImageType>(JsonNamingPolicy.SnakeCaseUpper) },
    };

    /// <summary>
    /// Gets the GraphQL enum literal of an image category, e.g. <c>FAN_ART</c>.
    /// </summary>
    /// <param name="type">The category.</param>
    /// <returns>The literal.</returns>
    public static string ToLiteral(FilmImageType type) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(type.ToString());

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
    /// Walks a dot separated path; the error explains what Kinopoisk said instead.
    /// </summary>
    /// <param name="root">The response root.</param>
    /// <param name="path">The path.</param>
    /// <returns>The element.</returns>
    public static JsonElement Walk(JsonElement root, string path)
    {
        var current = root;
        foreach (var segment in path.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current)
                || current.ValueKind == JsonValueKind.Null)
            {
                throw new KinopoiskApiException(
                    $"Kinopoisk response has no '{path}' (missing '{segment}'). {DescribeErrors(root)}".TrimEnd());
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
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var messages = errors.EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out var m) ? m.GetString() : null)
            .OfType<string>();
        return "Errors: " + string.Join("; ", messages);
    }
}
