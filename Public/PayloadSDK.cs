using System.Text.Json;
using PayloadCMS.DotNet.Config;
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
    private readonly QueryStringEncoder _encoder = new();
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of <see cref="PayloadSDK"/>.
    /// </summary>
    /// <param name="httpClient">
    /// The <see cref="HttpClient"/> instance to use for all requests.
    /// The caller is responsible for its lifetime and disposal.
    /// </param>
    /// <param name="baseUrl">The base URL of the Payload CMS instance (e.g. <c>https://cms.example.com</c>).</param>
    public PayloadSDK(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = _NormalizeUrl(baseUrl);
    }

    /// <summary>
    /// Validates and normalizes a base URL string.
    /// <para>Strips trailing slashes to prevent double-slash
    /// paths when building endpoint URLs.</para>
    /// </summary>
    /// <param name="url">The raw base URL to normalize.</param>
    /// <returns>The normalized URL without a trailing slash.</returns>
    /// <exception cref="Exception">If the URL is malformed.</exception>
    private string _NormalizeUrl(string url)
    {
        try
        {
            var urlString = new Uri(url).ToString();
            var normalized = urlString.TrimEnd('/');

            return normalized;
        }
        catch (Exception error)
        {
            throw new Exception($"[PayloadError] Invalid base URL: {url}", error);
        }
    }

    /// <summary>
    /// Sets the custom headers to include with every request.
    /// <para>These are merged with the default <c>Accept</c> and
    /// <c>Content-Type</c> headers at request time.</para>
    /// </summary>
    /// <param name="headers">The custom headers to set.</param>
    public void SetHeaders(Dictionary<string, string> headers)
    {
        _headers = headers;
    }

    /// <summary>
    /// Sets an API key credential for all subsequent requests.
    /// </summary>
    /// <param name="auth">The <see cref="ApiKeyAuth"/> credential to use.</param>
    public void SetApiKeyAuth(ApiKeyAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Sets a JWT bearer token credential for all subsequent requests.
    /// </summary>
    /// <param name="auth">The <see cref="JwtAuth"/> credential to use.</param>
    public void SetJwtAuth(JwtAuth auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Clears the current authentication credential.
    /// <para>Subsequent requests will be sent without authorization headers.</para>
    /// </summary>
    public void ClearAuth()
    {
        _auth = null;
    }

    /// <summary>
    /// Sends a raw HTTP request through the client pipeline.
    /// <para>An escape hatch for <c>Payload CMS</c> custom endpoints.
    /// Uses the same headers, auth, and error handling
    /// but returns raw JSON instead of a DTO.</para>
    /// </summary>
    /// <param name="config">The request options: method, path, optional body and query.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The parsed JSON response, or <c>null</c> for empty bodies.</returns>
    public async Task<Dictionary<string, object?>?> Request(RequestConfig config, CancellationToken cancellationToken = default)
    {
        var url = _AppendQueryString($"{_baseUrl}{config.Path}", config.Query);
        HttpContent? body = config.Body != null ? JsonParser.Serialize(config.Body) : null;

        return await _Fetch(url, config.Method, body, cancellationToken);
    }

    /// <summary>
    /// Appends a serialized query string to the given URL.
    /// </summary>
    /// <param name="url">The base URL to append query parameters to.</param>
    /// <param name="query">Optional query parameters.</param>
    /// <returns>The URL with an appended query string, if applicable.</returns>
    private string _AppendQueryString(string url, QueryBuilder? query)
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
    /// Executes an HTTP request and returns parsed JSON.
    /// <para>Merges default headers, applies auth, parses the
    /// response body, and normalizes errors into
    /// <see cref="PayloadError"/> instances.</para>
    /// </summary>
    /// <param name="url">Fully resolved request URL.</param>
    /// <param name="method">Optional HTTP method; defaults to GET.</param>
    /// <param name="body">Optional request body.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>Parsed JSON, or <c>null</c> for empty responses.</returns>
    /// <exception cref="PayloadError">On non-2xx responses.</exception>
    /// <exception cref="Exception">On network, parsing, or abort failures.</exception>
    private async Task<Dictionary<string, object?>?> _Fetch(string url, HttpMethod? method = null, HttpContent? body = null, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object?>? json = null;
        var defaultMethod = HttpMethod.Get;

        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["Content-Type"] = "application/json",
        };

        foreach (var kvp in _headers)
        {
            headers[kvp.Key] = kvp.Value;
        }

        if (body is MultipartFormDataContent)
        {
            headers.Remove("Content-Type");
        }

        if (_auth != null)
        {
            _auth.Apply(headers);
        }

        try
        {
            var resolvedMethod = method ?? defaultMethod;
            var request = new HttpRequestMessage(resolvedMethod, url);

            foreach (var kvp in headers)
            {
                if (kvp.Key == "Content-Type")
                {
                    continue;
                }

                request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
            }

            if (body != null)
            {
                request.Content = body;
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (text.Length > 0)
            {
                json = JsonParser.Parse(text);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new PayloadError(
                    statusCode: (int)response.StatusCode,
                    response: response,
                    cause: json
                );
            }

            return json;
        }
        catch (Exception error)
        {
            string message = "[PayloadError] Fetch failed";

            if (error is JsonException)
            {
                message = "[PayloadError] Failed to parse JSON response";
            }
            else if (error is HttpRequestException)
            {
                message = "[PayloadError] Network failure or CORS issue";
            }
            else if (error is TaskCanceledException)
            {
                message = "[PayloadError] Request was aborted or timed out";
            }
            else if (error is PayloadError)
            {
                throw;
            }
            else if (error is Exception)
            {
                message = $"[PayloadError] {error.Message}";
            }

            throw new Exception(message, error);
        }
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
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
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
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}", query);
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Creates a new document in a <c>collection</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="data">The document data to create.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The created document.</returns>
    public async Task<DocumentDTO> Create(string slug, Dictionary<string, object?> data, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}";
        var method = HttpMethod.Post;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
        Dictionary<string, object?> doc = new();

        if (json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Deletes multiple documents matching a query.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="query"><see cref="QueryBuilder"/> with <c>where</c> clause to select documents.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The bulk result containing deleted documents.</returns>
    public async Task<PaginatedDocsDTO> Delete(string slug, QueryBuilder query, CancellationToken cancellationToken = default)
    {
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Delete;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Deletes a single document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The deleted document.</returns>
    public async Task<DocumentDTO> DeleteById(string slug, string id, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}";
        var method = HttpMethod.Delete;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        Dictionary<string, object?> doc = new();

        if (json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Updates multiple documents matching a query.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="data">The fields to update on matching documents.</param>
    /// <param name="query"><see cref="QueryBuilder"/> with <c>where</c> clause to select documents.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The bulk result containing updated documents.</returns>
    public async Task<PaginatedDocsDTO> Update(string slug, Dictionary<string, object?> data, QueryBuilder query, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}", query);
        var method = HttpMethod.Patch;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Updates a single document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The document ID.</param>
    /// <param name="data">The fields to update.</param>
    /// <param name="file">Optional file for <c>upload</c>-enabled collections.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The updated document.</returns>
    public async Task<DocumentDTO> UpdateById(string slug, string id, Dictionary<string, object?> data, FileUpload? file = null, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/{Uri.EscapeDataString(id)}";
        var method = HttpMethod.Patch;
        HttpContent body = file != null ? FormDataBuilder.Build(file, data) : JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
        Dictionary<string, object?> doc = new();

        if (json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> value)
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
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/count", query);
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = TotalDocsDTO.FromJson(json);

        return dto.TotalDocs;
    }

    /// <summary>
    /// Retrieves a <c>global</c> document.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>global</c> document.</returns>
    public async Task<DocumentDTO> FindGlobal(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}";
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Updates a <c>global</c> document.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="data">The fields to update.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The updated <c>global</c> document.</returns>
    public async Task<DocumentDTO> UpdateGlobal(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}";
        var method = HttpMethod.Post;
        var body = JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
        Dictionary<string, object?> result = new();

        if (json.ContainsKey("result") && json["result"] is Dictionary<string, object?> value)
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
        var url = _AppendQueryString($"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions", query);
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a single <c>version</c> document by its ID.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The <c>version</c> ID.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>version</c> document.</returns>
    public async Task<DocumentDTO> FindVersionById(string slug, string id, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}";
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Restores a <c>collection</c> document to a specific <c>version</c>.
    /// </summary>
    /// <param name="slug">The <c>collection</c> slug.</param>
    /// <param name="id">The <c>version</c> ID to restore.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The restored document.</returns>
    public async Task<DocumentDTO> RestoreVersion(string slug, string id, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}";
        var method = HttpMethod.Post;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
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
        var url = _AppendQueryString($"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions", query);
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = PaginatedDocsDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Retrieves a single <c>global</c> <c>version</c> document by its ID.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="id">The <c>version</c> ID.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The <c>version</c> document.</returns>
    public async Task<DocumentDTO> FindGlobalVersionById(string slug, string id, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}";
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = DocumentDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Restores a <c>global</c> document to a specific <c>version</c>.
    /// </summary>
    /// <param name="slug">The <c>global</c> slug.</param>
    /// <param name="id">The <c>version</c> ID to restore.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The restored document.</returns>
    public async Task<DocumentDTO> RestoreGlobalVersion(string slug, string id, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/globals/{Uri.EscapeDataString(slug)}/versions/{Uri.EscapeDataString(id)}";
        var method = HttpMethod.Post;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        Dictionary<string, object?> doc = new();

        if (json.ContainsKey("doc") && json["doc"] is Dictionary<string, object?> value)
        {
            doc = value;
        }

        var dto = DocumentDTO.FromJson(doc);

        return dto;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="data">The login credentials (e.g. <c>{ email, password }</c>).</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The login result containing token, expiration, and user.</returns>
    public async Task<LoginResultDTO> Login(string slug, Dictionary<string, object?> data, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/login";
        var method = HttpMethod.Post;
        var body = JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
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
        var json = await _Fetch(url, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = MeResultDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Refreshes the current JWT token.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>The new token, expiration, and user.</returns>
    public async Task<RefreshResultDTO> RefreshToken(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/refresh-token";
        var method = HttpMethod.Post;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
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
        var body = JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
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
        var body = JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
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

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
        var dto = MessageDTO.FromJson(json);

        return dto;
    }

    /// <summary>
    /// Logs out the currently authenticated user.
    /// </summary>
    /// <param name="slug">The <c>auth</c>-enabled <c>collection</c> slug.</param>
    /// <param name="cancellationToken">An optional token to cancel the request.</param>
    /// <returns>A message confirming the logout.</returns>
    public async Task<MessageDTO> Logout(string slug, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/api/{Uri.EscapeDataString(slug)}/logout";
        var method = HttpMethod.Post;

        var json = await _Fetch(url, method, cancellationToken: cancellationToken) ?? new Dictionary<string, object?>();
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
        var body = JsonParser.Serialize(data);

        var json = await _Fetch(url, method, body, cancellationToken) ?? new Dictionary<string, object?>();
        var dto = MessageDTO.FromJson(json);

        return dto;
    }
}
