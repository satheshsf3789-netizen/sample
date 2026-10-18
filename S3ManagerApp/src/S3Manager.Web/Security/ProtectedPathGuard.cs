using Microsoft.Extensions.Options;
using S3Manager.Web.Models;

namespace S3Manager.Web.Security;

/// <summary>
/// Configuration for protected paths.
/// </summary>
public class ProtectedPathsOptions
{
    public List<string> ProtectedBuckets { get; set; } = new();
    public List<string> ProtectedPrefixes { get; set; } = new();
}

/// <summary>
/// Guards against mutations to protected paths and buckets.
/// </summary>
public interface IProtectedPathGuard
{
    void CheckBucket(string bucket);
    void CheckPrefix(string bucket, string prefix);
    void AllowUnprotect(ISession session);
    bool IsUnprotected(ISession session);
}

public class ProtectedPathGuard : IProtectedPathGuard
{
    private readonly ProtectedPathsOptions _options;
    private const string UnprotectKey = "unprotect.session";

    public ProtectedPathGuard(IOptions<ProtectedPathsOptions> options)
    {
        _options = options.Value;
    }

    public void CheckBucket(string bucket)
    {
        if (_options.ProtectedBuckets.Contains(bucket) || _options.ProtectedBuckets.Contains("*"))
        {
            throw new UnauthorizedAccessException($"Bucket '{bucket}' is protected.");
        }
    }

    public void CheckPrefix(string bucket, string prefix)
    {
        var fullPath = $"{bucket}/{prefix}";
        foreach (var protected_ in _options.ProtectedPrefixes)
        {
            if (MatchesPattern(fullPath, protected_))
            {
                throw new UnauthorizedAccessException($"Path '{fullPath}' is protected.");
            }
        }
    }

    public void AllowUnprotect(ISession session)
    {
        session.SetString(UnprotectKey, "true");
    }

    public bool IsUnprotected(ISession session)
    {
        return session.TryGetValue(UnprotectKey, out _);
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        if (pattern == "*")
            return true;

        pattern = pattern.Replace("*", ".*");
        return System.Text.RegularExpressions.Regex.IsMatch(path, $"^{pattern}$");
    }
}
