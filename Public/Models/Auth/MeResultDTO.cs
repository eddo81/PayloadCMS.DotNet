using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Models.Collection;

namespace PayloadCMS.DotNet.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>me</c> endpoint.
/// </summary>
public sealed class MeResultDTO
{
    /// <summary>The authenticated user, or <c>null</c> when the request is not authenticated.</summary>
    public DocumentDTO? User { get; set; } = null;
    /// <summary>The active JWT token.</summary>
    public string Token { get; set; } = "";
    /// <summary>The token expiration as a Unix timestamp, in seconds.</summary>
    public long Exp { get; set; } = 0;
    /// <summary>The collection the user belongs to.</summary>
    public string Collection { get; set; } = "";
    /// <summary>The authentication strategy in use.</summary>
    public string Strategy { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="MeResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>me</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    internal static MeResultDTO FromJson(JsonObject json)
    {
        var dto = new MeResultDTO();

        if (json.ContainsKey("user") && json["user"] is JsonObject userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        if (json.ContainsKey("token") && json["token"].TryConvertString() is string tokenValue)
        {
            dto.Token = tokenValue;
        }

        if (json.ContainsKey("exp"))
        {
            dto.Exp = json["exp"].TryConvertLong() ?? dto.Exp;
        }

        if (json.ContainsKey("collection") && json["collection"].TryConvertString() is string collectionValue)
        {
            dto.Collection = collectionValue;
        }

        if (json.ContainsKey("strategy") && json["strategy"].TryConvertString() is string strategyValue)
        {
            dto.Strategy = strategyValue;
        }

        return dto;
    }
}
