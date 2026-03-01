namespace PayloadCMS.DotNet.Internal.Contracts;

/// <summary>
/// Defines a credential that can apply authentication
/// to an outbound HTTP request's headers.
/// </summary>
internal interface IAuthCredential
{
    /// <summary>
    /// Applies authentication to the given headers object.
    /// 
    /// Implementations should add, update, or remove headers
    /// as required by their authentication method.
    /// </summary>
    /// <param name="headers">
    /// The mutable headers dictionary to modify.
    /// </param>
    void Apply(Dictionary<string, string> headers);
}
