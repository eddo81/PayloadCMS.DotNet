using System.Net;

namespace PayloadCMS.DotNet.Internal.Utils;

/// <summary>
/// The transport <see cref="PayloadSDK"/> sends requests through when no <c>HttpClient</c> is passed.
/// <para>Shared by every <see cref="PayloadSDK"/> instance in the process: connections are reused, each one is
/// replaced after two minutes so DNS changes are picked up, and no cookies are stored.</para>
/// </summary>
internal static class DefaultHttpClient
{
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.All,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });

    /// <summary>
    /// Returns the shared client.
    /// </summary>
    /// <returns>The <see cref="HttpClient"/> shared by every <see cref="PayloadSDK"/> instance.</returns>
    internal static HttpClient Get()
    {
        return _httpClient;
    }
}