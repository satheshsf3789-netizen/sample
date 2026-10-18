using S3Manager.Web.Services;

namespace S3Manager.Tests;

public class CsvExporterTests
{
    private readonly ICsvExporter _exporter = new CsvExporter();

    [Fact]
    public void Cell_NormalValue_QuotesAndEscapes()
    {
        var result = _exporter.Cell("hello");
        Assert.Equal("\"hello\"", result);
    }

    [Fact]
    public void Cell_ValueWithQuotes_EscapesQuotes()
    {
        var result = _exporter.Cell("hello\"world");
        Assert.Equal("\"hello\"\"world\"", result);
    }

    [Fact]
    public void Cell_FormulaInjection_PrependsSingleQuote()
    {
        var result = _exporter.Cell("=SUM(A1:A10)");
        Assert.StartsWith("\"'", result);
    }

    [Fact]
    public void Cell_Null_ReturnsEmptyString()
    {
        var result = _exporter.Cell(null);
        Assert.Equal("\"\"", result);
    }
}
