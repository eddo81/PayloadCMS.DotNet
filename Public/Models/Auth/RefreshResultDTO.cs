using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;
using PayloadCMS.DotNet.Models.Collection;

namespace PayloadCMS.DotNet.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>refresh-token</c> endpoint.
/// </summary>
public sealed class RefreshResultDTO
{
    /// <summary>The new JWT token.</summary>
    public string RefreshedToken { get; set; } = "";
    /// <summary>The token expiration as a Unix timestamp, in seconds.</summary>
    public long Exp { get; set; } = 0;
    /// <summary>The authenticated user document.</summary>
    public DocumentDTO User { get; set; } = new();

    /// <summary>
    /// Maps a plain JSON object into a <see cref="RefreshResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>refresh-token</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    internal static RefreshResultDTO FromJson(JsonObject json)
    {
        var dto = new RefreshResultDTO();
        var data = json ?? new JsonObject();

        if (data.ContainsKey("refreshedToken") && data["refreshedToken"].TryConvertString() is string refreshedTokenValue)
        {
            dto.RefreshedToken = refreshedTokenValue;
        }

        if (data.ContainsKey("exp"))
        {
            dto.Exp = data["exp"].TryConvertLong() ?? dto.Exp;
        }

        if (data.ContainsKey("user") && data["user"] is JsonObject userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        return dto;
    }
}
