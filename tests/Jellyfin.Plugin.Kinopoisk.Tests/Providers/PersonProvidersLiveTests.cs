using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Jellyfin.Plugin.Kinopoisk.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Providers;

/// <summary>
/// Person providers end to end, against the live API.
/// </summary>
public sealed class PersonProvidersLiveTests : IDisposable
{
    private readonly LiveServices _services = new();
    private readonly CancellationToken _token = TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetMetadata_ById_FillsPerson()
    {
        IRemoteMetadataProvider<Person, PersonLookupInfo> provider = _services.Create<PersonMetadataProvider>();
        var info = new PersonLookupInfo();
        info.SetKinopoiskId(KnownTitles.KeanuReevesId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        Assert.Equal("Киану Ривз", result.Item.Name);
        Assert.Equal("Keanu Reeves", result.Item.OriginalTitle);
        Assert.Equal(new DateTime(1964, 9, 2, 0, 0, 0, DateTimeKind.Utc), result.Item.PremiereDate);
        Assert.NotEmpty(result.Item.ProductionLocations);
    }

    [Fact]
    public async Task GetMetadata_ByName_FindsPerson()
    {
        IRemoteMetadataProvider<Person, PersonLookupInfo> provider = _services.Create<PersonMetadataProvider>();

        var result = await provider.GetMetadata(new PersonLookupInfo { Name = "Киану Ривз" }, _token);

        Assert.True(result.Item.TryGetKinopoiskId(out var id));
        Assert.Equal(KnownTitles.KeanuReevesId, id);
    }

    [Fact]
    public async Task GetImages_ReturnsDownloadablePhotos()
    {
        var provider = _services.Create<PersonImageProvider>();
        var person = new Person();
        person.SetKinopoiskId(KnownTitles.KeanuReevesId);

        var images = (await provider.GetImages(person, _token)).ToList();

        Assert.NotEmpty(images);
        Assert.All(images, i => Assert.Equal(ImageType.Primary, i.Type));
        using var response = await provider.GetImageResponse(images[0].Url, _token);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetImages_PhotolessPerson_ReturnsNoPlaceholder()
    {
        var provider = _services.Create<PersonImageProvider>();
        var person = new Person();
        person.SetKinopoiskId(KnownTitles.PhotolessPersonId);

        var images = await provider.GetImages(person, _token);

        Assert.DoesNotContain(images, i => i.Url.EndsWith(".svg", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMetadata_UnknownId_ReturnsEmptyResult()
    {
        IRemoteMetadataProvider<Person, PersonLookupInfo> provider = _services.Create<PersonMetadataProvider>();
        var info = new PersonLookupInfo();
        info.SetKinopoiskId(KnownTitles.UnknownId);

        var result = await provider.GetMetadata(info, _token);

        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task GetMetadata_DeceasedPerson_HasDeathDate()
    {
        var result = await GetPersonMetadataAsync(_services, KnownTitles.HeathLedgerId);

        Assert.Equal("Хит Леджер", result.Item.Name);
        Assert.Equal(new DateTime(1979, 4, 4, 0, 0, 0, DateTimeKind.Utc), result.Item.PremiereDate);
        Assert.Equal(new DateTime(2008, 1, 22, 0, 0, 0, DateTimeKind.Utc), result.Item.EndDate);
    }

    [Fact]
    public async Task GetMetadata_LivingPerson_HasNoDeathDate()
    {
        var result = await GetPersonMetadataAsync(_services, KnownTitles.KeanuReevesId);

        Assert.Null(result.Item.EndDate);
    }

    [Fact]
    public async Task GetMetadata_Biography_IsOffByDefault()
    {
        var result = await GetPersonMetadataAsync(_services, KnownTitles.KeanuReevesId);

        Assert.Null(result.Item.Overview);
    }

    [Fact]
    public async Task GetMetadata_BiographyEnabled_IsPlainTextFromFacts()
    {
        using var services = new LiveServices(new PluginConfiguration { IncludePersonBiography = true });

        var result = await GetPersonMetadataAsync(services, KnownTitles.KeanuReevesId);

        var paragraphs = result.Item.Overview?.Split("\n\n") ?? [];
        Assert.Equal(Constants.PersonBiographyFacts, paragraphs.Length);
        Assert.StartsWith("Полное имя — Киану Чарльз Ривз", paragraphs[0], StringComparison.Ordinal);
        Assert.All(paragraphs, p => Assert.False(string.IsNullOrWhiteSpace(p)));
        Assert.DoesNotContain("<", result.Item.Overview, StringComparison.Ordinal);
        Assert.DoesNotContain("href", result.Item.Overview, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"&(?:[a-z]+|#\d+);", result.Item.Overview);
    }

    [Fact]
    public async Task GetMetadata_BiographyEnabled_KeepsTextOfLinkedNames()
    {
        // Kinopoisk wraps names in links («сестры <a href="/name/282183/">Ким</a>»); the name must survive.
        using var services = new LiveServices(new PluginConfiguration { IncludePersonBiography = true });

        var result = await GetPersonMetadataAsync(services, KnownTitles.KeanuReevesId);

        Assert.Contains("сестры Ким", result.Item.Overview, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetMetadata_BiographyEnabled_PersonWithoutFactsHasNoOverview()
    {
        using var services = new LiveServices(new PluginConfiguration { IncludePersonBiography = true });

        var result = await GetPersonMetadataAsync(services, KnownTitles.PhotolessPersonId);

        Assert.Null(result.Item.Overview);
    }

    public void Dispose() => _services.Dispose();

    private async Task<MetadataResult<Person>> GetPersonMetadataAsync(LiveServices services, int personId)
    {
        IRemoteMetadataProvider<Person, PersonLookupInfo> provider = services.Create<PersonMetadataProvider>();
        var info = new PersonLookupInfo();
        info.SetKinopoiskId(personId);

        var result = await provider.GetMetadata(info, _token);

        Assert.True(result.HasMetadata);
        return result;
    }
}
