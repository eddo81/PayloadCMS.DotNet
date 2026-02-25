namespace Payload.CMS.Internal.Contracts;

/// <summary>
/// Defines the shape of a file for Payload CMS <c>upload</c> collections.
/// </summary>
internal interface IFileUpload
{
    /// <value>Property <c>Content</c> represents the binary content of the file.</value>
    byte[] Content { get; }

    /// <value>The filename to use for the upload (e.g., "photo.jpg").</value>
    string FileName { get; }

    /// <value>Optional MIME type (e.g., "image/jpeg"). If omitted, the content's type is used as-is.</value>
    string? MimeType { get; } 
}
