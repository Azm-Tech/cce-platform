using CCE.Infrastructure.Sanitization;

namespace CCE.Infrastructure.Tests.Sanitization;

public class HtmlSanitizerWrapperTests
{
    private readonly HtmlSanitizerWrapper _sut = new();

    [Fact]
    public void Strips_script_tags()
    {
        var input = "<p>safe</p><script>alert('xss')</script>";
        var output = _sut.Sanitize(input);
        output.Should().NotContain("script");
        output.Should().Contain("<p>safe</p>");
    }

    [Fact]
    public void Strips_javascript_href()
    {
        var input = "<a href=\"javascript:alert(1)\">click</a>";
        var output = _sut.Sanitize(input);
        output.Should().NotContain("javascript");
    }

    [Fact]
    public void Allows_https_href()
    {
        var input = "<a href=\"https://example.com\">click</a>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void Allows_basic_formatting_tags()
    {
        var input = "<p><strong>bold</strong> <em>italic</em></p><ul><li>item</li></ul>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("<strong>bold</strong>");
        output.Should().Contain("<em>italic</em>");
        output.Should().Contain("<ul><li>item</li></ul>");
    }

    [Fact]
    public void Empty_input_returns_empty_string()
    {
        _sut.Sanitize(string.Empty).Should().Be(string.Empty);
        _sut.Sanitize(null!).Should().Be(string.Empty);
    }

    [Fact]
    public void Preserves_arabic_text()
    {
        var input = "<p>مرحبا بالعالم</p>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("مرحبا بالعالم");
    }
}
