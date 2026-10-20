namespace Jellyfin.Plugin.Kinopoisk.Tests;

/// <summary>
/// Long-lived Kinopoisk entries the live tests rely on. If one of them changes on Kinopoisk itself,
/// update it here; anything else failing means the API changed.
/// </summary>
public static class KnownTitles
{
    /// <summary>«Матрица» / The Matrix (1999).</summary>
    public const int MatrixId = 301;

    /// <summary>Content UUID of «Матрица» on the streaming service.</summary>
    public const string MatrixContentId = "4824a95e60a7db7e86f14137516ba590";

    /// <summary>«Игра престолов» / Game of Thrones (2011-2019).</summary>
    public const int GameOfThronesId = 464963;

    /// <summary>«Метод» (2015), a series available on the streaming service with season content ids.</summary>
    public const int MetodId = 838050;

    /// <summary>Content UUID of «Метод».</summary>
    public const string MetodContentId = "49635d28e64c834f8c7327ebd9e421a2";

    /// <summary>Content UUID of season 1 of «Метод» (16 episodes).</summary>
    public const string MetodSeason1ContentId = "406547c4784c613a96dadc9321e78afa";

    /// <summary>«Незнайка на Луне» (1997-1999): a cartoon Kinopoisk files as a TV series, often kept as a movie.</summary>
    public const int NeznaykaNaLuneId = 41562;

    /// <summary>«Незнайка на Луне 2» (1999), the film sequel of «Незнайка на Луне».</summary>
    public const int NeznaykaNaLune2Id = 41563;

    /// <summary>«Незнайка учится» (1961).</summary>
    public const int NeznaykaUchitsyaId = 257947;

    /// <summary>Keanu Reeves.</summary>
    public const int KeanuReevesId = 7836;

    /// <summary>Lana Wachowski.</summary>
    public const int LanaWachowskiId = 23330;

    /// <summary>An id no film, series or person has.</summary>
    public const int UnknownId = 999_999_999;
}
