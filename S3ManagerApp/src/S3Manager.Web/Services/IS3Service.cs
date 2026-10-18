using S3Manager.Web.Models;

namespace S3Manager.Web.Services;

/// <summary>
/// Main S3 service interface - all S3 operations go through this.
/// </summary>
public interface IS3Service
{
    Task<ProbeReport> VerifyConnectionAsync(S3Connection conn, CancellationToken ct);
    Task<ProbeReport> CapabilityCheckAsync(S3Connection conn, string bucket, CancellationToken ct);
    Task<IReadOnlyList<BucketInfo>> ListBucketsAsync(S3Connection conn, CancellationToken ct);
    Task CreateBucketAsync(S3Connection conn, string name, string region, bool versioning, CancellationToken ct);
    Task<BucketDetails> GetBucketDetailsAsync(S3Connection conn, string bucket, CancellationToken ct);
    Task<CountSummary> CountObjectsAsync(S3Connection conn, string bucket, string? prefix, int cap, CancellationToken ct);
    Task DeleteBucketAsync(S3Connection conn, string bucket, CancellationToken ct);
    Task<DeleteSummary> EmptyBucketAsync(S3Connection conn, string bucket, IProgress<int>? progress, CancellationToken ct);
    Task SetVersioningAsync(S3Connection conn, string bucket, bool enabled, CancellationToken ct);
    Task<ObjectPage> ListObjectsAsync(S3Connection conn, string bucket, string prefix, string? token, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<ObjectItem>> ListRecentAsync(S3Connection conn, string bucket, string prefix, DateTime sinceUtc, int cap, CancellationToken ct);
    Task<ObjectDetails> GetObjectDetailsAsync(S3Connection conn, string bucket, string key, string? versionId, CancellationToken ct);
    Task<Amazon.S3.Model.GetObjectResponse> OpenObjectAsync(S3Connection conn, string bucket, string key, string? versionId, CancellationToken ct);
    Task<string> CreatePresignedUrlAsync(S3Connection conn, string bucket, string key, TimeSpan expiry, CancellationToken ct);
    Task UploadAsync(S3Connection conn, string bucket, string key, Stream data, UploadOptions opts, CancellationToken ct);
    Task CreateFolderAsync(S3Connection conn, string bucket, string prefix, CancellationToken ct);
    Task CopyAsync(S3Connection conn, string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken ct);
    Task MoveAsync(S3Connection conn, string srcBucket, string srcKey, string dstBucket, string dstKey, CancellationToken ct);
    Task UpdateMetadataAsync(S3Connection conn, string bucket, string key, Dictionary<string, string> metadata, CancellationToken ct);
    Task<DeleteSummary> DeleteObjectsAsync(S3Connection conn, string bucket, IEnumerable<string> keys, CancellationToken ct);
    Task<DryRunResult> DryRunDeletePrefixAsync(S3Connection conn, string bucket, string prefix, int sample, CancellationToken ct);
    Task<DeleteSummary> DeletePrefixAsync(S3Connection conn, string bucket, string prefix, CancellationToken ct);
    Task<IReadOnlyList<VersionItem>> ListVersionsAsync(S3Connection conn, string bucket, string? prefix, CancellationToken ct);
    Task DeleteVersionAsync(S3Connection conn, string bucket, string key, string versionId, CancellationToken ct);
    Task RestoreVersionAsync(S3Connection conn, string bucket, string key, string versionId, CancellationToken ct);
}
