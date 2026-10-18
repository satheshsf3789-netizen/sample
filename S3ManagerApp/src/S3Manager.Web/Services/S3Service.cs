using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using S3Manager.Web.Models;
using S3Manager.Web.Security;
using BucketInfo = S3Manager.Web.Models.BucketInfo;

namespace S3Manager.Web.Services;

/// <summary>
/// Core S3 service implementation - simplified for core operations
/// </summary>
public class S3Service : IS3Service
{
    private readonly IS3ClientFactory _factory;
    private readonly IS3ErrorMapper _errorMapper;
    private readonly IProtectedPathGuard _pathGuard;
    private readonly ILogger<S3Service> _logger;

    public S3Service(
        IS3ClientFactory factory,
        IS3ErrorMapper errorMapper,
        IProtectedPathGuard pathGuard,
        ILogger<S3Service> logger)
    {
        _factory = factory;
        _errorMapper = errorMapper;
        _pathGuard = pathGuard;
        _logger = logger;
    }

    public async Task<ProbeReport> VerifyConnectionAsync(S3Connection conn, CancellationToken ct)
    {
        try
        {
            using var s3 = _factory.Create(conn);
            await s3.ListBucketsAsync(ct);
            return new ProbeReport { Success = true, Message = "Connection verified" };
        }
        catch (AmazonServiceException ex)
        {
            var (msg, hint) = _errorMapper.Map(ex);
            return new ProbeReport { Success = false, Message = msg, Hint = hint, ErrorCode = ex.ErrorCode };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection verification failed");
            return new ProbeReport { Success = false, Message = "Connection failed" };
        }
    }

