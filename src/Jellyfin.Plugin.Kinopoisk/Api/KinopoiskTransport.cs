using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Kinopoisk.Common;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Kinopoisk.Api;

/// <summary>
/// Low level transport: posts embedded GraphQL documents and reads REST JSON, one request at a time.
/// </summary>
public sealed class KinopoiskTransport : IDisposable
{
    /// <summary>Named HttpClient for GraphQL.</summary>
    public const string GraphQlClientName = "Kinopoisk.GraphQL";

    /// <summary>Named HttpClient for the site REST API.</summary>
    public const string RestClientName = "Kinopoisk.Rest";

    private const string GraphQlUrl = "https://graphql.kinopoisk.ru/graphql";
    private const string RestUrl = Constants.SiteUrl + "/api/";
    private const string PageUrl = Constants.SiteUrl + "/";
    private const string QueryResourcePrefix = "Jellyfin.Plugin.Kinopoisk.Api.Queries.";
    private const int MaxAttempts = 3;

    // Without it every kinopoisk.ru page redirects to the Yandex login (sso.passport.yandex.ru) instead of rendering.
    private const string SsoRedirectOptOutCookie = "disable_server_sso_redirect=1";

    private static readonly ConcurrentDictionary<string, string> _queries = new(StringComparer.Ordinal);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KinopoiskTransport> _logger;
    private readonly SerialGate _gate;
    private readonly HttpRetry _retry;

    /// <summary>
    /// Initializes a new instance of the <see cref="KinopoiskTransport"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="options">The plugin options.</param>
    /// <param name="logger">The logger.</param>
    public KinopoiskTransport(IHttpClientFactory httpClientFactory, IKinopoiskOptions options, ILogger<KinopoiskTransport> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _gate = new SerialGate(() => TimeSpan.FromMilliseconds(options.MinRequestIntervalMs));
        _retry = new HttpRetry(MaxAttempts, logger);
    }

    /// <summary>
    /// Runs a GraphQL operation and deserializes the element at <paramref name="path"/>.
    /// Partial errors are tolerated as long as the requested data is present.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operationName">The operation name; also the embedded <c>.gql</c> file name.</param>
    /// <param name="variables">The variables.</param>
    /// <param name="path">Dot separated path to the data, e.g. <c>data.film</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The data.</returns>
    public async Task<T> QueryAsync<T>(string operationName, object variables, string path, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new { operationName, variables, query = GetQuery(operationName) });
        _logger.LogDebug("GraphQL {Operation} {Variables}", operationName, JsonSerializer.Serialize(variables));

        var text = await SendAsync(
            GraphQlClientName,
            () => new HttpRequestMessage(HttpMethod.Post, GraphQlUrl) { Content = new StringContent(body, Encoding.UTF8, "application/json") },
            cancellationToken).ConfigureAwait(false);

        var root = KinopoiskJson.Parse(text);
        return KinopoiskJson.Deserialize<T>(KinopoiskJson.Walk(root, path), $"{operationName}:{path}");
    }

    /// <summary>
    /// Gets a site REST resource.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="resource">The resource, e.g. <c>person/7836/</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The data.</returns>
    public async Task<T> GetAsync<T>(string resource, CancellationToken cancellationToken)
    {
        _logger.LogDebug("REST {Resource}", resource);
        var text = await SendAsync(
            RestClientName,
            () => new HttpRequestMessage(HttpMethod.Get, RestUrl + resource),
            cancellationToken).ConfigureAwait(false);

        return KinopoiskJson.Deserialize<T>(KinopoiskJson.Parse(text), resource);
    }

    /// <summary>
    /// Gets a kinopoisk.ru HTML page.
    /// </summary>
    /// <param name="path">The page path relative to the site, e.g. <c>film/301/episodes/</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The HTML.</returns>
    public Task<string> GetHtmlAsync(string path, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Page {Path}", path);
        return SendAsync(
            RestClientName,
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, PageUrl + path);
                request.Headers.Add("Cookie", SsoRedirectOptOutCookie);
                return request;
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Reads an embedded GraphQL document.
    /// Kinopoisk only accepts its own documents verbatim, so they must never be edited.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <returns>The document.</returns>
    internal static string GetQuery(string operationName)
        => _queries.GetOrAdd(operationName, static name =>
        {
            var resource = QueryResourcePrefix + name + ".gql";
            using var stream = typeof(KinopoiskTransport).Assembly.GetManifestResourceStream(resource)
                ?? throw new FileNotFoundException($"Embedded GraphQL query '{resource}' not found.", resource);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });

    /// <summary>
    /// Classifies a non-success HTTP status.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns>NotFound for 404, Transient for 429 and 5xx, Contract otherwise.</returns>
    internal static KinopoiskErrorKind ToErrorKind(HttpStatusCode status)
        => status == HttpStatusCode.NotFound ? KinopoiskErrorKind.NotFound
            : HttpRetry.IsTransient(status) ? KinopoiskErrorKind.Transient
            : KinopoiskErrorKind.Contract;

    private Task<string> SendAsync(string clientName, Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
        => _gate.RunAsync(
            async () =>
            {
                using var response = await SendWithRetryAsync(clientName, createRequest, cancellationToken).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new KinopoiskApiException(
                        $"Kinopoisk {response.RequestMessage?.RequestUri} answered {(int)response.StatusCode} {response.ReasonPhrase}: {KinopoiskJson.Snippet(content)}",
                        ToErrorKind(response.StatusCode));
                }

                return content;
            },
            cancellationToken);

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string clientName, Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        try
        {
            return await _retry.SendAsync(_httpClientFactory.CreateClient(clientName), createRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new KinopoiskApiException($"Kinopoisk is unreachable: {ex.Message}", KinopoiskErrorKind.Transient, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new KinopoiskApiException("Kinopoisk did not answer in time.", KinopoiskErrorKind.Transient, ex);
        }
    }
}
