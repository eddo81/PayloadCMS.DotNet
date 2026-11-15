using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;

namespace PayloadCMS.DotNet.Models.Errors;

/// <summary>
/// Represents a per-document error within a bulk write operation response.
/// <para>A bulk operation can partially succeed: documents that could not be written are reported
/// here, alongside the documents that were.</para>
/// </summary>
public sealed class BulkOperationErrorDTO
{
    /// <summary>The ID of the document that failed.</summary>
    public string Id { get; set; } = "";
    /// <summary>The error message.</summary>
    public string Message { get; set; } = "";

    internal static BulkOperationErrorDTO FromJson(JsonObject json)
    {
        var dto = new BulkOperationErrorDTO();

        if (json.ContainsKey("id") && json["id"].TryConvertString() is string idValue)
        {
            dto.Id = idValue;
        }
        else if (json.ContainsKey("id") && json["id"]?.GetValueKind() == JsonValueKind.Number)
        {
            // Postgres/SQLite adapters return numeric ids — normalize to string.
            dto.Id = Convert.ToString(json["id"].ToClr(), CultureInfo.InvariantCulture)!;
        }

        if (json.ContainsKey("message") && json["message"].TryConvertString() is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
