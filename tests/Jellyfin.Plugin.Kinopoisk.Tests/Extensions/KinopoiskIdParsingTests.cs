using Jellyfin.Plugin.Kinopoisk.Extensions;
using Xunit;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Extensions;

/// <summary>
/// Explicit <c>kp1234</c> id tags in file and folder names.
/// </summary>
public sealed class KinopoiskIdParsingTests
{
    [Theory]
    [InlineData("Матрица (1999) [kp301]", 301)]
    [InlineData("Матрица (1999) kp301", 301)]
    [InlineData("The.Matrix.1999.kp-301.1080p.mkv", 301)]
    [InlineData("The Matrix kp_301.mkv", 301)]
    [InlineData("Матрица {kp=301}", 301)]
    [InlineData("Матрица (KP-301)", 301)]
    [InlineData("Матрица [kpid-301]", 301)]
    [InlineData("Матрица [kpid=301]", 301)]
    [InlineData("Матрица [kinopoisk-301]", 301)]
    [InlineData("Матрица [KinopoiskId=301]", 301)]
    [InlineData("kp464963", 464963)]
    [InlineData("Игра престолов [kp-464963]", 464963)]
    [InlineData("Movie.2020.kp301.kp302.mkv", 301)]
    public void TryParseKinopoiskId_FindsTag(string name, int expected)
    {
        Assert.True(name.TryParseKinopoiskId(out var id));
        Assert.Equal(expected, id);
    }

    [Theory]
    [InlineData("Matrix Skp301.mkv")]
    [InlineData("Matrix kp301x.mkv")]
    [InlineData("Matrix kp 301.mkv")]
    [InlineData("Matrix kp-.mkv")]
    [InlineData("Matrix [kp-0]")]
    [InlineData("Matrix [kp-99999999999]")]
    [InlineData("Matrix [tmdbid-603]")]
    [InlineData("Пкп301")]
    [InlineData("The Matrix (1999)")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParseKinopoiskId_IgnoresNamesWithoutValidTag(string? name)
    {
        Assert.False(name.TryParseKinopoiskId(out var id));
        Assert.Equal(0, id);
    }
}
