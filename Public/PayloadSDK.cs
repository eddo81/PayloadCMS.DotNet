using System.Text.Json;
using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Config;
using PayloadCMS.DotNet.Query;
using PayloadCMS.DotNet.Internal.Contracts;
using PayloadCMS.DotNet.Internal.Upload;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Models.Auth;
using PayloadCMS.DotNet.Models.Collection;
using PayloadCMS.DotNet.Upload;

namespace PayloadCMS.DotNet;

/// <summary>
/// HTTP client for the Payload CMS REST API.
/// <para>Provides typed methods for <c>collections</c>, <c>globals</c>,
/// <c>auth</c>, <c>versions</c>, and file uploads.</para>
/// </summary>
public class PayloadSDK
{
    private string _baseUrl;
    private Dictionary<string, string> _headers = new();
    private IAuthCredential? _auth = null;
    private readonly PayloadSDKConfig _config;
    private readonly QueryStringEncoder _encoder;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="PayloadSDK"/>.
    /// </summary>
    /// <param name="baseUrl">The base URL of the Payload CMS instance (e.g. <c>https://cms.example.com</c>).</param>
    /// <param name="config">Settings applied to every request this instance makes. Defaults are used when omitted.</param>
    /// <param name="httpClient">
    /// Optional <see cref="HttpClient"/> instance to use for all requests.
    /// The caller is responsible for its lifetime and disposal. If omitted, a shared default client is used instead.
    /// </param>
    public PayloadSDK(string baseUrl, PayloadSDKConfig? config = null, HttpClient? httpClient = null)
    {
        _baseUrl = NormalizeUrl(baseUrl);
        _config = config ?? new ();
        _encoder = new QueryStringEncoder( strictEncoding: _config.StrictEncoding );
        _httpClient = httpClient ?? DefaultHttpClient.Get();
    }

    /// <summary>
    /// Validates and normalizes a base URL string.
    /// <para>Strips trailing slashes to prevent double-slash
    /// paths when building endpoint URLs.</para>
    /// </summary>
    /// <param name="url">The raw base URL to normalize.</param>
    /// <returns>The normalized URL without a trailing slash.</returns>
    /// <exception cref="Exception">If the URL is malformed.</exception>
    private string NormalizeUrl(string url)
    {
        try
        {
            var urlString = new Uri(url).ToString();
            var normalized = urlString.TrimEnd('/');

            return normalized;
        }
        catch (Exception error)
        {
            throw new Exception($"Invalid base URL: {url}", error);
        }
    }

    /// <summary>
    /// Sets the custom headers to include with every request, replacing any set before.
    /// <para>An <c>Authorization</c> header set here is overridden while a credential is set with
    /// <see cref="SetJwtAuth"/> or <see cref="SetApiKeyAuth"/>.</para>
    /// </summary>
    /// <param name="headers">The custom headers to set.</param>
    public void SetHeaders(Dictionary<string, string> headers)
    {
        _headers = headers;
    }

