namespace S3Manager.Web.Models;

/// <summary>
/// Connect form - do not retain SecretKey after failed attempts
/// </summary>
public class ConnectViewModel
{
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string? SessionToken { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
    public bool ReadOnly { get; set; }
    public string? DefaultBucket { get; set; }
    public bool AllowCustomEndpoint { get; set; } = true;
}

/// <summary>
/// Bucket information from ListBuckets response
/// </summary>
public class BucketInfo
{
    public string Name { get; set; } = "";
    public DateTime? CreationDate { get; set; }
}

/// <summary>
/// Details about a specific bucket
/// </summary>
public class BucketDetails
{
    public string Name { get; set; } = "";
    public bool VersioningEnabled { get; set; }
    public bool VersioningSuspended { get; set; }
    public long? ObjectCount { get; set; }
    public long? TotalSizeBytes { get; set; }
}

/// <summary>
/// Single object item in a listing
/// </summary>
public class ObjectItem
{
    public string Key { get; set; } = "";
    public string? FileName { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public string? StorageClass { get; set; }
    public string? ETag { get; set; }
    public bool IsFolder { get; set; }
}

/// <summary>
/// Paginated object listing
/// </summary>
public class ObjectPage
{
    public string Bucket { get; set; } = "";
    public string Prefix { get; set; } = "";
    public List<ObjectItem> Objects { get; set; } = new();
    public List<string> Folders { get; set; } = new();
    public string? NextToken { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// Object details for the detail view
/// </summary>
public class ObjectDetails
{
    public string Key { get; set; } = "";
    public long? SizeBytes { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public string? ContentType { get; set; }
    public string? StorageClass { get; set; }
    public string? ETag { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Version item for versioned bucket listing
/// </summary>
public class VersionItem
{
    public string Key { get; set; } = "";
    public string VersionId { get; set; } = "";
    public bool? IsDeleteMarker { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public long? SizeBytes { get; set; }
    public string? StorageClass { get; set; }
}

/// <summary>
/// Result of a delete operation
/// </summary>
public class DeleteSummary
{
    public int DeletedCount { get; set; }
    public List<(string Key, string Code)> Failures { get; set; } = new();
}

/// <summary>
/// Summary of object count (used before operations)
/// </summary>
public class CountSummary
{
    public int Count { get; set; }
    public bool Capped { get; set; }
}

/// <summary>
/// Dry-run result for prefix delete (shows what would be deleted)
/// </summary>
public class DryRunResult
{
    public int TotalCount { get; set; }
    public List<string> Sample { get; set; } = new();
    public bool WasCapped { get; set; }
}

/// <summary>
/// Verification result from connection probe
/// </summary>
public class ProbeReport
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Hint { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, bool> Capabilities { get; set; } = new();
}

/// <summary>
/// Options for upload
/// </summary>
public class UploadOptions
{
    public bool Overwrite { get; set; }
    public string? ContentType { get; set; }
    public string? StorageClass { get; set; }
    public bool UseSse { get; set; }
}

/// <summary>
/// Operation result (success/error)
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Hint { get; set; }
}

/// <summary>
/// View model for object listing page
/// </summary>
public class ObjectListViewModel
{
    public S3Connection? Connection { get; set; }
    public ObjectPage Page { get; set; } = new();
    public string? Message { get; set; }
    public bool ReadOnly { get; set; }
}
