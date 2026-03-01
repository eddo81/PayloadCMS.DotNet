using PayloadCMS.DotNet.Models.Collection;

namespace PayloadCMS.DotNet.Models.Auth;

/// <summary>
/// Represents the response from a Payload CMS <c>reset-password</c> endpoint.
/// </summary>
public sealed class ResetPasswordResultDTO
{
    /// <summary>The user document after the password reset.</summary>
    public DocumentDTO User { get; set; } = new();
    /// <summary>The JWT token issued after the reset, if any.</summary>
    public string Token { get; set; } = "";

    /// <summary>
    /// Maps a plain JSON object into a <see cref="ResetPasswordResultDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS <c>reset-password</c> endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static ResetPasswordResultDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new ResetPasswordResultDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("user") && data["user"] is Dictionary<string, object?> userObject)
        {
            dto.User = DocumentDTO.FromJson(userObject);
        }

        if (data.ContainsKey("token") && data["token"] is string tokenValue)
        {
            dto.Token = tokenValue;
        }

        return dto;
    }
}
