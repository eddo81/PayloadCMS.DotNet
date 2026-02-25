namespace Payload.CMS.Internal.Contracts;

/// <summary>
/// Contract for a serializable query clause strategy.
///</summary>
internal interface IClause
{
    /// <summary>
    /// Serializes the clause into a Payload CMS query object.
    /// </summary>
    /// <returns>
    /// A nested object for query string encoding.
    ///</returns>
    Dictionary<string, object?> Build();
}
