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
    public static MessageDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new MessageDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("message") && data["message"] is string messageValue)
        {
            dto.Message = messageValue;
        }

        return dto;
    }
}
