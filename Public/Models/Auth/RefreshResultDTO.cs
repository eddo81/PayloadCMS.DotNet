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
    /// <summary>The token expiration as a Unix timestamp.</summary>
    public int Exp { get; set; } = 0;
    /// <summary>The authenticated user document.</summary>
    public DocumentDTO User { get; set; } = new();

    /// <summary>
    /// Maps a plain JSON object into a <see cref="RefreshResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>refresh-token</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static RefreshResultDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new RefreshResultDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("refreshedToken") && data["refreshedToken"] is string refreshedTokenValue)
        {
            dto.RefreshedToken = refreshedTokenValue;
        }

        if (data.ContainsKey("exp"))
        {
            dto.Exp = JsonParser.TryConvertInt(data["exp"]) ?? dto.Exp;
        }

        if (data.ContainsKey("user") && data["user"] is Dictionary<string, object?> userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        return dto;
    }
}