    /// <summary>
    /// Sets an API key credential for all subsequent requests, replacing any credential set before.
    /// </summary>
    /// <param name="auth">The <see cref="ApiKeyAuth"/> credential to use.</param>
    public void SetApiKeyAuth(ApiKeyAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Sets a JWT bearer token credential for all subsequent requests, replacing any credential set before.
    /// </summary>
    /// <param name="auth">The <see cref="JwtAuth"/> credential to use.</param>
    public void SetJwtAuth(JwtAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Clears the credential set with <see cref="SetJwtAuth"/> or <see cref="SetApiKeyAuth"/>.
    /// <para>An <c>Authorization</c> header set with <see cref="SetHeaders"/> is still sent.</para>
    /// </summary>
    public void ClearAuth()
    {
        _auth = null;
    }

    /// <summary>
    /// Appends a serialized query string to the given URL.
    /// </summary>
    /// <param name="url">The base URL to append query parameters to.</param>
    /// <param name="query">Optional query parameters.</param>
    /// <returns>The URL with an appended query string, if applicable.</returns>
    private string AppendQueryString(string url, QueryBuilder? query)
    {
        if (query == null)
        {
            return url;
        }

        var @params = query.Build();
        var queryString = _encoder.Stringify(@params);

        return $"{url}{queryString}";
    }

    /// <summary>
    /// Sends a request to any path on the Payload CMS instance and returns the response.
    /// <para>An escape hatch for custom endpoints. Sends the custom headers and the credential like every
    /// other method. A non-2xx response throws <see cref="PayloadError"/>, like every other method; a
    /// successful response is returned unread, so its body is left to the caller.</para>
    /// </summary>
    /// <param name="config">The request options: method, path, and optional body, query and file.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The successful HTTP response.</returns>
    /// <exception cref="PayloadError">On non-2xx responses.</exception>
    public async Task<HttpResponseMessage> Request(RequestConfig config, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}{config.Path}", config.Query);
        var request = new HttpRequestMessage(config.Method, url);
        var headers = new Dictionary<string, string>();
        var data = config.Body ?? new Dictionary<string, object?>();

        foreach (var kvp in _headers)
        {
            headers[kvp.Key] = kvp.Value;
        }

        if (_auth != null)
        {
            _auth.Apply(headers);
        }

        foreach (var kvp in headers)
        {
            if (kvp.Key == "Content-Type")
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        if (config.Body != null || config.File != null)
        {
            request.Content = config.File != null ? FormDataBuilder.Build(config.File, data) : data.ToJsonContent();
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new PayloadError(
                statusCode: (int)response.StatusCode,
                response: response,
                body: text.Length > 0 ? text : null
            );
        }

        return response;
    }

    /// <summary>
    /// Executes an HTTP request and returns parsed JSON.
    /// <para>Merges default headers, applies auth, and parses the response body.
    /// A non-2xx response throws <see cref="PayloadError"/>. A response body that is not a JSON object throws <see cref="JsonException"/>.</para>
    /// </summary>
    /// <param name="url">Fully resolved request URL.</param>
    /// <param name="method">Optional HTTP method; defaults to GET.</param>
    /// <param name="body">Optional request body.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The parsed JSON object.</returns>
    /// <exception cref="PayloadError">On non-2xx responses.</exception>
    /// <exception cref="JsonException">On a response body that is not a JSON object.</exception>
    private async Task<JsonObject> Request(string url, HttpMethod? method = null, HttpContent? body = null, CancellationToken cancellationToken = default)
    {
        var resolvedMethod = method ?? HttpMethod.Get;
        var request = new HttpRequestMessage(resolvedMethod, url);
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
        };

        foreach (var kvp in _headers)
        {
            headers[kvp.Key] = kvp.Value;
        }

        if (_auth != null)
        {
            _auth.Apply(headers);
        }

        foreach (var kvp in headers)
        {
            if (kvp.Key == "Content-Type")
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
        }

        request.Content = body;

        var response = await _httpClient.SendAsync(request, cancellationToken);

        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new PayloadError(
                statusCode: (int)response.StatusCode,
                response: response,
                body: text.Length > 0 ? text : null
            );
        }

        var json = text.ParseJson();

        if (json == null)
        {
            throw new JsonException("Expected a JSON object in the response body.");
        }

        return json;
    }

