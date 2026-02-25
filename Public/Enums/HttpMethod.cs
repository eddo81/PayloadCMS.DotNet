using Payload.CMS.Internal.Attributes;

namespace Payload.CMS.Public.Enums;

/// <summary>HTTP methods supported by the Payload CMS REST API.</summary>
public enum HttpMethod
{
    /// <summary>Retrieve a resource or list of resources.</summary>
    [StringValue("GET")]
    GET,

    /// <summary>Create a new resource.</summary>
    [StringValue("POST")]
    POST,

    /// <summary>Replace a resource entirely.</summary>
    [StringValue("PUT")]
    PUT,

    /// <summary>Partially update a resource.</summary>
    [StringValue("PATCH")]
    PATCH,

    /// <summary>Delete a resource.</summary>
    [StringValue("DELETE")]
    DELETE,
}
