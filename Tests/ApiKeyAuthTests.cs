using PayloadCMS.DotNet.Config;

namespace Payload.CMS.Tests;

public class ApiKeyAuthTests
{
    [Fact]
    public void ApiKeyAuthShouldSetAuthorizationHeaderInPayloadCmsFormat()
    {
        var auth = new ApiKeyAuth("users", "abc123");
        var headers = new Dictionary<string, string>();

        auth.Apply(headers);

        Assert.Equal("users API-Key abc123", headers["Authorization"]);
    }

    [Fact]
    public void ApiKeyAuthShouldOverwriteAnExistingAuthorizationHeader()
    {
        var auth = new ApiKeyAuth("users", "abc123");
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer old-token"
        };

        auth.Apply(headers);

        Assert.Equal("users API-Key abc123", headers["Authorization"]);
    }

    [Fact]
    public void ApiKeyAuthShouldPreserveOtherHeaders()
    {
        var auth = new ApiKeyAuth("users", "abc123");
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json",
            ["X-Custom"] = "value"
        };

        auth.Apply(headers);

        Assert.Equal("application/json", headers["Accept"]);
        Assert.Equal("value", headers["X-Custom"]);
        Assert.Equal("users API-Key abc123", headers["Authorization"]);
    }

    [Fact]
    public void ApiKeyAuthShouldUseCollectionSlugAndKeyValuesAsProvided()
    {
        var auth = new ApiKeyAuth("admin-users", "key-with-dashes-123");
        var headers = new Dictionary<string, string>();

        auth.Apply(headers);

        Assert.Equal("admin-users API-Key key-with-dashes-123", headers["Authorization"]);
    }
}
