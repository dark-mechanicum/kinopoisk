using System;
using System.IO;
using System.Text.Json;
using Jellyfin.Plugin.Kinopoisk.Api;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Embedded query documents and response parsing.
/// </summary>
public sealed class KinopoiskTransportTests
{
    [Theory]
    [InlineData("FilmBaseInfo")]
    [InlineData("FilmPage")]
    [InlineData("MovieImagesItems")]
    [InlineData("SerialStructureSeason")]
    [InlineData("SuggestSearch")]
    [InlineData("TvSeriesBaseInfo")]
    [InlineData("TvSeriesEpisodes")]
    public void GetQuery_ReturnsEmbeddedDocumentForOperation(string operationName)
        => Assert.StartsWith($"query {operationName}(", KinopoiskTransport.GetQuery(operationName), StringComparison.Ordinal);

    [Fact]
    public void GetQuery_ThrowsForUnknownOperation()
        => Assert.Throws<FileNotFoundException>(() => KinopoiskTransport.GetQuery("Unknown"));

    [Fact]
    public void Walk_ReturnsNestedElement()
    {
        var root = JsonDocument.Parse("""{"a":{"b":{"c":11}}}""").RootElement;

        Assert.Equal(11, KinopoiskJson.Walk(root, "a.b.c").GetInt32());
    }

    [Fact]
    public void Walk_ExplainsMissingDataWithKinopoiskErrors()
    {
        var root = JsonDocument.Parse("""{"errors":[{"message":"There is no film with id 1"}],"data":{"film":null}}""").RootElement;

        var ex = Assert.Throws<KinopoiskApiException>(() => KinopoiskJson.Walk(root, "data.film"));

        Assert.Contains("missing 'film'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("There is no film with id 1", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Walk_ToleratesPartialErrorsWhenDataPresent()
    {
        var root = JsonDocument.Parse("""{"errors":[{"message":"There is no TV series with id 301"}],"data":{"film":{"id":301}}}""").RootElement;

        Assert.Equal(301, KinopoiskJson.Walk(root, "data.film.id").GetInt32());
    }

    [Theory]
    [InlineData("")]
    [InlineData("NOT JSON")]
    public void Parse_ThrowsOnEmptyOrInvalidDocument(string body)
        => Assert.Throws<KinopoiskApiException>(() => KinopoiskJson.Parse(body));

    [Theory]
    [InlineData(Kinopoisk.Api.Models.FilmImageType.FanArt, "FAN_ART")]
    [InlineData(Kinopoisk.Api.Models.FilmImageType.Poster, "POSTER")]
    public void ToLiteral_UsesGraphQlEnumNames(Kinopoisk.Api.Models.FilmImageType type, string expected)
        => Assert.Equal(expected, KinopoiskJson.ToLiteral(type));
}
