using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;

namespace PayloadCMS.DotNet.Models.Collection;

/// <summary>
/// Represents a paginated collection of Payload CMS documents.
/// </summary>
public sealed class PaginatedDocsDTO
{
    /// <summary>The documents in the current page.</summary>
    public List<DocumentDTO> Docs { get; set; } = new();
    /// <summary>Whether a next page exists.</summary>
    public bool HasNextPage { get; set; } = false;
    /// <summary>Whether a previous page exists.</summary>
    public bool HasPrevPage { get; set; } = false;
    /// <summary>The maximum number of documents per page.</summary>
    public int Limit { get; set; } = 10;
    /// <summary>The total number of documents matching the query.</summary>
    public int TotalDocs { get; set; } = 0;
    /// <summary>The total number of pages.</summary>
    public int TotalPages { get; set; } = 1;
    /// <summary>The current page number, if paginated.</summary>
    public int? Page { get; set; } = null;
    /// <summary>The next page number, if one exists.</summary>
    public int? NextPage { get; set; } = null;
    /// <summary>The previous page number, if one exists.</summary>
    public int? PrevPage { get; set; } = null;

    /// <summary>
    /// Maps a paginated JSON response into a <see cref="PaginatedDocsDTO"/>.
    /// </summary>
    /// <param name="json">The raw JSON from a Payload CMS endpoint.</param>
    /// <returns>A populated instance.</returns>
    internal static PaginatedDocsDTO FromJson(JsonObject json)
    {
        var dto = new PaginatedDocsDTO();

        if (json.ContainsKey("docs") && json["docs"] is JsonArray docList)
        {
            foreach (var item in docList)
            {
                if (item is JsonObject docItem)
                {
                    dto.Docs.Add(DocumentDTO.FromJson(docItem));
                }
            }
        }

        if (json.ContainsKey("hasNextPage") && json["hasNextPage"].TryConvertBool() is bool hasNextPageValue)
        {
            dto.HasNextPage = hasNextPageValue;
        }

        if (json.ContainsKey("hasPrevPage") && json["hasPrevPage"].TryConvertBool() is bool hasPrevPageValue)
        {
            dto.HasPrevPage = hasPrevPageValue;
        }

        if (json.ContainsKey("limit"))
        {
            dto.Limit = json["limit"].TryConvertInt() ?? dto.Limit;
        }

        if (json.ContainsKey("totalDocs"))
        {
            dto.TotalDocs = json["totalDocs"].TryConvertInt() ?? dto.TotalDocs;
        }

        if (json.ContainsKey("totalPages"))
        {
            dto.TotalPages = json["totalPages"].TryConvertInt() ?? dto.TotalPages;
        }

        if (json.ContainsKey("page"))
        {
            dto.Page = json["page"].TryConvertInt();
        }

        if (json.ContainsKey("nextPage"))
        {
            dto.NextPage = json["nextPage"].TryConvertInt();
        }

        if (json.ContainsKey("prevPage"))
        {
            dto.PrevPage = json["prevPage"].TryConvertInt();
        }

        return dto;
    }
}
