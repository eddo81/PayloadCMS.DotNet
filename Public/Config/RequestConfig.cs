using PayloadCMS.DotNet.Query;
using PayloadCMS.DotNet.Upload;

namespace PayloadCMS.DotNet.Config;

/// <summary>
/// Options object for <see cref="PayloadCMS.DotNet.PayloadSDK.Request(RequestConfig, CancellationToken)"/>.
/// </summary>
/// <param name="Method">The HTTP method to use.</param>
/// <param name="Path">URL path appended to the base URL (e.g. <c>/api/custom-endpoint</c>).</param>
/// <param name="Body">Optional JSON body to send.</param>
/// <param name="Query">Optional <see cref="QueryBuilder"/> for query parameters.</param>
/// <param name="File">Optional <see cref="FileUpload"/>. When set, the request is sent as multipart, with <c>Body</c> as its data.</param>
public sealed record RequestConfig(HttpMethod Method, string Path, Dictionary<string, object?>? Body = null, QueryBuilder? Query = null, FileUpload? File = null);
