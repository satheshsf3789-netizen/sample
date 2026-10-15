namespace S3Manager.Web.Services;

/// <summary>
/// Activity log for tracking user actions in the session.
/// </summary>
public interface IActivityLog
{
    void Log(string action, string details);
    IReadOnlyList<(DateTime, string, string)> GetHistory();
}

public class SessionActivityLog : IActivityLog
{
    private const string LogKey = "activity.log";
    private readonly ISession _session;

    public SessionActivityLog(IHttpContextAccessor accessor)
    {
        _session = accessor.HttpContext?.Session ?? throw new InvalidOperationException("No session");
    }

    public void Log(string action, string details)
    {
        var entries = GetHistory().ToList();
        entries.Add((DateTime.UtcNow, action, details));
        if (entries.Count > 100)
            entries.RemoveAt(0);
    }

    public IReadOnlyList<(DateTime, string, string)> GetHistory()
    {
        if (_session.TryGetValue(LogKey, out var data))
        {
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(data);
                return System.Text.Json.JsonSerializer.Deserialize<List<(DateTime, string, string)>>(json) ?? new();
            }
            catch { }
        }
        return new List<(DateTime, string, string)>();
    }
}
