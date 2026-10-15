using S3Manager.Web.Models;

namespace S3Manager.Web.Security;

/// <summary>
/// Exception thrown when a mutation is attempted on a read-only connection.
/// </summary>
public class ReadOnlyException : Exception
{
    public ReadOnlyException() : base("This connection is read-only. Mutations are not allowed.")
    {
    }
}

/// <summary>
/// Guards against mutations when in read-only mode.
/// </summary>
public static class ReadOnlyGuard
{
    public static void EnsureWritable(S3Connection connection)
    {
        if (connection.ReadOnly)
        {
            throw new ReadOnlyException();
        }
    }
}
