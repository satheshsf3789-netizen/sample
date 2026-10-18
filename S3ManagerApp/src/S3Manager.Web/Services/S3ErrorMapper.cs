using Amazon;
using Amazon.Runtime;
using Amazon.S3;

namespace S3Manager.Web.Services;

/// <summary>
/// Maps AWS S3 exceptions to user-friendly error messages.
/// </summary>
public interface IS3ErrorMapper
{
    (string Message, string? Hint) Map(AmazonServiceException ex);
}

public class S3ErrorMapper : IS3ErrorMapper
{
    public (string Message, string? Hint) Map(AmazonServiceException ex)
    {
        return ex switch
        {
            AmazonS3Exception { ErrorCode: "InvalidAccessKeyId" } =>
                ("The access key is not valid.", "Check for typos or truncation; keys are 20 characters (AKIA…)."),
            AmazonS3Exception { ErrorCode: "SignatureDoesNotMatch" } =>
                ("The secret key does not match the access key.", "Re-enter the secret key; check for missing characters or trailing spaces."),
            AmazonS3Exception { ErrorCode: "ExpiredToken" or "InvalidToken" } =>
                ("The session token is expired or invalid.", "Generate new temporary credentials."),
            AmazonS3Exception { ErrorCode: "AccessDenied" } =>
                ("Access denied for this action.", "The key's policy does not allow it. See Help for sample policies."),
            AmazonS3Exception { ErrorCode: "NoSuchBucket" } =>
                ("Bucket not found.", "Check the bucket name and region."),
            AmazonS3Exception { ErrorCode: "NoSuchKey" } =>
                ("Object not found.", "It may have been moved or deleted."),
            AmazonS3Exception { ErrorCode: "BucketNotEmpty" } =>
                ("Bucket is not empty.", "Use Empty bucket first (includes versions)."),
            AmazonS3Exception { ErrorCode: "BucketAlreadyExists" } =>
                ("That bucket name is taken globally.", "Choose a different name."),
            AmazonS3Exception { ErrorCode: "BucketAlreadyOwnedByYou" } =>
                ("You already own this bucket.", "Open it from the bucket list."),
            AmazonS3Exception
            {
                ErrorCode: "PermanentRedirect" or "AuthorizationHeaderMalformed" or "IllegalLocationConstraintException"
            } =>
                ("Wrong region for this bucket.", "Reconnect using the bucket's region."),
            AmazonS3Exception { ErrorCode: "RequestTimeTooSkewed" } =>
                ("Server clock is out of sync.", "Fix the system time (NTP)."),
            AmazonS3Exception { ErrorCode: "SlowDown" or "RequestLimitExceeded" } =>
                ("S3 is throttling requests.", "Wait a moment and retry."),
            _ => ("The S3 operation failed.", "See server logs for details.")
        };
    }
}
