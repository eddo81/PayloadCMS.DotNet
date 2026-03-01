using PayloadCMS.DotNet.Internal.Contracts;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PayloadCMS.DotNet.Internal.Upload;

/// <summary>
/// Constructs a <c>FormData</c> body for file upload requests.
///
/// Payload CMS expects <c>file</c> (Blob) and <c>_payload</c>
/// (JSON string) fields.
/// </summary>
internal class FormDataBuilder
{
    public static MultipartFormDataContent Build(IFileUpload file, Dictionary<string, object?> data)
    {
        var formData = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(content: file.Content);

        if (file.MimeType != null)
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType);
        }

        formData.Add(name: "file", content: fileContent, fileName: file.FileName);

        formData.Add(name: "_payload", content: JsonContent.Create(data));

        return formData;
    }
}
