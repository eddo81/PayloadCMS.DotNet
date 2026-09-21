using PayloadCMS.DotNet.Upload;

namespace Payload.CMS.Tests;

public class FileUploadTests
{
    [Fact]
    public void FileUploadShouldDefaultTheMimeTypeWhenNoneIsSupplied()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png");

        Assert.Equal("application/octet-stream", file.MimeType);
    }

    [Fact]
    public void FileUploadShouldKeepTheSuppliedMimeType()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png", "image/png");

        Assert.Equal("image/png", file.MimeType);
    }

    [Fact]
    public void FileUploadShouldDefaultTheMimeTypeWhenNullIsSuppliedExplicitly()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png", null);

        Assert.Equal("application/octet-stream", file.MimeType);
    }

    [Fact]
    public void FileUploadShouldKeepTheMimeTypeWhenCopiedWithOtherChanges()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png", "image/png");

        var renamed = file with { FileName = "other.png" };

        Assert.Equal("image/png", renamed.MimeType);
        Assert.Equal("other.png", renamed.FileName);
    }

    [Fact]
    public void FileUploadShouldAllowTheMimeTypeToBeChangedByCopy()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png", "image/png");

        var retyped = file with { MimeType = "image/webp" };

        Assert.Equal("image/webp", retyped.MimeType);
    }

    [Fact]
    public void FileUploadShouldDefaultTheMimeTypeWhenClearedByCopy()
    {
        var file = new FileUpload(new byte[] { 1, 2, 3 }, "photo.png", "image/png");

        // The property is non-nullable, so clearing it needs a deliberate suppression. The
        // invariant must survive that — a FileUpload never carries a null MIME type.
        var cleared = file with { MimeType = null! };

        Assert.Equal("application/octet-stream", cleared.MimeType);
    }
}
