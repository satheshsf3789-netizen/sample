using Amazon;
using Amazon.S3;
using S3Manager.Web.Services;

namespace S3Manager.Tests;

public class S3ErrorMapperTests
{
    private readonly IS3ErrorMapper _mapper = new S3ErrorMapper();

    [Fact]
    public void Map_InvalidAccessKeyId_ReturnsFriendlyMessage()
    {
        var ex = new AmazonS3Exception("test") { ErrorCode = "InvalidAccessKeyId" };
        var (msg, hint) = _mapper.Map(ex);
        Assert.Contains("access key is not valid", msg);
        Assert.NotNull(hint);
    }

    [Fact]
    public void Map_AccessDenied_ReturnsFriendlyMessage()
    {
        var ex = new AmazonS3Exception("test") { ErrorCode = "AccessDenied" };
        var (msg, hint) = _mapper.Map(ex);
        Assert.Contains("Access denied", msg);
    }

    [Fact]
    public void Map_NoSuchBucket_ReturnsFriendlyMessage()
    {
        var ex = new AmazonS3Exception("test") { ErrorCode = "NoSuchBucket" };
        var (msg, hint) = _mapper.Map(ex);
        Assert.Contains("Bucket not found", msg);
    }

    [Fact]
    public void Map_UnknownError_ReturnsGenericMessage()
    {
        var ex = new AmazonS3Exception("test") { ErrorCode = "UnknownCode" };
        var (msg, hint) = _mapper.Map(ex);
        Assert.Contains("failed", msg);
    }
}
