namespace S3Manager.Web.Services;

/// <summary>
/// CSV exporter with formula injection protection.
/// </summary>
public interface ICsvExporter
{
    string Cell(string? value);
    Task WriteListingAsync(Stream stream, IAsyncEnumerable<(string Key, long Size, DateTime Modified, string? StorageClass)> items);
}

public class CsvExporter : ICsvExporter
{
    public string Cell(string? value)
    {
        value ??= "";
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public async Task WriteListingAsync(Stream stream, IAsyncEnumerable<(string Key, long Size, DateTime Modified, string? StorageClass)> items)
    {
        using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
        await writer.WriteLineAsync("Key,FileName,SizeBytes,LastModifiedUtc,StorageClass,ETag");

        await foreach (var (key, size, modified, storageClass) in items)
        {
            var fileName = Path.GetFileName(key);
            var modifiedStr = modified.ToUniversalTime().ToString("O");
            var line = $"{Cell(key)},{Cell(fileName)},{Cell(size.ToString())},{Cell(modifiedStr)},{Cell(storageClass)}";
            await writer.WriteLineAsync(line);
        }
    }
}
