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

        dto.Json = json.ToDictionary();

        if (json.ContainsKey("id") && json["id"].TryConvertString() is string idValue)
        {
            dto.Id = idValue;
        }
        else if (json.ContainsKey("id") && json["id"]?.GetValueKind() == JsonValueKind.Number)
        {
            // Postgres/SQLite adapters return numeric ids — normalize to string.
            dto.Id = Convert.ToString(json["id"].ToClr(), CultureInfo.InvariantCulture)!;
        }

        if (json.ContainsKey("createdAt") && json["createdAt"].TryConvertString() is string createdAtString && createdAtString != "" && DateTime.TryParse(createdAtString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime createdAtDate))
        {
            dto.CreatedAt = createdAtDate;
        }

        if (json.ContainsKey("updatedAt") && json["updatedAt"].TryConvertString() is string updatedAtString && updatedAtString != "" && DateTime.TryParse(updatedAtString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime updatedAtDate))
        {
            dto.UpdatedAt = updatedAtDate;
        }

        return dto;
    }
}
