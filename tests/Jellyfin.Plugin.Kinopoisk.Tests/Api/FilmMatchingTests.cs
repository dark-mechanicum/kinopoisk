using Jellyfin.Plugin.Kinopoisk.Api.Models;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// The rules that rank search matches: release year and exact title.
/// </summary>
public sealed class FilmMatchingTests
{
    private static readonly FilmInfo _matrix = new() { Title = new FilmTitle("Матрица", "The Matrix"), ProductionYear = 1999 };

    [Theory]
    [InlineData(1999, true)]
    [InlineData(1998, false)]
    [InlineData(null, false)]
    public void IsReleasedIn_FilmMatchesOnlyItsProductionYear(int? year, bool expected)
        => Assert.Equal(expected, _matrix.IsReleasedIn(year));

    [Theory]
    [InlineData(1997, true)]
    [InlineData(1998, true)]
    [InlineData(1999, true)]
    [InlineData(1996, false)]
    [InlineData(2000, false)]
    public void IsReleasedIn_SeriesMatchesEveryYearOfItsRun(int year, bool expected)
        => Assert.Equal(expected, Series(new FilmYearsRange(1997, 1999)).IsReleasedIn(year));

    [Theory]
    [InlineData(2020, true)]
    [InlineData(2030, true)]
    [InlineData(2019, false)]
    public void IsReleasedIn_RunningSeriesMatchesEveryYearSinceItsStart(int year, bool expected)
        => Assert.Equal(expected, Series(new FilmYearsRange(2020)).IsReleasedIn(year));

    [Fact]
    public void IsReleasedIn_UndatedTitleMatchesNoYear()
    {
        Assert.False(new FilmInfo().IsReleasedIn(2020));
        Assert.False(Series(new FilmYearsRange()).IsReleasedIn(2020));
    }

    [Theory]
    [InlineData("Матрица")]
    [InlineData("матрица")]
    [InlineData("The Matrix")]
    [InlineData(" the matrix ")]
    public void HasTitle_MatchesRussianOrOriginalTitleIgnoringCaseAndSpaces(string title)
        => Assert.True(_matrix.HasTitle(title));

    [Theory]
    [InlineData("Матрица 2")]
    [InlineData("Матр")]
    [InlineData("")]
    public void HasTitle_RejectsPartialAndOtherTitles(string title)
        => Assert.False(_matrix.HasTitle(title));

    [Fact]
    public void HasTitle_MatchesLocalizedTitle()
        => Assert.True(new FilmInfo { Title = new FilmTitle(Localized: "Серия") }.HasTitle("Серия"));

    [Fact]
    public void HasTitle_UntitledFilmMatchesNothing()
        => Assert.False(new FilmInfo().HasTitle("Матрица"));

    private static FilmInfo Series(FilmYearsRange years) => new() { TypeName = "TvSeries", ReleaseYears = [years] };
}
