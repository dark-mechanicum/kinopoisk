using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Fakes;

/// <summary>
/// Records log entries so tests can check levels and exceptions.
/// </summary>
public sealed class CapturingLogger : ILogger
{
    public Collection<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, exception, formatter(state, exception)));
}
