using CKY.MultiAgentFramework.Core.Abstractions;
using CKY.MultiAgentFramework.Infrastructure.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CKY.MAF.IntegrationTests.RAG;

/// <summary>
/// 文档解析器集成测试 — 验证解析器与工厂协同工作
/// </summary>
public class DocumentParserIntegrationTests : IDisposable
{
    private readonly DocumentParserFactory _factory;
    private readonly string _tempDir;

    public DocumentParserIntegrationTests()
    {
        var parsers = new IDocumentParser[]
        {
            new TxtDocumentParser(new Mock<ILogger<TxtDocumentParser>>().Object),
            new MarkdownDocumentParser(new Mock<ILogger<MarkdownDocumentParser>>().Object),
            new HtmlDocumentParser(new Mock<ILogger<HtmlDocumentParser>>().Object),
            new CsvDocumentParser(new Mock<ILogger<CsvDocumentParser>>().Object)
        };

        _factory = new DocumentParserFactory(
            parsers, new Mock<ILogger<DocumentParserFactory>>().Object);

        _tempDir = Path.Combine(Path.GetTempPath(), $"maf_parser_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Factory_SupportedExtensions_Returns4Types()
    {
        _factory.SupportedExtensions.Should().HaveCount(4);
        _factory.SupportedExtensions.Should().Contain(new[] { ".txt", ".md", ".html", ".csv" });
    }

    [Fact]
    public void Factory_IsSupported_ReturnsTrueForKnownTypes()
    {
        _factory.IsSupported("document.txt").Should().BeTrue();
        _factory.IsSupported("readme.md").Should().BeTrue();
        _factory.IsSupported("page.html").Should().BeTrue();
        _factory.IsSupported("page.htm").Should().BeTrue();
        _factory.IsSupported("data.csv").Should().BeTrue();
    }

    [Fact]
    public void Factory_IsSupported_ReturnsFalseForUnknownTypes()
    {
        _factory.IsSupported("document.pdf").Should().BeFalse();
        _factory.IsSupported("spreadsheet.xlsx").Should().BeFalse();
        _factory.IsSupported("noextension").Should().BeFalse();
    }

    [Fact]
    public void Factory_GetParser_ThrowsForUnsupportedType()
    {
        var act = () => _factory.GetParser("document.pdf");
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public async Task TxtParser_ParsesRealFile()
    {
        var filePath = CreateTempFile("test.txt", "Hello, world!\nSecond line.");

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("Hello, world!");
        result.Text.Should().Contain("Second line.");
        result.Metadata["file_type"].Should().Be("txt");
    }

    [Fact]
    public async Task MarkdownParser_ParsesRealFile()
    {
        var markdown = "# 智能家居指南\n\n## 灯光控制\n\n通过语音命令控制灯光。\n\n## 温度调节\n\n自动温度控制功能。";
        var filePath = CreateTempFile("guide.md", markdown);

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("智能家居指南");
        result.Text.Should().Contain("灯光控制");
        result.Metadata["file_type"].Should().Be("markdown");
        result.Metadata.Should().ContainKey("headings");
    }

    [Fact]
    public async Task HtmlParser_ParsesRealFile()
    {
        var html = "<html><head><title>Test</title><style>body{color:red}</style></head>" +
                   "<body><h1>标题</h1><p>段落内容</p><script>alert('x')</script></body></html>";
        var filePath = CreateTempFile("page.html", html);

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("标题");
        result.Text.Should().Contain("段落内容");
        result.Text.Should().NotContain("<script>");
        result.Text.Should().NotContain("alert");
        result.Metadata["file_type"].Should().Be("html");
    }

    [Fact]
    public async Task HtmlParser_HtmExtension_WorksThroughFactory()
    {
        var html = "<p>HTM 文件内容</p>";
        var filePath = CreateTempFile("page.htm", html);

        _factory.IsSupported(filePath).Should().BeTrue();
        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("HTM 文件内容");
    }

    [Fact]
    public async Task CsvParser_ParsesRealFile()
    {
        var csv = "Name,Age,City\nAlice,30,Beijing\nBob,25,Shanghai";
        var filePath = CreateTempFile("data.csv", csv);

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("Alice");
        result.Text.Should().Contain("Beijing");
        result.Metadata["file_type"].Should().Be("csv");
        result.Metadata["row_count"].Should().Be(2);
    }

    [Fact]
    public async Task CsvParser_QuotedFields_ParsesCorrectly()
    {
        var csv = "Name,Description\n\"Alice, Jr.\",\"A person with a comma\"\nBob,Normal";
        var filePath = CreateTempFile("quoted.csv", csv);

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().Contain("Alice, Jr.");
        result.Text.Should().Contain("A person with a comma");
    }

    [Fact]
    public async Task CsvParser_EmptyFile_ReturnsEmptyText()
    {
        var filePath = CreateTempFile("empty.csv", "");

        var parser = _factory.GetParser(filePath);
        var result = await parser.ParseAsync(filePath);

        result.Text.Should().BeEmpty();
    }

    private string CreateTempFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // 清理失败不影响测试
        }
    }
}
