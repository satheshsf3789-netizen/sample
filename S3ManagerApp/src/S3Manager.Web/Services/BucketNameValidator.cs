using System.Text.RegularExpressions;

namespace S3Manager.Web.Services;

/// <summary>
/// Implementation of bucket name validator using generated regex patterns.
/// </summary>
public sealed partial class BucketNameValidator : IBucketNameValidator
{
    [GeneratedRegex(@"^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$")]
    private static partial Regex Shape();

    [GeneratedRegex(@"^\d{1,3}(\.\d{1,3}){3}$")]
    private static partial Regex IpLike();

    public (bool Ok, string? Error) Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Bucket name is required.");

        if (name.Length is < 3 or > 63)
            return (false, "Name must be 3 to 63 characters.");

        if (!Shape().IsMatch(name))
            return (false, "Use lowercase letters, numbers, dots and hyphens; start and end with a letter or number.");

        if (name.Contains("..") || name.Contains(".-") || name.Contains("-."))
            return (false, "Dots and hyphens cannot be adjacent in that way.");

        if (IpLike().IsMatch(name))
            return (false, "Name must not look like an IP address.");

        if (name.StartsWith("xn--") || name.StartsWith("sthree-") || 
            name.EndsWith("-s3alias") || name.EndsWith("--ol-s3"))
            return (false, "Name uses a reserved prefix or suffix.");

        return (true, null);
    }
}
