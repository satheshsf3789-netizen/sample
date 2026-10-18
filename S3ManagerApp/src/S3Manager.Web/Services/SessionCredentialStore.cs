using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using S3Manager.Web.Models;

namespace S3Manager.Web.Services;

/// <summary>
/// Implementation of ICredentialStore that uses Data Protection API to encrypt credentials in session.
/// </summary>
public sealed class SessionCredentialStore : ICredentialStore
{
    private const string SessionKey = "s3.connection";
    private readonly IDataProtector _protector;

    public SessionCredentialStore(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("S3Manager.Credentials.v1");
    }

    public void Save(ISession session, S3Connection connection)
    {
        var json = JsonSerializer.Serialize(connection);
        var encrypted = _protector.Protect(json);
        session.Set(SessionKey, Encoding.UTF8.GetBytes(encrypted));
    }

    public S3Connection? Get(ISession session)
    {
        if (!session.TryGetValue(SessionKey, out var bytes))
            return null;

        try
        {
            var encrypted = Encoding.UTF8.GetString(bytes);
            var json = _protector.Unprotect(encrypted);
            return JsonSerializer.Deserialize<S3Connection>(json);
        }
        catch
        {
            // Tampered/expired key ring: treat as disconnected
            session.Remove(SessionKey);
            return null;
        }
    }

    public void Clear(ISession session) => session.Clear();
}
