using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public void Dispose() => _services.Dispose();
}
