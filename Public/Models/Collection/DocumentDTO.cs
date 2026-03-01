namespace PayloadCMS.DotNet.Models.Collection;

/// <summary>
/// Represents a Payload CMS document.
/// <para>The <c>Json</c> field retains the full raw response so
/// user-defined fields remain accessible without the
/// DTO modeling every possible schema.</para>
/// </summary>
public sealed class DocumentDTO
{
    /// <summary>The full raw JSON response.</summary>
    public Dictionary<string, object?> Json { get; set; } = new();
    /// <summary>The document ID.</summary>
    public string Id { get; set; } = "";
    /// <summary>The UTC timestamp the document was created, if present.</summary>
    public DateTime? CreatedAt { get; set; } = null;
    /// <summary>The UTC timestamp the document was last updated, if present.</summary>
    public DateTime? UpdatedAt { get; set; } = null;

    /// <summary>
    /// Maps a plain JSON object into a <see cref="DocumentDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static DocumentDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new DocumentDTO();
        var data = json ?? new Dictionary<string, object?>();

        dto.Json = data;

        if (data.ContainsKey("id") && data["id"] is string idValue)
        {
            dto.Id = idValue;
        }

        if (data.ContainsKey("createdAt") && data["createdAt"] is string createdAtString && createdAtString != "" && DateTime.TryParse(createdAtString, out DateTime createdAtDate))
        {
            dto.CreatedAt = createdAtDate;
        }

        if (data.ContainsKey("updatedAt") && data["updatedAt"] is string updatedAtString && updatedAtString != "" && DateTime.TryParse(updatedAtString, out DateTime updatedAtDate))
        {
            dto.UpdatedAt = updatedAtDate;
        }

        return dto;
    }

    /// <summary>
    /// Maps a <see cref="DocumentDTO"/> into a plain JSON object.
    /// </summary>
    /// <param name="dto">The instance to serialize.</param>
    /// <returns>A plain JSON object for transport.</returns>
    public static Dictionary<string, object?> ToJson(DocumentDTO dto)
    {
        var result = new Dictionary<string, object?>(dto.Json)
        {
            ["id"] = dto.Id
        };

        if (dto.CreatedAt.HasValue)
        {
            result["createdAt"] = dto.CreatedAt.Value.ToString("O");
        }

        if (dto.UpdatedAt.HasValue)
        {
            result["updatedAt"] = dto.UpdatedAt.Value.ToString("O");
        }

        return result;
    }
}
