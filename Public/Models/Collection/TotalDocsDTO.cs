using Payload.CMS.Internal.Utils;

namespace Payload.CMS.Public.Models.Collection;

/// <summary>
/// Represents a total document count from a Payload CMS collection.
/// </summary>
public sealed class TotalDocsDTO
{
    /// <summary>The total number of documents matching the query.</summary>
    public int TotalDocs { get; set; } = 0;

    /// <summary>
    /// Maps a plain JSON object into a <see cref="TotalDocsDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS count endpoint.</param>
    /// <returns>A populated instance.</returns>
    public static TotalDocsDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new TotalDocsDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("totalDocs"))
        {
            dto.TotalDocs = JsonParser.TryConvertInt(data["totalDocs"]) ?? dto.TotalDocs;
        }

        return dto;
    }

    /// <summary>
    /// Maps a <see cref="TotalDocsDTO"/> into a plain JSON object.
    /// </summary>
    /// <param name="dto">The instance to serialize.</param>
    /// <returns>A plain JSON object for transport.</returns>
    public static Dictionary<string, object?> ToJson(TotalDocsDTO dto)
    {
        return new Dictionary<string, object?>
        {
            ["totalDocs"] = dto.TotalDocs
        };
    }
}
