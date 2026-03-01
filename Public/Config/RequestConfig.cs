namespace PayloadCMS.DotNet.Config;

/// <summary>
/// Options object for <see cref="PayloadCMS.DotNet.PayloadSDK.Request"/>.
/// <para>Mirrors the <c>RequestConfig</c> options contract shared across all ports.</para>
/// </summary>
/// <param name="Method">The HTTP method to use.</param>
/// <param name="Path">URL path appended to the base URL (e.g. <c>/api/custom-endpoint</c>).</param>
/// <param name="Body">Optional JSON body to send.</param>
/// <param name="Query">Optional <see cref="PayloadCMS.DotNet.QueryBuilder"/> for query parameters.</param>
public sealed record RequestConfig(HttpMethod Method, string Path, Dictionary<string, object?>? Body = null, QueryBuilder? Query = null);
