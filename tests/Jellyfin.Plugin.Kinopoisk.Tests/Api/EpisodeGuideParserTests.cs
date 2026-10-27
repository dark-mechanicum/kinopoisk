using System;
using System.Linq;
using Jellyfin.Plugin.Kinopoisk.Api;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Api;

/// <summary>
/// Episodes page parsing for cases live pages do not reliably show: placeholder titles, partial and missing dates,
/// the login redirect. The rows are copied verbatim from <c>kinopoisk.ru/film/5258557/episodes/</c>; the live tests
/// in <see cref="SeriesAndPersonApiLiveTests"/> check the real pages.
/// </summary>
public sealed class EpisodeGuideParserTests
{
    private const string Page = $"""
        <a name="s1"></a>
        {SeasonHeader}
        {Row1}
        <a name="s3"></a>
        {SeasonHeader}
        {Row31}
        {Row33}
        {Row324}
        """;

    private const string SeasonHeader = """
        <table cellpadding=0 cellspacing=0 width=100%>
            <tr>
                <td colspan=2 class="news" style="color:#777;padding:10px 0px;border-bottom:3px solid #ccc"><h1 class="moviename-big" style="font-size:21px;padding:0px;margin:0px;color:#f60">Сезон 1</h1>
                    2023, эпизодов: 24
                </td>
            </tr>
            <tr><td colspan=2><br></td></tr>
        """;

    private const string Row1 = """
                                <tr>
                    <td width=80% class="news" style="border-bottom:1px dotted #ccc;padding:10px 0px">
                        <a name="y2023"></a>                            <span style="color:#777">Эпизод 1</span><br>
                        <h1 class="moviename-big" style="font-size:16px;padding:0px;color:#444"><b>Маомао</b></h1>
                        <span class='episodesOriginalName'>Maomao</span>                        </td>
                    <td width=20% class="news" align=right valign=bottom style="border-bottom:1px dotted #ccc;padding:15px 0px;font-size:12px" align=left>21 октября 2023</td>
                </tr>
        """;

    private const string Row31 = """
                                <tr>
                    <td width=80% class="news" style="border-bottom:1px dotted #ccc;padding:10px 0px">
                        <a name="y2026"></a>                            <span style="color:#777">Эпизод 1</span><br>
                        <h1 class="moviename-big" style="font-size:16px;padding:0px;color:#444"><b>Саранча</b></h1>
                        <span class='episodesOriginalName'>Episode #3.1</span>                        </td>
                    <td width=20% class="news" align=right valign=bottom style="border-bottom:1px dotted #ccc;padding:15px 0px;font-size:12px" align=left>октября 2026</td>
                </tr>
        """;

    private const string Row33 = """
                                <tr>
                    <td width=80% class="news" style="border-bottom:1px dotted #ccc;padding:10px 0px">
                                                    <span style="color:#777">Эпизод 3</span><br>
                        <h1 class="moviename-big" style="font-size:16px;padding:0px;color:#444"><b>Если нет хлеба…</b></h1>
                                                </td>
                    <td width=20% class="news" align=right valign=bottom style="border-bottom:1px dotted #ccc;padding:15px 0px;font-size:12px" align=left>&nbsp;</td>
                </tr>
        """;

    private const string Row324 = """
                                <tr>
                    <td width=80% class="news" style="border-bottom:1px dotted #ccc;padding:10px 0px">
                                                    <span style="color:#777">Эпизод 24</span><br>
                        <h1 class="moviename-big" style="font-size:16px;padding:0px;color:#444"><b>Эпизод #3.24</b></h1>
                                                </td>
                    <td width=20% class="news" align=right valign=bottom style="border-bottom:1px dotted #ccc;padding:15px 0px;font-size:12px" align=left>&nbsp;</td>
                </tr>
        """;

