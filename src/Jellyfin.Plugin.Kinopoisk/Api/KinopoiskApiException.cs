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

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskApiException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="kind">The kind of failure.</param>
    /// <param name="innerException">The cause, if any.</param>
    public KinopoiskApiException(string message, KinopoiskErrorKind kind, Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the kind of failure; <see cref="KinopoiskErrorKind.Contract"/> unless stated otherwise.
    /// </summary>
    public KinopoiskErrorKind Kind { get; } = KinopoiskErrorKind.Contract;
}
