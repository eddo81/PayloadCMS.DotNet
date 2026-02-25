namespace Payload.CMS.Public;

/// <summary>
/// A structured error thrown on failed Payload CMS requests.
/// <para>Captures the HTTP status code, the originating response,
/// and an optional cause (e.g. parsed JSON error payload).</para>
/// <para>Thrown by <see cref="Client"/> on non-2xx responses or
/// fatal transport / parsing errors.</para>
/// </summary>
public class PayloadError : Exception
{
    /// <summary>The HTTP status code returned by the server.</summary>
    public readonly int StatusCode;

    /// <summary>The originating HTTP response, if available.</summary>
    public readonly HttpResponseMessage? Response;

    /// <summary>
    /// The raw cause of the error — typically a parsed JSON error payload.
    /// May be any type; cast before use.
    /// </summary>
    public readonly object? Cause;

    /// <summary>Initializes a new <see cref="PayloadError"/>.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="message">Optional human-readable error message.</param>
    /// <param name="response">The originating HTTP response.</param>
    /// <param name="cause">The underlying cause — typically a parsed JSON error body.</param>
    public PayloadError(
        int statusCode,
        string? message = null,
        HttpResponseMessage? response = null,
        object? cause = null)
        : base(message ?? $"[PayloadError] Request failed with status: {statusCode}", cause as Exception)
    {
        StatusCode = statusCode;
        Response = response;
        Cause = cause;
    }
}
