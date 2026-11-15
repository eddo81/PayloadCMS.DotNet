using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Models.Collection;

namespace PayloadCMS.DotNet.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>login</c> endpoint.
/// </summary>
public sealed class LoginResultDTO
{
    /// <summary>The JWT token issued on login.</summary>
    public string Token { get; set; } = "";
    /// <summary>The token expiration as a Unix timestamp, in seconds.</summary>
    public long Exp { get; set; } = 0;
    /// <summary>The authenticated user document.</summary>
    public DocumentDTO User { get; set; } = new();
    /// <summary>The message returned by the endpoint.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="LoginResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>login</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    internal static LoginResultDTO FromJson(JsonObject json)
    {
        var dto = new LoginResultDTO();
        var data = json ?? new JsonObject();

        if (data.ContainsKey("token") && data["token"].TryConvertString() is string tokenValue)
        {
            dto.Token = tokenValue;
        }

        if (data.ContainsKey("exp"))
        {
            dto.Exp = data["exp"].TryConvertLong() ?? dto.Exp;
        }

        if (data.ContainsKey("user") && data["user"] is JsonObject userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        if (data.ContainsKey("message") && data["message"].TryConvertString() is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
