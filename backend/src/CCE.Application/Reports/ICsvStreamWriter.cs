namespace CCE.Application.Reports;

/// <summary>
/// Writes an <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> as CSV (UTF-8 with BOM)
/// to the given <see cref="Stream"/>. Header row derived from the row type's public properties
/// via CsvHelper conventions.
/// </summary>
public interface ICsvStreamWriter
{
    /// <summary>
    /// Streams <paramref name="rows"/> as CSV records to <paramref name="destination"/>.
    /// </summary>
    /// <typeparam name="TRow">The row type whose public properties become CSV columns.</typeparam>
    Task WriteAsync<TRow>(
        Stream destination,
        System.Collections.Generic.IAsyncEnumerable<TRow> rows,
        CancellationToken ct);
}
