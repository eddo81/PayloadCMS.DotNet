using PayloadCMS.DotNet.Internal.Contracts;

namespace PayloadCMS.DotNet.Upload;

/// <summary>
/// Represents a file to upload to a Payload CMS <c>upload</c> collection.
/// <para>Pass to the <c>file</c> parameter of <c>PayloadSDK.Create</c>,
/// <c>PayloadSDK.UpdateById</c>, or <c>PayloadSDK.Update</c>.</para>
/// </summary>
public sealed record FileUpload(byte[] Content, string FileName, string? MimeType = null) : IFileUpload
{
    private const string DefaultMimeType = "application/octet-stream";
    private readonly string _mimeType = MimeType ?? DefaultMimeType;

    /// <summary>
    /// The MIME type sent with the file part.
    /// <para>When not supplied, <c>application/octet-stream</c> is used — a multipart part with
    /// no declared type is read as <c>text/plain</c> by the server.</para>
    /// </summary>
    public string MimeType
    {
      get => _mimeType;
      init => _mimeType = value ?? DefaultMimeType;
    }
}
