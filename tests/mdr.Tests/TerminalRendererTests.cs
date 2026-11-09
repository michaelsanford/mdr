using Markdig;
using Xunit;

namespace mdr.Tests;

public class TerminalRendererTests
{
    private static readonly ColorScheme DefaultScheme = new(
        "Monokai",
        "38;5;228;1", "38;5;81;1", "38;5;166;1", "38;5;141;1",
        "38;5;197", "38;5;186", "38;5;242", "38;5;81", "38;5;141;48;5;236", "38;5;242", "1");

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static List<string> Render(string markdown, int width = 80, ColorScheme? scheme = null)
    {
        var doc = Markdown.Parse(markdown, Pipeline);
        var renderer = new TerminalRenderer(width, scheme ?? DefaultScheme);
        return renderer.RenderToLines(doc);
    }

    [Fact]
    public void VisibleLength_WithoutAnsi_ReturnsStringLength()
    {
        Assert.Equal(0, TerminalRenderer.VisibleLength(""));
        Assert.Equal(5, TerminalRenderer.VisibleLength("Hello"));
        Assert.Equal(12, TerminalRenderer.VisibleLength("Hello, World"));
    }

    [Fact]
    public void VisibleLength_WithAnsi_IgnoresEscapeSequences()
    {
        var withColor = "\x1b[38;5;197mHello\x1b[0m";
        Assert.Equal(5, TerminalRenderer.VisibleLength(withColor));

        var complex = "\x1b[1m\x1b[38;5;81mBold Blue\x1b[0m \x1b[3mItalic\x1b[0m";
        Assert.Equal(16, TerminalRenderer.VisibleLength(complex));
    }

    [Theory]
    [InlineData("# Heading 1", "# Heading 1", "38;5;228;1")]
    [InlineData("## Heading 2", "## Heading 2", "38;5;81;1")]
    [InlineData("### Heading 3", "### Heading 3", "38;5;166;1")]
    [InlineData("#### Heading 4", "#### Heading 4", "38;5;141;1")]
    public void Headings_RenderWithCorrectLevelAndColor(string markdown, string expectedText, string expectedColor)
    {
        var lines = Render(markdown);

        Assert.NotEmpty(lines);
        var headingLine = lines[0];
        Assert.Contains(expectedText, headingLine);
        Assert.Contains($"\x1b[{expectedColor}m", headingLine);
    }

    [Fact]
    public void Paragraph_WrapsAtSpecifiedWidth()
    {
        var markdown = "The quick brown fox jumps over the lazy dog and runs across the wide open green meadow.";
        var lines = Render(markdown, width: 30);

        Assert.True(lines.Count > 1);
        foreach (var line in lines)
        {
            Assert.True(TerminalRenderer.VisibleLength(line) <= 30);
        }
    }

    [Theory]
    [InlineData("csharp", "public class Greeter { string name; }", "public", "38;5;197")]
    [InlineData("cs", "namespace Demo; var x = 10;", "namespace", "38;5;197")]
    [InlineData("c#", "async Task Run() { await Task.Delay(1); }", "async", "38;5;197")]
    [InlineData("javascript", "const x = async () => { return 42; };", "const", "38;5;197")]
    [InlineData("js", "let y = function() { return null; };", "let", "38;5;197")]
    [InlineData("typescript", "interface User { id: number; }", "interface", "38;5;197")]
    [InlineData("ts", "type ID = string | number;", "type", "38;5;197")]
    [InlineData("python", "def greet(): return True", "def", "38;5;197")]
    [InlineData("py", "class Dog: pass", "class", "38;5;197")]
    [InlineData("rust", "fn main() { let mut x = 5; }", "fn", "38;5;197")]
    [InlineData("rs", "pub struct Point { x: i32 }", "pub", "38;5;197")]
    [InlineData("go", "func main() { var x int; return; }", "func", "38;5;197")]
    public void FencedCodeBlock_HighlightsKeywords(string lang, string code, string keyword, string expectedColor)
    {
        var markdown = $"```{lang}\n{code}\n```";
        var lines = Render(markdown);

        var codeLine = lines.FirstOrDefault(l => l.Contains(keyword));
        Assert.NotNull(codeLine);
        Assert.Contains($"\x1b[{expectedColor}m{keyword}\x1b[0m", codeLine);
    }

