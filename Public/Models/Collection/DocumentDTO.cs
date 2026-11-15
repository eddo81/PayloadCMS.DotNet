using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PayloadCMS.DotNet.Internal.Utils;

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
    internal static DocumentDTO FromJson(JsonObject json)
    {
        var dto = new DocumentDTO();
        var data = json ?? new JsonObject();

        dto.Json = data.ToDictionary();

        if (data.ContainsKey("id") && data["id"].TryConvertString() is string idValue)
        {
            dto.Id = idValue;
        }
        else if (data.ContainsKey("id") && data["id"]?.GetValueKind() == JsonValueKind.Number)
        {
            // Postgres/SQLite adapters return numeric ids — normalize to string.
            dto.Id = Convert.ToString(data["id"].ToClr(), CultureInfo.InvariantCulture)!;
        }

        if (data.ContainsKey("createdAt") && data["createdAt"].TryConvertString() is string createdAtString && createdAtString != "" && DateTime.TryParse(createdAtString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdAtDate))
        {
            dto.CreatedAt = createdAtDate;
        }

        if (data.ContainsKey("updatedAt") && data["updatedAt"].TryConvertString() is string updatedAtString && updatedAtString != "" && DateTime.TryParse(updatedAtString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime updatedAtDate))
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
    internal static JsonObject ToJson(DocumentDTO dto)
    {
        var result = dto.Json.ToNode()!.AsObject();

        result["id"] = dto.Id;

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
