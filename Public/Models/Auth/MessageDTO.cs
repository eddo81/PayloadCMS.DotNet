using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;
namespace PayloadCMS.DotNet.Models.Auth;

/// <summary>
/// Represents a simple message response from Payload CMS.
/// <para>Used by <c>forgot-password</c>, <c>verify-email</c>, <c>logout</c>,
/// and <c>unlock</c> endpoints.</para>
/// </summary>
public sealed class MessageDTO
{
    /// <summary>The message returned by the endpoint.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="MessageDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS endpoint.</param>
    /// <returns>A populated instance.</returns>
    internal static MessageDTO FromJson(JsonObject json)
    {
        var dto = new MessageDTO();

        if (json.ContainsKey("message") && json["message"].TryConvertString() is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
