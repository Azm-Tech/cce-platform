using System.Globalization;
using System.Text;
using CsvHelper;

namespace CCE.Application.Reports;

public sealed class CsvStreamWriter : ICsvStreamWriter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public async Task WriteAsync<TRow>(
        Stream destination,
        System.Collections.Generic.IAsyncEnumerable<TRow> rows,
        CancellationToken ct)
    {
        await using var writer = new StreamWriter(destination, Utf8WithBom, leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, leaveOpen: true);
        csv.WriteHeader<TRow>();
        await csv.NextRecordAsync().ConfigureAwait(false);
        await foreach (var row in rows.WithCancellation(ct).ConfigureAwait(false))
        {
            csv.WriteRecord(row);
            await csv.NextRecordAsync().ConfigureAwait(false);
        }
    }
}
