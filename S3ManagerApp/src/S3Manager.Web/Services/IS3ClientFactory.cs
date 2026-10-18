using Amazon.S3;
using S3Manager.Web.Models;

namespace S3Manager.Web.Services;

/// <summary>
/// Factory for creating AWS S3 clients from connection credentials.
/// </summary>
public interface IS3ClientFactory
{
    IAmazonS3 Create(S3Connection connection, string? regionOverride = null);
}
