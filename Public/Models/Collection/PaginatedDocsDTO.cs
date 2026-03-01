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
    public static PaginatedDocsDTO FromJson(Dictionary<string, object?> json)
    {
        var dto = new PaginatedDocsDTO();
        var data = json ?? new Dictionary<string, object?>();

        if (data.ContainsKey("docs") && data["docs"] is System.Collections.IList docList)
        {
            foreach (var item in docList)
            {
                if (item is Dictionary<string, object?> docItem)
                {
                    dto.Docs.Add(DocumentDTO.FromJson(docItem));
                }
            }
        }

        if (data.ContainsKey("hasNextPage") && data["hasNextPage"] is bool hasNextPageValue)
        {
            dto.HasNextPage = hasNextPageValue;
        }

        if (data.ContainsKey("hasPrevPage") && data["hasPrevPage"] is bool hasPrevPageValue)
        {
            dto.HasPrevPage = hasPrevPageValue;
        }

        if (data.ContainsKey("limit"))
        {
            dto.Limit = JsonParser.TryConvertInt(data["limit"]) ?? dto.Limit;
        }

        if (data.ContainsKey("totalDocs"))
        {
            dto.TotalDocs = JsonParser.TryConvertInt(data["totalDocs"]) ?? dto.TotalDocs;
        }

        if (data.ContainsKey("totalPages"))
        {
            dto.TotalPages = JsonParser.TryConvertInt(data["totalPages"]) ?? dto.TotalPages;
        }

        if (data.ContainsKey("page"))
        {
            dto.Page = JsonParser.TryConvertInt(data["page"]);
        }

        if (data.ContainsKey("nextPage"))
        {
            dto.NextPage = JsonParser.TryConvertInt(data["nextPage"]);
        }

        if (data.ContainsKey("prevPage"))
        {
            dto.PrevPage = JsonParser.TryConvertInt(data["prevPage"]);
        }

        return dto;
    }
}
