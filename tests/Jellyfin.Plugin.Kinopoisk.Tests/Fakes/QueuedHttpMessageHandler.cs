using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Kinopoisk.Tests.Fakes;

/// <summary>
/// Replays scripted answers in order and records what was sent. Used only to test the plugin's own
/// retry and parsing logic; Kinopoisk itself is always tested live.
/// </summary>
public sealed class QueuedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>> _steps = new();

    public int Calls { get; private set; }

    public Collection<string> RequestBodies { get; } = [];

    public QueuedHttpMessageHandler Respond(HttpStatusCode status, string body = "{}", Action<HttpResponseMessage>? configure = null)
    {
        _steps.Enqueue((_, _) =>
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
            configure?.Invoke(response);
            return response;
        });
        return this;
    }

    public QueuedHttpMessageHandler Throw(Exception exception)
    {
        _steps.Enqueue((_, _) => throw exception);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        if (!_steps.TryDequeue(out var step))
        {
            throw new InvalidOperationException($"No scripted answer for call {Calls}.");
        }

        var response = step(request, cancellationToken);
        response.RequestMessage = request;
        return response;
    }
}
