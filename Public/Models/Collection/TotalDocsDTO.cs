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

        if (json.ContainsKey("totalDocs"))
        {
            dto.TotalDocs = json["totalDocs"].TryConvertInt() ?? dto.TotalDocs;
        }

        return dto;
    }
}
