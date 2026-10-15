namespace S3Manager.Web.Services;

/// <summary>
/// Validates S3 bucket names according to AWS rules.
/// </summary>
public interface IBucketNameValidator
{
    (bool Ok, string? Error) Validate(string? name);
}
