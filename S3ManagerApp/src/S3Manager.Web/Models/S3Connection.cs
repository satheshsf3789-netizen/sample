namespace S3Manager.Web.Models;

/// <summary>
/// Represents an S3 connection with credentials and configuration.
/// Never logged directly (ToString is redacted).
/// </summary>
public sealed record S3Connection(
    string AccessKey, 
    string SecretKey, 
    string? SessionToken,
    string Region, 
    string? ServiceUrl, 
    bool ReadOnly, 
    string? DefaultBucket)
{
    /// <summary>
    /// Show only the last 4 characters of the access key for UI display.
    /// </summary>
    public string AccessKeyHint => AccessKey.Length >= 4 ? AccessKey[^4..] : "****";

    /// <summary>
    /// Redacts credentials to prevent accidental logging.
    /// </summary>
    public override string ToString() => "S3Connection(redacted)";
}