    /// <summary>
    /// Retrieves a paginated list of documents from a <c>collection</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> for filtering, sorting, pagination.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A paginated response containing matching documents.</returns>
    public async Task<PaginatedDocsDTO> Find(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a single document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> for depth, locale, etc.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The requested document.</returns>
    public async Task<DocumentDTO> FindById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Creates a new document in a <c>collection</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="data">The document data to create.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Draft(true)</c> to save as a draft, or <c>Locale</c>.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The created document.</returns>
    public async Task<DocumentDTO> Create(string slug, Dictionary<string, object?> data, QueryBuilder? query = null, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Post;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        JsonObject doc = new();

        if (json.ContainsKey("doc") && json["doc"] is JsonObject value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Deletes multiple documents matching a query.
    /// <para>The query must contain <c>Where</c> conditions; without them Payload rejects the request.</para>
    /// <para>Deletes permanently, even in collections with a trash.</para>
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="query"><see cref="QueryBuilder"/> with <c>Where</c> conditions selecting the documents.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>
    /// The bulk result containing deleted documents. When some documents could not be deleted, the
    /// result is still returned and <see cref="BulkOperationDTO.Errors"/> reports them.
    /// </returns>
    public async Task<BulkOperationDTO> Delete(string slug, QueryBuilder query, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Delete;

        JsonObject? json;

        try
        {
            json = await Request(url, method, cancellationToken: cancellationToken);
        }
        catch (PayloadError error)
        {
            json = error.AsBulkOperationResult();

            if (json == null)
            {
                throw;
            }
        }

        var dto = BulkOperationDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Deletes a single document by its ID.
    /// <para>Deletes permanently, even in collections with a trash.</para>
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Trash(true)</c> to permanently delete an already soft-deleted document (without it, the server can't find a trashed document by ID at all).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The deleted document.</returns>
    public async Task<DocumentDTO> DeleteById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}", query);
        var method = HttpMethod.Delete;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        JsonObject doc = new();

        if (json.ContainsKey("doc") && json["doc"] is JsonObject value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Updates multiple documents matching a query.
    /// <para>The query must contain <c>Where</c> conditions; without them Payload rejects the request.</para>
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="data">The fields to update on matching documents.</param>
    /// <param name="query"><see cref="QueryBuilder"/> with <c>Where</c> conditions selecting the documents.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>
    /// The bulk result containing updated documents. When some documents could not be updated, the
    /// result is still returned and <see cref="BulkOperationDTO.Errors"/> reports them.
    /// </returns>
    public async Task<BulkOperationDTO> Update(string slug, Dictionary<string, object?> data, QueryBuilder query, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Patch;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : data.ToJsonContent();

        JsonObject? json;

        try
        {
            json = await Request(url, method, body, cancellationToken);
        }
        catch (PayloadError error)
        {
            json = error.AsBulkOperationResult();

            if (json == null)
            {
                throw;
            }
        }

        var dto = BulkOperationDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Updates a single document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="data">The fields to update.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Draft(true)</c> to save the edit as a draft version, or <c>Locale</c>.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The updated document.</returns>
    public async Task<DocumentDTO> UpdateById(string slug, string id, Dictionary<string, object?> data, QueryBuilder? query = null, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}", query);
        var method = HttpMethod.Patch;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        JsonObject doc = new();

        if (json.ContainsKey("doc") && json["doc"] is JsonObject value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Retrieves the total document count for a <c>collection</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> for filtering.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The total document count.</returns>
    public async Task<int> Count(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/count", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = TotalDocsDTO.FromJson(json);

        return dto.TotalDocs;
    }

    /// <summary>
    /// Retrieves a <c>global</c> document.
    /// <para>A <c>global</c> that has never been saved, or that read access filters out, is still
    /// returned, but without an <c>Id</c>.</para>
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Depth</c>, <c>Locale</c>, <c>Draft(true)</c>, <c>Select</c>, <c>Populate</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>global</c> document.</returns>
    public async Task<DocumentDTO> FindGlobal(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Updates a <c>global</c> document.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="data">The fields to update.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Draft(true)</c> to save the edit as a draft version, or <c>Locale</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The updated <c>global</c> document.</returns>
    public async Task<DocumentDTO> UpdateGlobal(string slug, Dictionary<string, object?> data, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Post;
        var body = data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        JsonObject result = new();

        if (json.ContainsKey("result") && json["result"] is JsonObject value)
        {
            result = value;
        }

        var dto = DocumentDTO.FromJson(result);

        return dto;
    }

    /// <summary>
    /// Retrieves a paginated list of <c>versions</c> for a <c>collection</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> for filtering, sorting, pagination.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A paginated response containing <c>version</c> documents.</returns>
    public async Task<PaginatedDocsDTO> FindVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a single <c>version</c> document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The <c>version</c> ID.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Depth</c>, <c>Locale</c>, <c>Draft(true)</c>, <c>Select</c>, <c>Populate</c>, <c>Trash(true)</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>version</c> document.</returns>
    public async Task<DocumentDTO> FindVersionById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Restores a <c>collection</c> document to a specific <c>version</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The <c>version</c> ID to restore.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Depth</c>, <c>Locale</c>, <c>Draft(true)</c>, <c>Populate</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The restored document.</returns>
    public async Task<DocumentDTO> RestoreVersion(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}", query);
        var method = HttpMethod.Post;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a paginated list of <c>versions</c> for a <c>global</c>.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> for filtering, sorting, pagination.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A paginated response containing <c>version</c> documents.</returns>
    public async Task<PaginatedDocsDTO> FindGlobalVersions(string slug, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a single <c>global</c> <c>version</c> document by its ID.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="id">The <c>version</c> ID.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Depth</c>, <c>Locale</c>, <c>Draft(true)</c>, <c>Select</c>, <c>Populate</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>version</c> document.</returns>
    public async Task<DocumentDTO> FindGlobalVersionById(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}", query);
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Restores a <c>global</c> document to a specific <c>version</c>.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="id">The <c>version</c> ID to restore.</param>
    /// <param name="query">Optional <see cref="QueryBuilder"/> — e.g. <c>Depth</c>, <c>Locale</c>, <c>Draft(true)</c>, <c>Populate</c>.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The restored document.</returns>
    public async Task<DocumentDTO> RestoreGlobalVersion(string slug, string id, QueryBuilder? query = null, CancellationToken cancellationToken = default)
    {
        var url = AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}", query);
        var method = HttpMethod.Post;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        JsonObject doc = new();

        if (json.ContainsKey("doc") && json["doc"] is JsonObject value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Sends the credentials to Payload and returns the JWT it issues.
    /// <para>To authenticate this SDK instance pass the returned token to
    /// <see cref="SetJwtAuth"/> before making further requests.</para>
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="data">The login credentials (e.g. <c>{ email, password }</c>).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The login result containing token, expiration, and user.</returns>
    public async Task<LoginResultDTO> Login(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/login";
        var method = HttpMethod.Post;
        var body = data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        var dto = LoginResultDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves the currently authenticated user.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The current user with token and session metadata.</returns>
    public async Task<MeResultDTO> Me(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/me";
        var json = await Request(url, cancellationToken: cancellationToken);
        var dto = MeResultDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Exchanges the previously set token for a new one.
    /// <para>Pass the new token to <see cref="SetJwtAuth"/> to use it for further requests.
    /// The previous token stays valid until it expires.</para>
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The new token, expiration, and user.</returns>
    public async Task<RefreshResultDTO> RefreshToken(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/refresh-token";
        var method = HttpMethod.Post;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        var dto = RefreshResultDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Initiates the forgot-password flow.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="data">The request data (e.g. <c>{ email }</c>).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A message confirming the request was processed.</returns>
    public async Task<MessageDTO> ForgotPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/forgot-password";
        var method = HttpMethod.Post;
        var body = data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        var dto = MessageDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Completes a password reset using a reset token.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="data">The reset data (e.g. <c>{ token, password }</c>).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The user document and optional new token.</returns>
    public async Task<ResetPasswordResultDTO> ResetPassword(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/reset-password";
        var method = HttpMethod.Post;
        var body = data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        var dto = ResetPasswordResultDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Verifies a user's email address using a verification token.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="token">The email verification token.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A message confirming the verification result.</returns>
    public async Task<MessageDTO> VerifyEmail(string slug, string token, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/verify/{Uri.EscapeDataString(token)}";
        var method = HttpMethod.Post;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        var dto = MessageDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Ends the session of the previously set token.
    /// <para>To clear this SDK instance's credential call <see cref="ClearAuth"/> afterwards.</para>
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A message confirming the logout.</returns>
    public async Task<MessageDTO> Logout(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/logout";
        var method = HttpMethod.Post;

        var json = await Request(url, method, cancellationToken: cancellationToken);
        var dto = MessageDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Unlocks a user account locked by failed login attempts.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="data">The request data (e.g. <c>{ email }</c>).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A message confirming the unlock.</returns>
    public async Task<MessageDTO> Unlock(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/unlock";
        var method = HttpMethod.Post;
        var body = data.ToJsonContent();

        var json = await Request(url, method, body, cancellationToken);
        var dto = MessageDTO.FromJson(json);

        return dto;
    }
}
