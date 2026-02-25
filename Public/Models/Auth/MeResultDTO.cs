using Payload.CMS.Internal.Utils;
using Payload.CMS.Public.Models.Collection;

namespace Payload.CMS.Public.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>me</c> endpoint.
/// </summary>
public sealed class MeResultDTO
{
    /// <summary>The currently authenticated user document.</summary>
    public DocumentDTO User { get; set; } = new();
    /// <summary>The active JWT token.</summary>
    public string Token { get; set; } = "";
    /// <summary>The token expiration as a Unix timestamp.</summary>
    public int Exp { get; set; } = 0;
    /// <summary>The collection the user belongs to.</summary>
    public string Collection { get; set; } = "";
    /// <summary>The authentication strategy in use.</summary>
    public string Strategy { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="MeResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>me</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static MeResultDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new MeResultDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("user") && data["user"] is Dictionary<string, object?> userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        if (data.ContainsKey("token") && data["token"] is string tokenValue)
        {
            dto.Token = tokenValue;
        }

        if (data.ContainsKey("exp"))
        {
            dto.Exp = JsonParser.TryConvertInt(data["exp"]) ?? dto.Exp;
        }

        if (data.ContainsKey("collection") && data["collection"] is string collectionValue)
        {
            dto.Collection = collectionValue;
        }

        if (data.ContainsKey("strategy") && data["strategy"] is string strategyValue)
        {
            dto.Strategy = strategyValue;
        }

        return dto;
    }
}