    [Fact]
    public void FencedCodeBlock_HighlightsStringsAndComments()
    {
        var markdown = "```csharp\nvar greeting = \"Hello World\"; // friendly greeting\n```";
        var lines = Render(markdown);

        var codeLine = lines.FirstOrDefault(l => l.Contains("Hello World"));
        Assert.NotNull(codeLine);

        // String highlighting
        Assert.Contains($"\x1b[{DefaultScheme.String}m\"Hello World\"\x1b[0m", codeLine);

        // Comment highlighting
        Assert.Contains($"\x1b[{DefaultScheme.Comment}m// friendly greeting\x1b[0m", codeLine);
    }

    [Fact]
    public void FencedCodeBlock_UnknownLanguage_RendersWithBorderWithoutCrashing()
    {
        var markdown = "```unknownlang\nsome arbitrary code\n```";
        var lines = Render(markdown);

        Assert.Contains(lines, l => l.Contains("unknownlang"));
        Assert.Contains(lines, l => l.Contains("some arbitrary code"));
    }

    [Fact]
    public void UnorderedList_RendersBulletsAndIndentation()
    {
        var markdown = "- Item 1\n- Item 2\n  - Subitem A";
        var lines = Render(markdown);

        Assert.Contains(lines, l => l.Contains("• Item 1"));
        Assert.Contains(lines, l => l.Contains("• Item 2"));
        Assert.Contains(lines, l => l.Contains("• Subitem A"));
    }

    [Fact]
    public void OrderedList_RendersNumberedPrefixes()
    {
        var markdown = "1. First\n2. Second\n3. Third";
        var lines = Render(markdown);

        Assert.Contains(lines, l => l.Contains("1. First"));
        Assert.Contains(lines, l => l.Contains("2. Second"));
        Assert.Contains(lines, l => l.Contains("3. Third"));
    }

    [Fact]
    public void Blockquote_RendersWithBorderAndItalics()
    {
        var markdown = "> Inspiring quote here.";
        var lines = Render(markdown);

        var quoteLine = lines.FirstOrDefault(l => l.Contains("Inspiring quote here."));
        Assert.NotNull(quoteLine);
        Assert.Contains("│", quoteLine);
        Assert.Contains("\x1b[3m", quoteLine);
    }

    [Fact]
    public void ThematicBreak_RendersHorizontalLineMatchingWidth()
    {
        var markdown = "---";
        var width = 40;
        var lines = Render(markdown, width: width);

        var hrLine = lines.FirstOrDefault(l => l.Contains('─'));
        Assert.NotNull(hrLine);
        Assert.Equal(width, TerminalRenderer.VisibleLength(hrLine));
    }

    [Fact]
    public void Inlines_BoldItalicCodeLinks_RenderWithExpectedAnsi()
    {
        var markdown = "**bold** *italic* `code` [link](https://example.com)";
        var lines = Render(markdown);

        var line = lines[0];
        Assert.Contains($"\x1b[{DefaultScheme.Bold}mbold\x1b[22m", line);
        Assert.Contains("\x1b[3mitalic\x1b[23m", line);
        Assert.Contains($"\x1b[{DefaultScheme.InlineCode}mcode\x1b[0m", line);
        Assert.Contains($"\x1b[{DefaultScheme.Link};4mlink\x1b[0m", line);
        Assert.Contains("(https://example.com)", line);
    }

    [Fact]
    public void Table_RendersHeadersRowsAndBorders()
    {
        var markdown = """
            | Name  | Role      |
            |-------|-----------|
            | Alice | Developer |
            | Bob   | Designer  |
            """;
        var lines = Render(markdown, width: 50);

        Assert.Contains(lines, l => l.Contains('╭') && l.Contains('┬') && l.Contains('╮'));
        Assert.Contains(lines, l => l.Contains("Alice") && l.Contains("Developer"));
        Assert.Contains(lines, l => l.Contains("Bob") && l.Contains("Designer"));
        Assert.Contains(lines, l => l.Contains('╰') && l.Contains('┴') && l.Contains('╯'));
    }

