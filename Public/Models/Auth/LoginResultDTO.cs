using Payload.CMS.Internal.Utils;
using Payload.CMS.Public.Models.Collection;

namespace Payload.CMS.Public.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>login</c> endpoint.
/// </summary>
public sealed class LoginResultDTO
{
    /// <summary>The JWT token issued on login.</summary>
    public string Token { get; set; } = "";
    /// <summary>The token expiration as a Unix timestamp.</summary>
    public int Exp { get; set; } = 0;
    /// <summary>The authenticated user document.</summary>
    public DocumentDTO User { get; set; } = new();
    /// <summary>The message returned by the endpoint.</summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="LoginResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>login</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static LoginResultDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new LoginResultDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("token") && data["token"] is string tokenValue)
        {
            dto.Token = tokenValue;
        }

        if (data.ContainsKey("exp"))
        {
            dto.Exp = JsonParser.TryConvertInt(data["exp"]) ?? dto.Exp;
        }

        if (data.ContainsKey("user") && data["user"] is Dictionary<string, object?> userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        if (data.ContainsKey("message") && data["message"] is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
