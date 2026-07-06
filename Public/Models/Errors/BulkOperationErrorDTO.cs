using System.Globalization;

namespace PayloadCMS.DotNet.Models.Errors;

/// <summary>
/// Represents a per-document error within a bulk write operation response.
/// A bulk operation can partially succeed — the operation itself returns 200,
/// but individual documents that could not be updated or deleted are reported here
/// alongside the successfully affected documents.
/// </summary>
public sealed class BulkOperationErrorDTO
{
    /// <summary>The ID of the document that failed.</summary>
    public string Id { get; set; } = "";
    /// <summary>The error message.</summary>
    public string Message { get; set; } = "";

    internal static BulkOperationErrorDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new BulkOperationErrorDTO();

        if (json.ContainsKey("id") && json["id"] is string idValue)
        {
            dto.Id = idValue;
        }
        else if (json.ContainsKey("id") && (json["id"] is int || json["id"] is long || json["id"] is double))
        {
            // Postgres/SQLite adapters return numeric ids — normalize to string.
            dto.Id = Convert.ToString(json["id"], CultureInfo.InvariantCulture)!;
        }

        if (json.ContainsKey("message") && json["message"] is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
