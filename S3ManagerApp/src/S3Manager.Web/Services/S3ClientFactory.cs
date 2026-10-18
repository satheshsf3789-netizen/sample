using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using S3Manager.Web.Models;

namespace S3Manager.Web.Services;

/// <summary>
/// Implementation of IS3ClientFactory that creates configured S3 clients.
/// </summary>
public sealed class S3ClientFactory : IS3ClientFactory
{
    public IAmazonS3 Create(S3Connection c, string? regionOverride = null)
    {
        AWSCredentials creds = string.IsNullOrWhiteSpace(c.SessionToken)
            ? new BasicAWSCredentials(c.AccessKey, c.SecretKey)
            : new SessionAWSCredentials(c.AccessKey, c.SecretKey, c.SessionToken);

        var cfg = new AmazonS3Config { Timeout = TimeSpan.FromSeconds(30), MaxErrorRetry = 2 };

        if (!string.IsNullOrWhiteSpace(c.ServiceUrl))
        {
            cfg.ServiceURL = c.ServiceUrl;
            cfg.ForcePathStyle = true;
        }
        else
        {
            cfg.RegionEndpoint = RegionEndpoint.GetBySystemName(regionOverride ?? c.Region);
        }

        return new AmazonS3Client(creds, cfg);
    }
}
