namespace PayloadCMS.DotNet.Models.Collection;

/// <summary>Represents a single error from a bulk operation.</summary>
public sealed class BulkOperationError
{
    /// <summary>The ID of the document that failed.</summary>
    public string Id { get; set; } = "";
    /// <summary>The error message.</summary>
    public string Message { get; set; } = "";
}

/// <summary>
/// Represents the result of a bulk write operation (update or delete).
/// Maps to Payload CMS's <c>BulkOperationResult</c> response shape.
/// </summary>
public sealed class BulkOperationDTO
{
    /// <summary>The documents that were successfully affected.</summary>
    public List<DocumentDTO> Docs { get; set; } = new();
    /// <summary>Any per-document errors that occurred during the operation.</summary>
    public List<BulkOperationError> Errors { get; set; } = new();

    internal static BulkOperationDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new BulkOperationDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("docs") && data["docs"] is System.Collections.IList docList)
        {
            foreach (var item in docList)
            {
                if (item is Dictionary<string, object?> docItem)
                {
                    dto.Docs.Add(DocumentDTO.FromJson(docItem));
                }
            }
        }

        if (data.ContainsKey("errors") && data["errors"] is System.Collections.IList errorList)
        {
            foreach (var item in errorList)
            {
                if (item is Dictionary<string, object?> errorItem)
                {
                    var error = new BulkOperationError();

                    if (errorItem.ContainsKey("id") && errorItem["id"] is string idValue)
                    {
                        error.Id = idValue;
                    }

                    if (errorItem.ContainsKey("message") && errorItem["message"] is string messageValue)
                    {
                        error.Message = messageValue;
                    }

                    dto.Errors.Add(error);
                }
            }
        }

        return dto;
    }
}
