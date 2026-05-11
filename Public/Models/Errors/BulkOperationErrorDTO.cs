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
}
