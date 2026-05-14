namespace PayloadCMS.DotNet.Models.Errors;

/// <summary>
/// Represents one entry in the <c>errors[]</c> array from a failed Payload CMS response.
/// <para>
/// Payload's error response shape is intentionally dynamic — only <c>name</c>, <c>message</c>,
/// and <c>field</c> are guaranteed across all error types. The raw <c>Json</c> property gives
/// access to the full entry, including the <c>data</c> block present on <c>ValidationError</c>
/// and <c>APIError</c> responses, which consumers can inspect and map to their own types.
/// </para>
/// </summary>
public sealed class RequestErrorDTO
{
    /// <summary>The full raw JSON for this <c>errors[n]</c> entry.</summary>
    public Dictionary<string, object?> Json { get; set; } = new();

    /// <summary>
    /// The error class name from the wire format, if present
    /// (e.g. <c>"ValidationError"</c>, <c>"Forbidden"</c>, <c>"NotFound"</c>).
    /// Sourced from <c>errors[n].name</c>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>The human-readable error message. Sourced from <c>errors[n].message</c>.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// The field path associated with the error, if present.
    /// Sourced from <c>errors[n].field</c> — set on Mongoose validation error items only.
    /// </summary>
    public string? Field { get; set; }

    internal static RequestErrorDTO FromJson(Dictionary<string, object?> json)
    {
        var result = new RequestErrorDTO
        {
            Json = json,
        };

        if (json.ContainsKey("name") && json["name"] is string name)
        {
            result.Name = name;
        }

        if (json.ContainsKey("message") && json["message"] is string messageValue)
        {
            result.Message = messageValue;
        }

        if (json.ContainsKey("field") && json["field"] is string fieldValue)
        {
            result.Field = fieldValue;
        }

        return result;
    }
}
