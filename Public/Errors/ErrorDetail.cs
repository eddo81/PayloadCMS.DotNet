namespace PayloadCMS.DotNet;

/// <summary>
/// Represents a single error entry extracted from a <see cref="PayloadError"/> response body.
/// </summary>
public sealed class ErrorDetail
{
    /// <summary>The human-readable error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The field name associated with the error, if any.</summary>
    public string? Field { get; set; }
}
