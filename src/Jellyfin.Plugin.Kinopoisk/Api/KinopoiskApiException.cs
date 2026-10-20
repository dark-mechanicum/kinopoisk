using System;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Kinopoisk answered, but not with the data that was asked for.
/// </summary>
public sealed class KinopoiskApiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskApiException"/> class.
    /// </summary>
    public KinopoiskApiException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskApiException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public KinopoiskApiException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskApiException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public KinopoiskApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
