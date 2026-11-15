using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Models.Errors;

namespace PayloadCMS.DotNet.Models.Collection;

/// <summary>
/// Represents the result of a bulk write operation (update or delete).
/// Maps to Payload CMS's <c>BulkOperationResult</c> response shape.
/// </summary>
public sealed class BulkOperationDTO
{
    /// <summary>The documents that were successfully affected.</summary>
    public List<DocumentDTO> Docs { get; set; } = new();
    /// <summary>Any per-document errors that occurred during the operation.</summary>
    public List<BulkOperationErrorDTO> Errors { get; set; } = new();

    internal static BulkOperationDTO FromJson(JsonObject json)
    {
        var dto = new BulkOperationDTO();
        var data = json ?? new JsonObject();

        if (data.ContainsKey("docs") && data["docs"] is JsonArray docList)
        {
            foreach (var item in docList)
            {
                if (item is JsonObject docItem)
                {
                    dto.Docs.Add(DocumentDTO.FromJson(docItem));
                }
            }
        }

        if (data.ContainsKey("errors") && data["errors"] is JsonArray errorList)
        {
            foreach (var item in errorList)
            {
                if (item is JsonObject errorItem)
                {
                    dto.Errors.Add(BulkOperationErrorDTO.FromJson(errorItem));
                }
            }
        }

        return dto;
    }
}
