namespace PayloadCMS.DotNet.Internal.Contracts;

/// <summary>
/// Defines the shape of a file for Payload CMS <c>upload</c> collections.
/// </summary>
internal interface IFileUpload
{
    /// <value>Property <c>Content</c> represents the binary content of the file.</value>
    byte[] Content { get; }

    /// <value>The filename to use for the upload (e.g., "photo.jpg").</value>
    string FileName { get; }

    /// <value>The MIME type (e.g., "image/jpeg"). Defaults to <c>application/octet-stream</c>.</value>
    string MimeType { get; } 
}
