using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Kinopoisk.Common;

/// <summary>
/// Opens TCP connections trying the resolved addresses in a chosen order, so a host with broken IPv6
/// (e.g. Yandex CDN behind some home routers) can still be reached over IPv4.
/// </summary>
internal static class AddressOrderConnector
{
    /// <summary>
    /// Orders addresses for connecting.
    /// </summary>
    /// <param name="addresses">The addresses in DNS order.</param>
    /// <param name="preferIpv4">Whether IPv4 addresses go first.</param>
    /// <returns>The addresses; the order within each family is kept.</returns>
    public static IReadOnlyList<IPAddress> Order(IEnumerable<IPAddress> addresses, bool preferIpv4)
        => preferIpv4
            ? [.. addresses.OrderBy(a => a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)]
            : [.. addresses];

    /// <summary>
    /// Connects to the first address that answers.
    /// shortcut: sequential IPv4-first, not parallel happy-eyeballs; race the attempts if IPv4 timeouts become common.
    /// </summary>
    /// <param name="endPoint">The host and port.</param>
    /// <param name="preferIpv4">Whether IPv4 addresses go first.</param>
    /// <param name="cancellationToken">The cancellation token (carries the handler's connect timeout).</param>
    /// <returns>The connected stream.</returns>
    public static async ValueTask<Stream> ConnectAsync(DnsEndPoint endPoint, bool preferIpv4, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        var addresses = Order(await Dns.GetHostAddressesAsync(endPoint.Host, cancellationToken).ConfigureAwait(false), preferIpv4);
        SocketException? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(address, endPoint.Port, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                lastError = ex;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw lastError ?? new SocketException((int)SocketError.HostNotFound);
    }
}