    public async Task<ProbeReport> CapabilityCheckAsync(S3Connection conn, string bucket, CancellationToken ct)
    {
        var report = new ProbeReport { Success = true, Message = "Capabilities checked" };
        try
        {
            using var s3 = _factory.Create(conn);
            var req = new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 };
            await s3.ListObjectsV2Async(req, ct);
            report.Capabilities["read"] = true;
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capability check failed");
            report.Success = false;
            return report;
        }
    }

    public async Task<IReadOnlyList<BucketInfo>> ListBucketsAsync(S3Connection conn, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var result = await s3.ListBucketsAsync(ct);
        return result.Buckets?.Select(b => new BucketInfo 
        { 
            Name = b.BucketName, 
            CreationDate = b.CreationDate 
        }).ToList() ?? new List<BucketInfo>();
    }

    public async Task CreateBucketAsync(S3Connection conn, string name, string region, bool versioning, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn, region);
        var req = new PutBucketRequest { BucketName = name };
        if (region != "us-east-1")
            req.BucketRegionName = region;
        await s3.PutBucketAsync(req, ct);

        if (versioning)
        {
            await s3.PutBucketVersioningAsync(new PutBucketVersioningRequest
            {
                BucketName = name,
                VersioningConfig = new S3BucketVersioningConfig { Status = VersionStatus.Enabled }
            }, ct);
        }
    }

    public async Task<BucketDetails> GetBucketDetailsAsync(S3Connection conn, string bucket, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var versioning = await s3.GetBucketVersioningAsync(bucket, ct);
        var count = await CountObjectsAsync(conn, bucket, null, 1000, ct);

        return new BucketDetails
        {
            Name = bucket,
            VersioningEnabled = versioning.VersioningConfig?.Status == VersionStatus.Enabled,
            VersioningSuspended = versioning.VersioningConfig?.Status == VersionStatus.Suspended,
            ObjectCount = count.Count
        };
    }

    public async Task<CountSummary> CountObjectsAsync(S3Connection conn, string bucket, string? prefix, int cap, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix, MaxKeys = cap };
        var result = await s3.ListObjectsV2Async(req, ct);
        return new CountSummary
        {
            Count = result.S3Objects?.Count ?? 0,
            Capped = result.IsTruncated == true
        };
    }

    public async Task DeleteBucketAsync(S3Connection conn, string bucket, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        await s3.DeleteBucketAsync(bucket, ct);
    }

    public async Task<DeleteSummary> EmptyBucketAsync(S3Connection conn, string bucket, IProgress<int>? progress, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);

        int deleted = 0;
        var failures = new List<(string, string)>();
        string? token = null;

        do
        {
            var listReq = new ListObjectsV2Request { BucketName = bucket, ContinuationToken = token, MaxKeys = 1000 };
            var listRes = await s3.ListObjectsV2Async(listReq, ct);
            
            if ((listRes.S3Objects?.Count ?? 0) > 0)
            {
                var delReq = new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = listRes.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                };
                try
                {
                    var res = await s3.DeleteObjectsAsync(delReq, ct);
                    deleted += res.DeletedObjects?.Count ?? 0;
                    progress?.Report(deleted);
                }
                catch (DeleteObjectsException ex)
                {
                    deleted += ex.Response.DeletedObjects?.Count ?? 0;
                    failures.AddRange(ex.Response.DeleteErrors.Select(e => (e.Key, e.Code)));
                }
            }

            token = listRes.IsTruncated == true ? listRes.NextContinuationToken : null;
        } while (token != null);

        return new DeleteSummary { DeletedCount = deleted, Failures = failures };
    }

    public async Task SetVersioningAsync(S3Connection conn, string bucket, bool enabled, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        await s3.PutBucketVersioningAsync(new PutBucketVersioningRequest
        {
            BucketName = bucket,
            VersioningConfig = new S3BucketVersioningConfig
            {
                Status = enabled ? VersionStatus.Enabled : VersionStatus.Suspended
            }
        }, ct);
    }

    public async Task<ObjectPage> ListObjectsAsync(S3Connection conn, string bucket, string prefix, string? token, int pageSize, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = prefix,
            Delimiter = "/",
            MaxKeys = pageSize,
            ContinuationToken = token
        };

        var result = await s3.ListObjectsV2Async(req, ct);
        var folders = result.CommonPrefixes ?? new List<string>();
        var objects = result.S3Objects?
            .Where(o => o.Key != prefix)
            .Select(o => new ObjectItem
            {
                Key = o.Key,
                FileName = Path.GetFileName(o.Key.TrimEnd('/')),
                SizeBytes = o.Size,
                LastModifiedUtc = o.LastModified,
                IsFolder = false
            })
            .ToList() ?? new();

        return new ObjectPage
        {
            Bucket = bucket,
            Prefix = prefix,
            Objects = objects,
            Folders = folders,
            NextToken = result.IsTruncated == true ? result.NextContinuationToken : null,
            HasMore = result.IsTruncated == true
        };
    }

    public async Task<IReadOnlyList<ObjectItem>> ListRecentAsync(S3Connection conn, string bucket, string prefix, DateTime sinceUtc, int cap, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix, MaxKeys = cap };
        var result = await s3.ListObjectsV2Async(req, ct);

        return result.S3Objects?
            .Where(o => o.LastModified > sinceUtc)
            .OrderByDescending(o => o.LastModified)
            .Select(o => new ObjectItem
            {
                Key = o.Key,
                FileName = Path.GetFileName(o.Key),
                SizeBytes = o.Size ?? 0,
                LastModifiedUtc = o.LastModified ?? DateTime.UtcNow
            })
            .ToList() ?? new List<ObjectItem>();
    }

    public async Task<ObjectDetails> GetObjectDetailsAsync(S3Connection conn, string bucket, string key, string? versionId, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new GetObjectMetadataRequest { BucketName = bucket, Key = key, VersionId = versionId };
        var metadata = await s3.GetObjectMetadataAsync(req, ct);

        return new ObjectDetails
        {
            Key = key,
            SizeBytes = metadata.ContentLength,
            LastModifiedUtc = metadata.LastModified,
            ContentType = metadata.ContentType
        };
    }

    public async Task<GetObjectResponse> OpenObjectAsync(S3Connection conn, string bucket, string key, string? versionId, CancellationToken ct)
    {
        var s3 = _factory.Create(conn);
        var req = new GetObjectRequest { BucketName = bucket, Key = key, VersionId = versionId };
        return await s3.GetObjectAsync(req, ct);
    }

    public async Task<string> CreatePresignedUrlAsync(S3Connection conn, string bucket, string key, TimeSpan expiry, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };
        return s3.GetPreSignedURL(req);
    }

    public async Task UploadAsync(S3Connection conn, string bucket, string key, Stream data, UploadOptions opts, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        using var transfer = new TransferUtility(s3);
        await transfer.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = data,
            ContentType = opts.ContentType,
            StorageClass = opts.StorageClass
        }, ct);
    }

    public async Task CreateFolderAsync(S3Connection conn, string bucket, string prefix, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        var folderKey = prefix.EndsWith('/') ? prefix : prefix + '/';
        await s3.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = folderKey, ContentBody = "" }, ct);
    }

    public async Task CopyAsync(S3Connection conn, string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        await s3.CopyObjectAsync(srcBucket, srcKey, dstBucket, dstKey, ct);
    }

    public async Task MoveAsync(S3Connection conn, string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        await CopyAsync(conn, srcBucket, srcKey, dstBucket, dstKey, ct);
        using var s3 = _factory.Create(conn);
        await s3.DeleteObjectAsync(srcBucket, srcKey, ct);
    }

    public async Task UpdateMetadataAsync(S3Connection conn, string bucket, string key, Dictionary<string, string> metadata, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        var copyReq = new CopyObjectRequest
        {
            SourceBucket = bucket,
            SourceKey = key,
            DestinationBucket = bucket,
            DestinationKey = key,
            MetadataDirective = S3MetadataDirective.REPLACE
        };
        foreach (var kvp in metadata)
            copyReq.Metadata[kvp.Key] = kvp.Value;
        await s3.CopyObjectAsync(copyReq, ct);
    }

    public async Task<DeleteSummary> DeleteObjectsAsync(S3Connection conn, string bucket, IEnumerable<string> keys, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);

        int deleted = 0;
        var failures = new List<(string, string)>();

        foreach (var chunk in keys.Chunk(1000))
        {
            try
            {
                var res = await s3.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = bucket,
                    Objects = chunk.Select(k => new KeyVersion { Key = k }).ToList()
                }, ct);
                deleted += res.DeletedObjects?.Count ?? 0;
            }
            catch (DeleteObjectsException ex)
            {
                deleted += ex.Response.DeletedObjects?.Count ?? 0;
                failures.AddRange(ex.Response.DeleteErrors.Select(e => (e.Key, e.Code)));
            }
        }

        return new DeleteSummary { DeletedCount = deleted, Failures = failures };
    }

    public async Task<DryRunResult> DryRunDeletePrefixAsync(S3Connection conn, string bucket, string prefix, int sample, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        
        int count = 0;
        var sampleKeys = new List<string>();
        string? token = null;

        do
        {
            var req = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix, MaxKeys = 1000, ContinuationToken = token };
            var result = await s3.ListObjectsV2Async(req, ct);

            foreach (var obj in result.S3Objects ?? new List<S3Object>())
            {
                count++;
                if (sampleKeys.Count < sample)
                    sampleKeys.Add(obj.Key);
            }

            token = result.IsTruncated == true ? result.NextContinuationToken : null;
        } while (token != null && count < sample * 2);

        return new DryRunResult { TotalCount = count, Sample = sampleKeys, WasCapped = count >= sample * 2 };
    }

    public async Task<DeleteSummary> DeletePrefixAsync(S3Connection conn, string bucket, string prefix, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);

        int deleted = 0;
        var failures = new List<(string, string)>();
        string? token = null;

        do
        {
            var listReq = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix, MaxKeys = 1000, ContinuationToken = token };
            var listRes = await s3.ListObjectsV2Async(listReq, ct);

            if ((listRes.S3Objects?.Count ?? 0) > 0)
            {
                try
                {
                    var res = await s3.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = bucket,
                        Objects = listRes.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    }, ct);
                    deleted += res.DeletedObjects?.Count ?? 0;
                }
                catch (DeleteObjectsException ex)
                {
                    deleted += ex.Response.DeletedObjects?.Count ?? 0;
                    failures.AddRange(ex.Response.DeleteErrors.Select(e => (e.Key, e.Code)));
                }
            }

            token = listRes.IsTruncated == true ? listRes.NextContinuationToken : null;
        } while (token != null);

        return new DeleteSummary { DeletedCount = deleted, Failures = failures };
    }

    public async Task<IReadOnlyList<VersionItem>> ListVersionsAsync(S3Connection conn, string bucket, string? prefix, CancellationToken ct)
    {
        using var s3 = _factory.Create(conn);
        var req = new ListVersionsRequest { BucketName = bucket, Prefix = prefix };
        var result = await s3.ListVersionsAsync(req, ct);

        var versions = new List<VersionItem>();
        foreach (var obj in result.Versions ?? new List<S3ObjectVersion>())
        {
            versions.Add(new VersionItem
            {
                Key = obj.Key,
                VersionId = obj.VersionId,
                IsDeleteMarker = obj.IsDeleteMarker,
                LastModifiedUtc = obj.LastModified,
                SizeBytes = obj.Size
            });
        }

        return versions;
    }

    public async Task DeleteVersionAsync(S3Connection conn, string bucket, string key, string versionId, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        await s3.DeleteObjectAsync(bucket, key, versionId, ct);
    }

    public async Task RestoreVersionAsync(S3Connection conn, string bucket, string key, string versionId, CancellationToken ct)
    {
        ReadOnlyGuard.EnsureWritable(conn);
        using var s3 = _factory.Create(conn);
        var getReq = new GetObjectRequest { BucketName = bucket, Key = key, VersionId = versionId };
        var getRes = await s3.GetObjectAsync(getReq, ct);

        using var transfer = new TransferUtility(s3);
        await transfer.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = getRes.ResponseStream
        }, ct);
    }
}
