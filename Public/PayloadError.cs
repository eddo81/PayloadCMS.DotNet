using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Models.Errors;

namespace PayloadCMS.DotNet;

/// <summary>
/// A structured error thrown on failed Payload CMS requests.
/// <para>Captures the HTTP status code, the originating response, the raw response
/// body, and a typed list of error entries parsed from that body.</para>
/// <para>Thrown by <see cref="PayloadSDK"/> on non-2xx responses.</para>
/// </summary>
public class PayloadError : Exception
{
    /// <summary>The HTTP status code returned by the server.</summary>
    public readonly int StatusCode;

    /// <summary>The originating HTTP response, if available.</summary>
    public readonly HttpResponseMessage? Response;

    /// <summary>
    /// The raw unparsed JSON response body, if available.
    /// Use this as an escape hatch to access undocumented or unrecognized top-level fields.
    /// </summary>
    public readonly string? Body;

    /// <summary>
    /// The server-side stack trace, if present.
    /// Payload includes this only in development mode via <c>ErrorResult.stack</c>.
    /// </summary>
    public readonly string? ServerStack;

    /// <summary>
    /// The parsed error entries from <c>errors[]</c> in the response body.
    /// Each entry exposes the base fields Payload guarantees across all error types
    /// (<see cref="ErrorResultDTO.Name"/>, <see cref="ErrorResultDTO.Message"/>,
    /// <see cref="ErrorResultDTO.Field"/>), plus a <see cref="ErrorResultDTO.Json"/>
    /// escape hatch for richer types such as <c>ValidationError</c>.
    /// </summary>
    public readonly IReadOnlyList<ErrorResultDTO> Result;

    /// <summary>Initializes a new <see cref="PayloadError"/>.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="response">The originating HTTP response.</param>
    /// <param name="body">The raw JSON response body string.</param>
    public PayloadError(int statusCode, HttpResponseMessage? response = null, string? body = null) : base($"[PayloadError] Request failed with status: {statusCode}")
    {
        StatusCode = statusCode;
        Response = response;
        Body = body;

        Dictionary<string, object?>? json = null;

        if (body != null)
        {
            try
            {
                json = JsonParser.Parse(body);
            }
            catch
            {
                // Non-JSON body — leave json null, Result will be empty
            }
        }

        if (json != null && json.ContainsKey("stack") && json["stack"] is string stackValue)
        {
            ServerStack = stackValue;
        }

        var result = new List<ErrorResultDTO>();

        if (json != null && json.ContainsKey("errors") && json["errors"] is List<object?> errors)
        {
            foreach (var item in errors)
            {
                if (item is not Dictionary<string, object?> error)
                {
                    continue;
                }

                result.Add(ErrorResultDTO.FromJson(error));
            }
        }

        Result = result;
    }
}