    [Fact]
    public void Table_WrapsContent_WhenTableIsTooWide()
    {
        var markdown = """
            | Job | Where | Role (= CLI profile) | What it can do |
            | --- | --- | --- | --- |
            | **Investigate** | production `655014936372`, `ap-southeast-2` (Sydney) | `ReadOnlyDataSafe` | See every configuration, metric, alarm and audit event. No customer data, no secret values, no changes. |
            | **Build** | Training Sandbox `713881802651`, `ca-central-1` (Montreal) | `CloudTrainee` | Create, break, fix and tear down your own `trainee-<name>-*` resources. |
            """;
        var lines = Render(markdown, width: 80);

        // Does not truncate with ellipsis
        Assert.DoesNotContain(lines, l => l.Contains('…'));
        // Does not dump Markdig AST type names
        Assert.DoesNotContain(lines, l => l.Contains("Markdig.Syntax"));
        Assert.DoesNotContain(lines, l => l.Contains("CodeInline"));

        // Wraps into multiple lines for the data row
        Assert.True(lines.Count > 10);

        // Every line within the table boundaries has visible length equal to table width
        var borderLines = lines.Where(l => l.Contains('│') || l.Contains('╭') || l.Contains('├') || l.Contains('╰')).ToList();
        Assert.NotEmpty(borderLines);
        var expectedWidth = TerminalRenderer.VisibleLength(borderLines[0]);
        foreach (var bl in borderLines)
        {
            Assert.Equal(expectedWidth, TerminalRenderer.VisibleLength(bl));
        }
    }

    [Fact]
    public void Table_RendersFormattedInlines_InsideCells()
    {
        var markdown = """
            | Feature | Example |
            |---|---|
            | Bold and Code | **Investigate** `ReadOnlyDataSafe` |
            """;
        var lines = Render(markdown, width: 60);

        Assert.Contains(lines, l => l.Contains($"\x1b[{DefaultScheme.Bold}mInvestigate\x1b[22m")
                                 && l.Contains($"\x1b[{DefaultScheme.InlineCode}mReadOnlyDataSafe\x1b[0m"));
    }

    [Fact]
    public void Table_RespectsColumnAlignment()
    {
        var markdown = """
            | Left | Center | Right |
            | :--- | :---: | ---: |
            | 1 | 2 | 3 |
            | Long text | Middle | Far right |
            """;
        var lines = Render(markdown, width: 50);

        // Right-aligned column should have padding spaces before the content
        var rightCellLine = lines.FirstOrDefault(l => l.Contains("Far right"));
        Assert.NotNull(rightCellLine);
        // "Far right │" has no trailing extra spaces after the text before the border
        Assert.Contains("Far right \x1b[38;5;242m│\x1b[0m", rightCellLine);
    }

    [Fact]
    public void Table_HandlesBrInCells()
    {
        var markdown = """
            | Col 1 | Col 2 |
            | --- | --- |
            | Line 1<br>Line 2 | Other |
            """;
        var lines = Render(markdown, width: 40);

        Assert.Contains(lines, l => l.Contains("Line 1") && l.Contains("Other"));
        Assert.Contains(lines, l => l.Contains("Line 2"));
    }

    [Fact]
    public void Headings_WithInlineCode_RendersCleanTextWithoutTypeName()
    {
        var markdown = "### The `aws sts` command";
        var lines = Render(markdown);

        Assert.NotEmpty(lines);
        Assert.Contains("### The aws sts command", lines[0]);
        Assert.DoesNotContain("CodeInline", lines[0]);
    }

    [Fact]
    public void Inlines_AutolinksAndHtmlEntitiesAndTaskLists_RenderCorrectly()
    {
        var markdown = """
            Autolink: <https://github.com>
            Entity: &copy; 2026 &mdash; Stelvio
            - [ ] Unchecked
            - [x] Checked
            """;
        var lines = Render(markdown);

        Assert.Contains(lines, l => l.Contains("https://github.com"));
        Assert.Contains(lines, l => l.Contains("© 2026 — Stelvio"));
        Assert.Contains(lines, l => l.Contains("• [ ] Unchecked"));
        Assert.Contains(lines, l => l.Contains("• [x] Checked"));
        Assert.DoesNotContain(lines, l => l.Contains("Markdig"));
    }

    [Fact]
    public void Inlines_Strikethrough_RendersWithAnsiStrikethrough()
    {
        var markdown = "~~deleted text~~";
        var lines = Render(markdown);

        Assert.NotEmpty(lines);
        Assert.Contains("\x1b[9mdeleted text\x1b[29m", lines[0]);
    }

    [Fact]
    public void Blockquote_WrapsLongTextAtSpecifiedWidth()
    {
        var markdown = "> This is a very long blockquote line that definitely exceeds thirty characters in length and must wrap cleanly across multiple lines.";
        var lines = Render(markdown, width: 35);

        Assert.True(lines.Count > 2);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            Assert.StartsWith("\x1b[38;5;242m│\x1b[0m \x1b[3m", line);
            Assert.True(TerminalRenderer.VisibleLength(line) <= 35);
        }
    }
}
