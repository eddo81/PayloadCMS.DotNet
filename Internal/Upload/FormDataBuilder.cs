using PayloadCMS.DotNet.Internal.Contracts;
using System.Net.Http.Headers;
using System.Text.Json;

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

        // Plain string part — mirrors TS formData.append('_payload', JSON.stringify(data)).
        // JsonContent would stamp the part with Content-Type: application/json, which some
        // multipart parsers treat as a file rather than a string field.
        formData.Add(name: "_payload", content: new StringContent(JsonSerializer.Serialize(data)));

        return formData;
    }
}