    // What kinopoisk.ru sends without the login-redirect opt-out cookie.
    private const string LoginRedirectPage = """
        <html><body><script>var it = {"host":"https:\u002F\u002Fsso.kinopoisk.ru\u002Finstall?uuid=1","root":"sso.passport.yandex.ru"};</script></body></html>
        """;

    [Fact]
    public void Parse_ReadsEveryEpisodeWithItsSeasonInPageOrder()
    {
        var episodes = EpisodeGuideParser.Parse(Page);

        Assert.Equal([(1, 1), (3, 1), (3, 3), (3, 24)], episodes.Select(e => (e.Season!.Number, e.Number)));
        Assert.All(episodes, e => Assert.Equal(0, e.Id));
    }

    [Fact]
    public void Parse_ReadsTitlesAndDayAccurateDate()
    {
        var pilot = EpisodeGuideParser.Parse(Page)[0];

        Assert.Equal("Маомао", pilot.Title?.Russian);
        Assert.Equal("Maomao", pilot.Title?.Original);
        Assert.Equal("2023-10-21", pilot.ReleaseDate?.Date);
        Assert.Equal("DAY", pilot.ReleaseDate?.Accuracy);
    }

    [Fact]
    public void Parse_EpisodeWithoutOriginalTitleOrDate()
    {
        var announced = Assert.Single(EpisodeGuideParser.Parse(Page), e => e is { Number: 3, Season.Number: 3 });

        Assert.Equal("Если нет хлеба…", announced.Title?.Russian);
        Assert.Null(announced.Title?.Original);
        Assert.Null(announced.ReleaseDate);
    }

    [Fact]
    public void Parse_PlaceholderTitlesAreDropped()
    {
        var episodes = EpisodeGuideParser.Parse(Page);

        Assert.Null(Assert.Single(episodes, e => e is { Number: 24, Season.Number: 3 }).Title?.Display);
        var first = Assert.Single(episodes, e => e is { Number: 1, Season.Number: 3 });
        Assert.Equal("Саранча", first.Title?.Russian);
        Assert.Null(first.Title?.Original);
    }

    [Theory]
    [InlineData("23 марта 2024", "2024-03-23", "DAY")]
    [InlineData("3 мая 2015", "2015-05-03", "DAY")]
    [InlineData("1 января 1999", "1999-01-01", "DAY")]
    [InlineData("  31 декабря 2010 ", "2010-12-31", "DAY")]
    [InlineData("октября 2026", "2026-10-01", "MONTH")]
    public void ParseReleaseDate_ReadsRussianDates(string text, string date, string accuracy)
    {
        var parsed = EpisodeGuideParser.ParseReleaseDate(text);

        Assert.Equal(date, parsed?.Date);
        Assert.Equal(accuracy, parsed?.Accuracy);
    }

    [Theory]
    [InlineData("&nbsp;")]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("31 февраля 2024")]
    [InlineData("12 March 2024")]
    public void ParseReleaseDate_NoDateForEmptyOrUnreadableCells(string text)
        => Assert.Null(EpisodeGuideParser.ParseReleaseDate(text));

    [Fact]
    public void Parse_LoginRedirectHasNoEpisodesAndIsRecognized()
    {
        Assert.Empty(EpisodeGuideParser.Parse(LoginRedirectPage));
        Assert.True(EpisodeGuideParser.IsLoginPage(LoginRedirectPage));
        Assert.False(EpisodeGuideParser.IsLoginPage(Page));
    }

    [Fact]
    public void Parse_EpisodesBeforeTheFirstSeasonAreIgnored()
    {
        var withoutFirstSeasonAnchor = Page.Replace("<a name=\"s1\"></a>", string.Empty, StringComparison.Ordinal);

        var episodes = EpisodeGuideParser.Parse(withoutFirstSeasonAnchor);

        Assert.Equal([(3, 1), (3, 3), (3, 24)], episodes.Select(e => (e.Season!.Number, e.Number)));
    }
}
