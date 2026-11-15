using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;

namespace PayloadCMS.DotNet.Models.Collection;

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
    internal static TotalDocsDTO FromJson(JsonObject json)
    {
        var dto = new TotalDocsDTO();
        var data = json ?? new JsonObject();

        if (data.ContainsKey("totalDocs"))
        {
            dto.TotalDocs = data["totalDocs"].TryConvertInt() ?? dto.TotalDocs;
        }

        return dto;
    }

    /// <summary>
    /// Maps a <see cref="TotalDocsDTO"/> into a plain JSON object.
    /// </summary>
    /// <param name="dto">The instance to serialize.</param>
    /// <returns>A plain JSON object for transport.</returns>
    internal static JsonObject ToJson(TotalDocsDTO dto)
    {
        return new JsonObject
        {
            ["totalDocs"] = dto.TotalDocs
        };
    }
}
