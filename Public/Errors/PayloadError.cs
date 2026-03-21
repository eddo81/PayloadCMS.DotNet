namespace PayloadCMS.DotNet;

/// <summary>
/// A structured error thrown on failed Payload CMS requests.
/// <para>Captures the HTTP status code, the originating response,
/// and an optional cause (e.g. parsed JSON error payload).</para>
/// <para>Thrown by <see cref="PayloadSDK"/> on non-2xx responses or
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
    public PayloadError(int statusCode, string? message = null, HttpResponseMessage? response = null, object? cause = null) : base(message ?? $"[PayloadError] Request failed with status: {statusCode}", cause as Exception) {
        StatusCode = statusCode;
        Response = response;
        Cause = cause;
    }

    /// <summary>
    /// Extracts structured error entries from the response body.
    /// <para>Navigates <c>Cause["errors"]</c> for validation-style errors (e.g. duplicate
    /// email, missing required field), or falls back to a top-level
    /// <c>Cause["message"]</c> for simpler error shapes (e.g. auth errors).</para>
    /// <para>Returns an empty list if no recognisable error structure is found.</para>
    /// </summary>
    public IReadOnlyList<ErrorDetail> GetDetails()
    {
        if (Cause is not Dictionary<string, object?> cause)
        {
            return new List<ErrorDetail>();
        }

        if (cause.ContainsKey("errors") && cause["errors"] is List<object?> errors)
        {
            var details = new List<ErrorDetail>();

            foreach (var item in errors)
            {
                if (item is not Dictionary<string, object?> error)
                {
                    continue;
                }

                if (!error.ContainsKey("message") || error["message"] is not string message)
                {
                    continue;
                }

                string? field = null;

                if (error.ContainsKey("field") && error["field"] is string fieldValue)
                {
                    field = fieldValue;
                }

                details.Add(new ErrorDetail { Message = message, Field = field });
            }

            return details;
        }

        if (cause.ContainsKey("message") && cause["message"] is string causeMessage)
        {
            return new List<ErrorDetail> { new ErrorDetail { Message = causeMessage } };
        }

        return new List<ErrorDetail>();
    }
}
