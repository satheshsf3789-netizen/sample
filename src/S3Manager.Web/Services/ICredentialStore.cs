using S3Manager.Web.Models;

namespace S3Manager.Web.Services;

/// <summary>
/// Credential store that keeps S3 connection details encrypted in the session.
/// </summary>
public interface ICredentialStore
{
    void Save(ISession session, S3Connection connection);
    S3Connection? Get(ISession session);
    void Clear(ISession session);
}
