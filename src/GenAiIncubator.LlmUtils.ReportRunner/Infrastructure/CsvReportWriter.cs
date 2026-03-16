using CsvHelper;
using System.Globalization;

namespace GenAiIncubator.LlmUtils.ReportRunner.Infrastructure;

public class CsvReportWriter : IReportWriter
{
    public async Task WriteAsync<T>(string path, IEnumerable<T> rows, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(rows, cancellationToken);
        await writer.FlushAsync();
        await stream.FlushAsync(cancellationToken);
    }
}


