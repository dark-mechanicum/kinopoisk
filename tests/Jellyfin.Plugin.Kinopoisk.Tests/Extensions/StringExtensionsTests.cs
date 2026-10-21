using System.Linq;
using Jellyfin.Plugin.Kinopoisk.Extensions;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Extensions;

/// <summary>
/// File name parsing, ported from the original plugin's test cases.
/// </summary>
public sealed class StringExtensionsTests
{
    [Theory]
    [InlineData("Жить_2010.avi", "Жить", 2010)]
    [InlineData("Зелёный слоник.avi", "Зелёный слоник", null)]
    [InlineData("Хозяин_2025_WEB-DLRip-AVC.mkv", "Хозяин", 2025)]
    [InlineData("Девушка в тумане (2017) BDRip-AVC_ivanes20031987.mkv", "Девушка в тумане", 2017)]
    [InlineData("Three.Billboards.Outside.Ebbing,.Missouri.2017.720p.BluRay.x264-[YTS.AM].mp4", "Three Billboards Outside Ebbing Missouri", 2017)]
    [InlineData("28.Weeks.Later.2007.720p.BrRip.264.YIFY.mp4", "28 Weeks Later", 2007)]
    [InlineData("Kill.Bill.Vol.1.2003.1080p.BrRIp.x264.YIFY.mp4", "Kill Bill Vol 1", 2003)]
    [InlineData("Mickey.17.2025.720p.WEBRip.x264.AAC-[YTS.MX].mp4", "Mickey 17", 2025)]
    [InlineData("Nobody.2.2025.DUB.WEB-DLRip-AVC.seleZen.mkv", "Nobody 2", 2025)]
    [InlineData("Terminator.2.1991.1080p.BluRay.x264-[YTS.AG].mp4", "Terminator 2", 1991)]
    [InlineData("Сайлент Хилл 2 (2012) BDRip-AVC [Open Matte].mkv", "Сайлент Хилл 2", 2012)]
    [InlineData("Brat_2_2000_WEB-DLRip_by_Dalemake.avi", "Brat 2", 2000)]
    [InlineData("2001.A.Space.Odyssey.1968.1080p.BluRay.x264-[YTS.AM].mp4", "2001 A Space Odyssey", 1968)]
    public void ParseFileName_BestCandidateComesFirst(string path, string title, int? year)
        => AssertPosition(path, title, year, 0);

    [Theory]
    [InlineData("Бумер Фильм второй_745.avi", "Бумер Фильм второй", null)]
    [InlineData("Rock.n.Rolla.brrip.mkv", "Rock n Rolla", null)]
    public void ParseFileName_CleanedCandidateComesSecond(string path, string title, int? year)
        => AssertPosition(path, title, year, 1);

    [Theory]
    [InlineData("Blade.Runner.2054.mp4", "Blade Runner 2054", null)]
    [InlineData("04.Сумерки. Сага. Рассвет - Часть 1 (2011) BDRip 1080p [HEVC] 10 bit.mkv", "Сумерки Сага Рассвет Часть 1", 2011)]
    [InlineData("Walk.the.Line.EXTENDED.2005.1080p.BrRip.x264.YIFY.mp4", "Walk the Line", 2005)]
    public void ParseFileName_FallbackCandidateComesThird(string path, string title, int? year)
        => AssertPosition(path, title, year, 2);

    [Theory]
    [InlineData("2004.Blade.Runner.mp4", "Blade Runner", 2004)]
    [InlineData("1968.A.Space.Odyssey.1080p.BluRay.x264-[YTS.AM].mp4", "A Space Odyssey", 1968)]
    public void ParseFileName_LeadingYearIsNotTreatedAsReleaseYear(string path, string title, int? year)
        => Assert.DoesNotContain((title, year), path.ParseFileName());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseFileName_EmptyPathGivesNoCandidates(string? path)
        => Assert.Empty(path.ParseFileName());

    [Fact]
    public void ParseFileName_UsesOnlyFileNameNotDirectories()
        => Assert.All("/media/Фильмы 2020/Матрица.1999.mkv".ParseFileName(), c => Assert.DoesNotContain("Фильмы", c.Title, System.StringComparison.Ordinal));

    [Fact]
    public void ParseFileName_BracketedAlternativeTitleIsAnExtraCandidate()
    {
        var candidates = "Кровь машин [Blood Machines] (2019) [1080p] [x264] [2ru, en].mkv".ParseFileName();

        Assert.Equal(("Кровь машин", 2019), candidates[0]);
        Assert.Contains(("Blood Machines", 2019), candidates);
        Assert.DoesNotContain(candidates, c => c.Title.Contains("1080p", System.StringComparison.Ordinal));
    }

    [Fact]
    public void ParseFileName_TrackerTagInBracketsIsDropped()
        => Assert.Contains(("Erin Brokovich", null), "Erin_Brokovich_[torrents.ru].avi".ParseFileName());

    [Theory]
    [InlineData("Film.2019.WEB-DL.1080p.EniaHD.mkv", "Film", 2019)]
    [InlineData("Дюна.2021.WEB-DL.2160p.HDR.DDP5.1.Atmos-EniaHD.mkv", "Дюна", 2021)]
    [InlineData("Солярис.1972.BDRip.rus.eng.mkv", "Солярис", 1972)]
    public void ParseFileName_ReleaseTagsAndTrailingGroupAreRemoved(string path, string title, int? year)
        => AssertPosition(path, title, year, 0);

    [Fact]
    public void ParseFileName_ReleaseGroupWithoutYearIsRemoved()
        => Assert.Contains(("Film", null), "Film.WEB-DL.1080p.EniaHD.mkv".ParseFileName());

    [Theory]
    [InlineData("фантастика", "Фантастика")]
    [InlineData("для взрослых", "Для взрослых")]
    [InlineData("Драма", "Драма")]
    [InlineData("ёлки", "Ёлки")]
    [InlineData("", "")]
    public void ToGenreName_CapitalizesFirstLetterOnly(string name, string expected)
        => Assert.Equal(expected, name.ToGenreName());

    private static void AssertPosition(string path, string title, int? year, int expectedPosition)
    {
        var candidates = path.ParseFileName().ToList();

        Assert.Equal(expectedPosition, candidates.IndexOf((title, year)));
    }
}
