using System;
using System.IO;
using System.Net;
using System.Text.Json;
using Jellyfin.Plugin.Kinopoisk.Api;
using Jellyfin.Plugin.Kinopoisk.Api.Models;
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
        Assert.Equal(KinopoiskErrorKind.Contract, ex.Kind);
    }

    [Fact]
    public void Walk_NotFoundErrorOnRequestedField_HasNotFoundKind()
    {
        var root = JsonDocument.Parse(
            """{"errors":[{"message":"There is no film with id 1","path":["film"],"extensions":{"code":"NotFoundError"}}],"data":{"film":null}}""").RootElement;

        var ex = Assert.Throws<KinopoiskApiException>(() => KinopoiskJson.Walk(root, "data.film"));

        Assert.Equal(KinopoiskErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public void Walk_NotFoundErrorOnOtherField_HasContractKind()
    {
        // Every FilmBaseInfo answer carries a NotFoundError for tvSeries; it says nothing about the film.
        var root = JsonDocument.Parse(
            """{"errors":[{"message":"There is no TV series with id 1","path":["tvSeries"],"extensions":{"code":"NotFoundError"}}],"data":{"film":{"id":1}}}""").RootElement;

        var ex = Assert.Throws<KinopoiskApiException>(() => KinopoiskJson.Walk(root, "data.film.title"));

        Assert.Equal(KinopoiskErrorKind.Contract, ex.Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, KinopoiskErrorKind.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests, KinopoiskErrorKind.Transient)]
    [InlineData(HttpStatusCode.InternalServerError, KinopoiskErrorKind.Transient)]
    [InlineData(HttpStatusCode.ServiceUnavailable, KinopoiskErrorKind.Transient)]
    [InlineData(HttpStatusCode.BadRequest, KinopoiskErrorKind.Contract)]
    [InlineData(HttpStatusCode.Forbidden, KinopoiskErrorKind.Contract)]
    public void ToErrorKind_MapsHttpStatus(HttpStatusCode status, KinopoiskErrorKind expected)
        => Assert.Equal(expected, KinopoiskTransport.ToErrorKind(status));

    [Theory]
    [InlineData("POSTER", FilmImageType.Poster)]
    [InlineData("FAN_ART", FilmImageType.FanArt)]
    [InlineData("PROMO", null)]
    [InlineData("poster", null)]
    [InlineData(null, null)]
    public void ParseImageType_MapsKnownLiteralsAndRejectsOthers(string? literal, FilmImageType? expected)
        => Assert.Equal(expected, KinopoiskJson.ParseImageType(literal));

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
    [InlineData(FilmImageType.FanArt, "FAN_ART")]
    [InlineData(FilmImageType.Poster, "POSTER")]
    public void ToLiteral_UsesGraphQlEnumNames(FilmImageType type, string expected)
        => Assert.Equal(expected, KinopoiskJson.ToLiteral(type));
}
