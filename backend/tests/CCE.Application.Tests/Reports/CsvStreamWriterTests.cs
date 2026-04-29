using System.Text;
using CCE.Application.Reports;

namespace CCE.Application.Tests.Reports;

public class CsvStreamWriterTests
{
    private sealed class TestRow
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Fact]
    public async Task Writes_header_and_rows_with_utf8_bom()
    {
        var sut = new CsvStreamWriter();
        using var output = new MemoryStream();

        var rows = ToAsyncEnumerable(new[]
        {
            new TestRow { Name = "Alice", Age = 30 },
            new TestRow { Name = "Bob", Age = 25 },
        });

        await sut.WriteAsync(output, rows, CancellationToken.None);

        var bytes = output.ToArray();
        bytes.Take(3).Should().Equal(new byte[] { 0xEF, 0xBB, 0xBF }, "UTF-8 BOM");
        var text = Encoding.UTF8.GetString(bytes);
        text.Should().Contain("Name,Age");
        text.Should().Contain("Alice,30");
        text.Should().Contain("Bob,25");
    }

    private static async System.Collections.Generic.IAsyncEnumerable<T> ToAsyncEnumerable<T>(System.Collections.Generic.IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
