using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;
using Table = Markdig.Extensions.Tables.Table;
using TableCell = Markdig.Extensions.Tables.TableCell;
using TableRow = Markdig.Extensions.Tables.TableRow;

Console.OutputEncoding = Encoding.UTF8;

ColorScheme[] schemes =
[
    new("Monokai",    "38;5;228;1", "38;5;81;1", "38;5;166;1", "38;5;141;1",
        "38;5;197", "38;5;186", "38;5;242", "38;5;81", "38;5;141;48;5;236", "38;5;242", "1"),
    new("Dracula",    "38;5;141;1", "38;5;117;1", "38;5;84;1", "38;5;215;1",
        "38;5;212", "38;5;228", "38;5;103", "38;5;117", "38;5;215;48;5;236", "38;5;103", "1"),
    new("Nord",       "38;5;110;1", "38;5;180;1", "38;5;108;1", "38;5;139;1",
        "38;5;110", "38;5;108", "38;5;60", "38;5;180", "38;5;139;48;5;236", "38;5;60", "1"),
    new("GitHub Dark","38;5;111;1", "38;5;215;1", "38;5;150;1", "38;5;176;1",
        "38;5;204", "38;5;150", "38;5;245", "38;5;111", "38;5;215;48;5;236", "38;5;245", "1"),
    new("Solarized",  "38;5;136;1", "38;5;37;1", "38;5;64;1", "38;5;166;1",
        "38;5;37", "38;5;36", "38;5;246", "38;5;33", "38;5;136;48;5;236", "38;5;246", "1"),
    new("Catppuccin", "38;5;183;1", "38;5;116;1", "38;5;150;1", "38;5;217;1",
        "38;5;183", "38;5;150", "38;5;243", "38;5;116", "38;5;217;48;5;236", "38;5;243", "1"),
];


// --- Parse input ---
var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

string markdown;
string fileName;

if (args.Length > 0 && args[0] != "-")
{
    if (!File.Exists(args[0]))
    {
        AnsiConsole.MarkupLine($"[red]File not found:[/] {args[0]}");
        return 1;
    }
    fileName = args[0];
}
else if (args.Length == 0 && !Console.IsInputRedirected)
{
    if (!AnsiConsole.Console.Profile.Capabilities.Interactive)
    {
        AnsiConsole.MarkupLine("[bold]mdr[/] - Markdown Renderer for the Terminal");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Usage:[/]");
        AnsiConsole.MarkupLine("  mdr <file-path>    Render a markdown file");
        AnsiConsole.MarkupLine("  mdr -              Render markdown from stdin");
        AnsiConsole.MarkupLine("  some-command | mdr Render output piped from another command");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Run in an interactive terminal to use the interactive file picker.");
        return 0;
    }

    var selectedFile = ShowFilePicker();
    if (selectedFile == null)
    {
        return 0;
    }
    fileName = selectedFile;
}
else
{
    fileName = "stdin";
}

if (fileName == "stdin")
{
    try
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        markdown = reader.ReadToEnd();
    }
    catch (IOException ex)
    {
        AnsiConsole.MarkupLine($"[red]Error reading stdin:[/] {ex.Message}");
        return 1;
    }
}
else
{
    try
    {
        markdown = File.ReadAllText(fileName);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        AnsiConsole.MarkupLine($"[red]Error reading file:[/] {ex.Message}");
        return 1;
    }
    fileName = Path.GetFileName(fileName);
}

var width = AnsiConsole.Console.Profile.Width;
var doc = Markdown.Parse(markdown, pipeline);

int schemeIndex = 0;
var renderer = new TerminalRenderer(width, schemes[schemeIndex]);
var lines = renderer.RenderToLines(doc);

// Non-interactive: dump and exit
if (!AnsiConsole.Console.Profile.Capabilities.Interactive || lines.Count <= GetConsoleWindowHeight() - 2)
{
    foreach (var line in lines)
        Console.WriteLine(line);
    return 0;
}

// --- Interactive pager ---
var pageHeight = GetConsoleWindowHeight() - 2;
int offset = 0;
int maxOffset = Math.Max(0, lines.Count - pageHeight);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    SafeClearConsole();
    Console.CursorVisible = true;
    Environment.Exit(0);
};

Console.CursorVisible = false;
SafeClearConsole();
DrawPage(lines, offset, pageHeight, fileName, width, schemes[schemeIndex]);

int lastWidth = GetConsoleWindowWidth();
int lastHeight = GetConsoleWindowHeight();

while (true)
{
    while (!Console.KeyAvailable)
    {
        Thread.Sleep(50);
        int currentWidth = GetConsoleWindowWidth();
        int currentHeight = GetConsoleWindowHeight();
        if (currentWidth != lastWidth || currentHeight != lastHeight)
        {
            lastWidth = currentWidth;
            lastHeight = currentHeight;
            width = lastWidth;
            pageHeight = Math.Max(1, lastHeight - 2);
            renderer = new TerminalRenderer(width, schemes[schemeIndex]);
            lines = renderer.RenderToLines(doc);
            maxOffset = Math.Max(0, lines.Count - pageHeight);
            offset = Math.Min(offset, maxOffset);
            SafeClearConsole();
            DrawPage(lines, offset, pageHeight, fileName, width, schemes[schemeIndex]);
        }
    }

    var key = Console.ReadKey(true);
    int prevOffset = offset;
    bool redraw = false;

    switch (key.Key)
    {
        case ConsoleKey.DownArrow:
        case ConsoleKey.J:
            offset = Math.Min(offset + 1, maxOffset);
            break;
        case ConsoleKey.UpArrow:
        case ConsoleKey.K:
            offset = Math.Max(offset - 1, 0);
            break;
        case ConsoleKey.PageDown:
        case ConsoleKey.Spacebar:
            offset = Math.Min(offset + pageHeight, maxOffset);
            break;
        case ConsoleKey.PageUp:
        case ConsoleKey.B:
            offset = Math.Max(offset - pageHeight, 0);
            break;
        case ConsoleKey.Home:
        case ConsoleKey.G when key.Modifiers == 0 && !char.IsUpper(key.KeyChar):
            offset = 0;
            break;
        case ConsoleKey.End:
        case ConsoleKey.G when char.IsUpper(key.KeyChar):
            offset = maxOffset;
            break;
        case ConsoleKey.T:
            schemeIndex = (schemeIndex + 1) % schemes.Length;
            renderer = new TerminalRenderer(width, schemes[schemeIndex]);
            lines = renderer.RenderToLines(doc);
            maxOffset = Math.Max(0, lines.Count - pageHeight);
            offset = Math.Min(offset, maxOffset);
            redraw = true;
            break;
        case ConsoleKey.Q:
        case ConsoleKey.Escape:
            SafeClearConsole();
            Console.CursorVisible = true;
            return 0;
    }

    int w = GetConsoleWindowWidth();
    int h = GetConsoleWindowHeight();
    if (w != lastWidth || h != lastHeight)
    {
        lastWidth = w;
        lastHeight = h;
        width = lastWidth;
        pageHeight = Math.Max(1, lastHeight - 2);
        renderer = new TerminalRenderer(width, schemes[schemeIndex]);
        lines = renderer.RenderToLines(doc);
        maxOffset = Math.Max(0, lines.Count - pageHeight);
        offset = Math.Min(offset, maxOffset);
        SafeClearConsole();
        redraw = true;
    }

    if (offset != prevOffset || redraw)
        DrawPage(lines, offset, pageHeight, fileName, width, schemes[schemeIndex]);
}

static void DrawPage(List<string> lines, int offset, int pageHeight, string fileName, int width, ColorScheme scheme)
{
    Console.SetCursorPosition(0, 0);
    for (int i = 0; i < pageHeight; i++)
    {
        var idx = offset + i;
        if (idx < lines.Count)
            Console.Write(lines[idx]);
        Console.Write("\x1b[K");
        Console.WriteLine();
    }

    // Status bar: progress
    var progress = lines.Count > 0 ? (int)((offset + pageHeight) * 100.0 / lines.Count) : 100;
    progress = Math.Min(progress, 100);
    var status = $" {fileName} │ {offset + 1}–{Math.Min(offset + pageHeight, lines.Count)} of {lines.Count} lines │ {progress}%";
    Console.Write($"\x1b[7m{status.PadRight(width)}\x1b[0m\x1b[K");

    // Keymap bar with theme button showing current scheme name
    var keySb = new StringBuilder(" ");
    ReadOnlySpan<string> keys = ["↑↓", "PgUp", "PgDn", "Home", "End", "t", "q"];
    ReadOnlySpan<string> labels = ["scroll", "page up", "page down", "top", "bottom", scheme.Name, "quit"];
    for (int k = 0; k < keys.Length; k++)
        keySb.Append($"\x1b[97;48;5;238m {keys[k]} \x1b[0m\x1b[90m {labels[k]}  \x1b[0m");
    Console.Write(keySb.ToString());
    Console.Write("\x1b[K");
}

static string? ShowFilePicker()
{
    var currentDir = Directory.GetCurrentDirectory();
    while (true)
    {
        var items = new List<FilePickerItem>();
        
        var parent = Directory.GetParent(currentDir);
        if (parent != null)
        {
            items.Add(new FilePickerItem(parent.FullName, "📁 .. [grey](Parent Directory)[/]", true));
        }

        try
        {
            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                var name = Path.GetFileName(dir);
                items.Add(new FilePickerItem(dir, $"📁 [blue]{name}/[/]", true));
            }

            foreach (var file in Directory.GetFiles(currentDir))
            {
                var name = Path.GetFileName(file);
                var isMarkdown = name.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                var fileText = isMarkdown ? $"📄 [green]{name}[/]" : $"📄 {name}";
                items.Add(new FilePickerItem(file, fileText, false));
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error reading directory:[/] {ex.Message}");
            if (parent != null)
            {
                currentDir = parent.FullName;
                continue;
            }
            else
            {
                return null;
            }
        }

        items.Add(new FilePickerItem("", "❌ [red]Cancel[/]", false));

        SafeClearConsole();
        var prompt = new SelectionPrompt<FilePickerItem>()
            .Title($"[bold yellow]Select a file to open[/]\n[grey]Current: {currentDir}[/]")
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .AddChoices(items)
            .UseConverter(item => item.DisplayText);

        var selected = AnsiConsole.Prompt(prompt);

        if (selected.Path == "")
        {
            return null;
        }

        if (selected.IsDirectory)
        {
            currentDir = selected.Path;
        }
        else
        {
            return selected.Path;
        }
    }
}

static void SafeClearConsole()
{
    try
    {
        Console.Clear();
    }
    catch (IOException)
    {
        // Fallback: Use standard ANSI escape sequences to clear screen and home cursor
        Console.Write("\x1b[2J\x1b[H");
    }
}

static int GetConsoleWindowWidth(int fallback = 80)
{
    try
    {
        return Console.WindowWidth;
    }
    catch (IOException)
    {
        return fallback;
    }
}

static int GetConsoleWindowHeight(int fallback = 24)
{
    try
    {
        return Console.WindowHeight;
    }
    catch (IOException)
    {
        return fallback;
    }
}

// --- Renderer ---
partial class TerminalRenderer(int width, ColorScheme cs)
{
    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiRegex();

    [GeneratedRegex(@"""[^""]*""")]
    private static partial Regex StringRegex();

    [GeneratedRegex(@"(//.*|#\s.*)")]
    private static partial Regex CommentRegex();

    [GeneratedRegex(@"\b(?:using|namespace|class|struct|interface|enum|public|private|protected|internal|static|void|int|string|bool|var|new|return|if|else|for|foreach|while|switch|case|break|async|await|null|true|false|readonly|override|virtual|abstract|sealed)\b")]
    private static partial Regex CSharpKeywordsRegex();

    [GeneratedRegex(@"\b(?:const|let|var|function|return|if|else|for|while|class|import|export|from|async|await|new|null|undefined|true|false|this|typeof|interface|type)\b")]
    private static partial Regex JavaScriptKeywordsRegex();

    [GeneratedRegex(@"\b(?:def|class|import|from|return|if|elif|else|for|while|in|not|and|or|None|True|False|self|with|as|try|except|raise|yield|async|await|lambda)\b")]
    private static partial Regex PythonKeywordsRegex();

    [GeneratedRegex(@"\b(?:fn|let|mut|pub|struct|enum|impl|trait|use|mod|return|if|else|for|while|match|self|Self|true|false|async|await|where|type)\b")]
    private static partial Regex RustKeywordsRegex();

    [GeneratedRegex(@"\b(?:func|package|import|var|const|type|struct|interface|return|if|else|for|range|switch|case|defer|go|chan|map|nil|true|false)\b")]
    private static partial Regex GoKeywordsRegex();

    // Fence labels are mapped onto precompiled regexes via a frozen dictionary for O(1) allocation-free lookup
    private static readonly FrozenDictionary<string, Regex> LanguageRegexes = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase)
    {
        ["csharp"] = CSharpKeywordsRegex(),
        ["cs"] = CSharpKeywordsRegex(),
        ["c#"] = CSharpKeywordsRegex(),
        ["javascript"] = JavaScriptKeywordsRegex(),
        ["js"] = JavaScriptKeywordsRegex(),
        ["typescript"] = JavaScriptKeywordsRegex(),
        ["ts"] = JavaScriptKeywordsRegex(),
        ["python"] = PythonKeywordsRegex(),
        ["py"] = PythonKeywordsRegex(),
        ["rust"] = RustKeywordsRegex(),
        ["rs"] = RustKeywordsRegex(),
        ["go"] = GoKeywordsRegex(),
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public List<string> RenderToLines(MarkdownDocument doc)
    {
        var output = new List<string>();
        foreach (var block in doc)
            RenderBlock(block, output);
        return output;
    }

    private void RenderBlock(Block block, List<string> output)
    {
        switch (block)
        {
            case HeadingBlock h:
                var level = h.Level;
                var text = GetInlineText(h.Inline);
                var color = level switch { 1 => cs.Heading1, 2 => cs.Heading2, 3 => cs.Heading3, _ => cs.Heading4 };
                output.Add($"\x1b[{color}m{new string('#', level)} {text}\x1b[0m");
                output.Add("");
                break;

            case ParagraphBlock p:
                var paraText = RenderInlines(p.Inline);
                foreach (var line in WrapText(paraText, width))
                    output.Add(line);
                output.Add("");
                break;

            case FencedCodeBlock code:
                var lang = code.Info ?? "";
                var codeLines = code.Lines.Lines.Take(code.Lines.Count).Select(l => l.ToString());
                RenderCodeBlock(codeLines, lang, output);
                output.Add("");
                break;

            case CodeBlock code2:
                var plainLines = code2.Lines.Lines.Take(code2.Lines.Count).Select(l => l.ToString());
                RenderCodeBlock(plainLines, "", output);
                output.Add("");
                break;

            case Markdig.Extensions.Tables.Table table:
                RenderTable(table, output);
                output.Add("");
                break;

            case ListBlock list:
                RenderList(list, 0, output);
                output.Add("");
                break;

            case ThematicBreakBlock:
                output.Add($"\x1b[{cs.Border}m{"".PadRight(width, '─')}\x1b[0m");
                output.Add("");
                break;

            case QuoteBlock quote:
                foreach (var child in quote)
                {
                    if (child is ParagraphBlock qp)
                    {
                        var qText = RenderInlines(qp.Inline);
                        var availWidth = Math.Max(1, width - 2);
                        foreach (var line in WrapText(qText, availWidth))
                        {
                            var styledLine = line.Replace("\x1b[0m", "\x1b[0m\x1b[3m");
                            output.Add($"\x1b[{cs.Border}m│\x1b[0m \x1b[3m{styledLine}\x1b[0m");
                        }
                    }
                    else RenderBlock(child, output);
                }
                output.Add("");
                break;

            case ContainerBlock container:
                foreach (var child in container)
                    RenderBlock(child, output);
                break;
        }
    }

    private void RenderList(ListBlock list, int indent, List<string> output)
    {
        int index = 1;
        foreach (var item in list)
        {
            if (item is ListItemBlock li)
            {
                var prefix = list.IsOrdered ? $"{index++}. " : "• ";
                var pad = new string(' ', indent * 2);
                foreach (var child in li)
                {
                    if (child is ParagraphBlock p)
                    {
                        var text = RenderInlines(p.Inline);
                        var availWidth = Math.Max(1, width - pad.Length - prefix.Length);
                        var wrapped = WrapText(text, availWidth);
                        output.Add($"{pad}{prefix}{(wrapped.Count > 0 ? wrapped[0] : "")}");
                        var continuation = new string(' ', prefix.Length);
                        for (int wi = 1; wi < wrapped.Count; wi++)
                            output.Add($"{pad}{continuation}{wrapped[wi]}");
                        prefix = "  ";
                    }
                    else if (child is ListBlock nested)
                        RenderList(nested, indent + 1, output);
                    else RenderBlock(child, output);
                }
            }
        }
    }

    private void RenderTable(Table table, List<string> output)
    {
        var rows = new List<(List<string> Cells, bool IsHeader)>();
        foreach (var rowObj in table)
        {
            if (rowObj is TableRow row)
            {
                var cells = new List<string>();
                foreach (var cell in row)
                {
                    if (cell is TableCell tc)
                        cells.Add(RenderTableCell(tc));
                }
                rows.Add((cells, row.IsHeader));
            }
        }

        if (rows.Count == 0) return;
        var colCount = rows.Max(r => r.Cells.Count);
        if (colCount == 0) return;

        foreach (var row in rows)
        {
            while (row.Cells.Count < colCount)
                row.Cells.Add("");
        }

        var alignments = new TableColumnAlign?[colCount];
        if (table.ColumnDefinitions != null)
        {
            for (int i = 0; i < Math.Min(colCount, table.ColumnDefinitions.Count); i++)
                alignments[i] = table.ColumnDefinitions[i]?.Alignment;
        }

        var naturalWidths = new int[colCount];
        var minWidths = new int[colCount];

        for (int c = 0; c < colCount; c++)
        {
            int maxCellNatural = 3;
            int maxWordLen = 3;
            for (int r = 0; r < rows.Count; r++)
            {
                var cell = rows[r].Cells[c];
                var lines = cell.Split('\n');
                foreach (var line in lines)
                {
                    maxCellNatural = Math.Max(maxCellNatural, VisibleLength(line));
                    foreach (var w in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        maxWordLen = Math.Max(maxWordLen, VisibleLength(w));
                }
            }
            naturalWidths[c] = maxCellNatural;
            minWidths[c] = Math.Min(maxCellNatural, maxWordLen);
        }

        var borderChars = 1 + colCount * 3;
        var available = Math.Max(colCount * 3, width - borderChars);

        var colWidths = new int[colCount];
        var totalNatural = naturalWidths.Sum();

        if (totalNatural <= available)
        {
            for (int i = 0; i < colCount; i++)
                colWidths[i] = naturalWidths[i];
        }
        else
        {
            var totalMin = minWidths.Sum();
            if (totalMin <= available)
            {
                var excess = available - totalMin;
                var demands = new int[colCount];
                for (int i = 0; i < colCount; i++)
                    demands[i] = naturalWidths[i] - minWidths[i];
                var totalDemand = demands.Sum();

                int allocated = 0;
                for (int i = 0; i < colCount; i++)
                {
                    int extra = totalDemand > 0 ? (int)((long)demands[i] * excess / totalDemand) : 0;
                    colWidths[i] = minWidths[i] + extra;
                    allocated += colWidths[i];
                }

                int remaining = available - allocated;
                while (remaining > 0)
                {
                    int bestCol = -1;
                    int maxShortfall = -1;
                    for (int i = 0; i < colCount; i++)
                    {
                        int shortfall = naturalWidths[i] - colWidths[i];
                        if (shortfall > maxShortfall)
                        {
                            maxShortfall = shortfall;
                            bestCol = i;
                        }
                    }
                    if (bestCol == -1 || maxShortfall <= 0)
                    {
                        colWidths[0] += remaining;
                        break;
                    }
                    colWidths[bestCol]++;
                    remaining--;
                }
            }
            else
            {
                int allocated = 0;
                for (int i = 0; i < colCount; i++)
                {
                    colWidths[i] = Math.Max(3, (int)((long)available * naturalWidths[i] / totalNatural));
                    allocated += colWidths[i];
                }
                int diff = available - allocated;
                if (diff != 0 && colWidths.Length > 0)
                    colWidths[^1] = Math.Max(3, colWidths[^1] + diff);
            }
        }

        var b = cs.Border;
        var topBorder = "╭" + string.Join("┬", colWidths.Select(w => new string('─', w + 2))) + "╮";
        var midBorder = "├" + string.Join("┼", colWidths.Select(w => new string('─', w + 2))) + "┤";
        var botBorder = "╰" + string.Join("┴", colWidths.Select(w => new string('─', w + 2))) + "╯";

        output.Add($"\x1b[{b}m{topBorder}\x1b[0m");

        for (int r = 0; r < rows.Count; r++)
        {
            var isHeader = rows[r].IsHeader || (r == 0 && rows.Count > 1 && !rows.Any(x => x.IsHeader));
            var cellLinesByCol = new List<string>[colCount];
            int rowHeight = 1;

            for (int c = 0; c < colCount; c++)
            {
                var wrapped = WrapText(rows[r].Cells[c], colWidths[c]);
                if (wrapped.Count == 0) wrapped = [""];
                cellLinesByCol[c] = wrapped;
                rowHeight = Math.Max(rowHeight, wrapped.Count);
            }

            for (int lineIdx = 0; lineIdx < rowHeight; lineIdx++)
            {
                var sb = new StringBuilder();
                sb.Append($"\x1b[{b}m│\x1b[0m");
                for (int c = 0; c < colCount; c++)
                {
                    var lineText = lineIdx < cellLinesByCol[c].Count ? cellLinesByCol[c][lineIdx] : "";
                    var padded = PadCell(lineText, colWidths[c], alignments[c]);
                    if (isHeader)
                    {
                        var boldPadded = padded.Replace("\x1b[0m", $"\x1b[0m\x1b[{cs.Bold}m");
                        sb.Append($" \x1b[{cs.Bold}m{boldPadded}\x1b[0m \x1b[{b}m│\x1b[0m");
                    }
                    else
                    {
                        sb.Append($" {padded} \x1b[{b}m│\x1b[0m");
                    }
                }
                output.Add(sb.ToString());
            }

            if (isHeader && r == 0)
            {
                output.Add($"\x1b[{b}m{midBorder}\x1b[0m");
            }
        }

        output.Add($"\x1b[{b}m{botBorder}\x1b[0m");
    }

    private static string PadCell(string text, int width, TableColumnAlign? alignment)
    {
        var vLen = VisibleLength(text);
        var pad = Math.Max(0, width - vLen);
        if (pad == 0) return text;

        return alignment switch
        {
            TableColumnAlign.Right => new string(' ', pad) + text,
            TableColumnAlign.Center => new string(' ', pad / 2) + text + new string(' ', pad - pad / 2),
            _ => text + new string(' ', pad),
        };
    }

    private string RenderTableCell(TableCell cell)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var block in cell)
        {
            if (block is ParagraphBlock { Inline: not null } p)
            {
                if (!first) sb.Append('\n');
                sb.Append(RenderInlines(p.Inline));
                first = false;
            }
        }
        return sb.ToString();
    }

    private void RenderCodeBlock(IEnumerable<string> codeLines, string lang, List<string> output)
    {
        var innerWidth = width - 4;
        var b = cs.Border;

        var topLabel = string.IsNullOrEmpty(lang)
            ? $"\x1b[{b}m╭{new string('─', width - 2)}╮\x1b[0m"
            : $"\x1b[{b}m╭─\x1b[{cs.Keyword}m{lang}\x1b[{b}m{new string('─', Math.Max(0, width - 4 - lang.Length))}╮\x1b[0m";
        output.Add(topLabel);

        foreach (var line in codeLines)
        {
            var highlighted = HighlightLine(line, lang);
            var visibleLen = VisibleLength(line);
            var padding = Math.Max(0, innerWidth - visibleLen);
            output.Add($"\x1b[{b}m│\x1b[0m {highlighted}{new string(' ', padding)} \x1b[{b}m│\x1b[0m");
        }

        output.Add($"\x1b[{b}m╰{new string('─', width - 2)}╯\x1b[0m");
    }

    /// <summary>Keyword pattern for a fence label, or null when the language is unknown.</summary>
    private static Regex? KeywordRegexFor(string lang) => LanguageRegexes.GetValueOrDefault(lang);

    private string HighlightLine(string line, string lang)
    {
        if (string.IsNullOrEmpty(lang)) return line;
        
        var keywordRegex = KeywordRegexFor(lang);

        var result = line;
        if (keywordRegex != null)
        {
            result = keywordRegex.Replace(result, m => $"\x1b[{cs.Keyword}m{m.Value}\x1b[0m");
        }

        // String literals and comments are applied after and take visual priority —
        // their spans re-colour anything (including keyword highlights) they contain.
        result = StringRegex().Replace(result,  m => $"\x1b[{cs.String}m{m.Value}\x1b[0m");
        result = CommentRegex().Replace(result, m => $"\x1b[{cs.Comment}m{m.Value}\x1b[0m");
        return result;
    }

    internal static int VisibleLength(string s) => s.Contains('\x1b') ? AnsiRegex().Replace(s, "").Length : s.Length;

    private string RenderInlines(ContainerInline? inlines)
    {
        if (inlines == null) return "";
        var sb = new StringBuilder();
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content.AsSpan());
                    break;
                case EmphasisInline em:
                    var inner = RenderInlines(em);
                    if (em.DelimiterChar == '~')
                    {
                        sb.Append($"\x1b[9m{inner}\x1b[29m");
                    }
                    else if (em.DelimiterCount >= 3)
                    {
                        sb.Append($"\x1b[{cs.Bold};3m{inner}\x1b[22;23m");
                    }
                    else if (em.DelimiterCount == 2)
                    {
                        sb.Append($"\x1b[{cs.Bold}m{inner}\x1b[22m");
                    }
                    else
                    {
                        sb.Append($"\x1b[3m{inner}\x1b[23m");
                    }
                    break;
                case CodeInline code:
                    sb.Append($"\x1b[{cs.InlineCode}m{code.Content}\x1b[0m");
                    break;
                case LinkInline link:
                    var linkText = RenderInlines(link);
                    if (link.IsImage)
                    {
                        sb.Append($"\x1b[{cs.Link}m🖼 {linkText}\x1b[0m");
                        if (!string.IsNullOrEmpty(link.Url))
                            sb.Append($" \x1b[{cs.Comment}m({link.Url})\x1b[0m");
                    }
                    else
                    {
                        sb.Append($"\x1b[{cs.Link};4m{linkText}\x1b[0m");
                        if (!string.IsNullOrEmpty(link.Url))
                            sb.Append($" \x1b[{cs.Comment}m({link.Url})\x1b[0m");
                    }
                    break;
                case AutolinkInline auto:
                    sb.Append($"\x1b[{cs.Link};4m{auto.Url}\x1b[0m");
                    break;
                case LineBreakInline lb:
                    sb.Append(lb.IsHard ? '\n' : ' ');
                    break;
                case HtmlEntityInline entity:
                    sb.Append(entity.Transcoded.AsSpan());
                    break;
                case HtmlInline html:
                    if (html.Tag.Equals("<br>", StringComparison.OrdinalIgnoreCase) ||
                        html.Tag.Equals("<br/>", StringComparison.OrdinalIgnoreCase) ||
                        html.Tag.Equals("<br />", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append('\n');
                    }
                    break;
                case TaskList task:
                    sb.Append(task.Checked ? "[x]" : "[ ]");
                    break;
                case ContainerInline container:
                    sb.Append(RenderInlines(container));
                    break;
                default:
                    break;
            }
        }
        return sb.ToString();
    }

    private static string GetInlineText(ContainerInline? inlines)
    {
        if (inlines == null) return "";
        var sb = new StringBuilder();
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content.AsSpan());
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case EmphasisInline em:
                    sb.Append(GetInlineText(em));
                    break;
                case LinkInline link:
                    sb.Append(GetInlineText(link));
                    break;
                case AutolinkInline auto:
                    sb.Append(auto.Url);
                    break;
                case HtmlEntityInline entity:
                    sb.Append(entity.Transcoded.AsSpan());
                    break;
                case HtmlInline html:
                    if (html.Tag.Equals("<br>", StringComparison.OrdinalIgnoreCase) ||
                        html.Tag.Equals("<br/>", StringComparison.OrdinalIgnoreCase) ||
                        html.Tag.Equals("<br />", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append('\n');
                    }
                    break;
                case LineBreakInline lb:
                    sb.Append(lb.IsHard ? '\n' : ' ');
                    break;
                case TaskList task:
                    sb.Append(task.Checked ? "[x]" : "[ ]");
                    break;
                case ContainerInline container:
                    sb.Append(GetInlineText(container));
                    break;
                default:
                    break;
            }
        }
        return sb.ToString();
    }

    private static List<string> WrapText(string text, int width)
    {
        if (width <= 0) return [text];
        var result = new List<string>();
        foreach (var segment in text.Split('\n'))
        {
            if (VisibleLength(segment) <= width)
            {
                result.Add(segment);
                continue;
            }

            var words = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            int currentLen = 0;
            var activeAnsi = new List<string>();

            foreach (var word in words)
            {
                var wLen = VisibleLength(word);
                if (wLen > width)
                {
                    if (currentLen > 0)
                    {
                        if (activeAnsi.Count > 0) current.Append("\x1b[0m");
                        result.Add(current.ToString());
                        current.Clear();
                        currentLen = 0;
                    }

                    var chunks = SplitWordToWidth(word, width, activeAnsi);
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        if (i < chunks.Count - 1)
                        {
                            result.Add(chunks[i]);
                        }
                        else
                        {
                            current.Append(chunks[i]);
                            currentLen = VisibleLength(chunks[i]);
                        }
                    }
                    continue;
                }

                if (currentLen + wLen + (currentLen > 0 ? 1 : 0) > width && currentLen > 0)
                {
                    if (activeAnsi.Count > 0) current.Append("\x1b[0m");
                    result.Add(current.ToString());
                    current.Clear();
                    currentLen = 0;
                    if (activeAnsi.Count > 0)
                    {
                        foreach (var ansi in activeAnsi) current.Append(ansi);
                    }
                }

                if (currentLen > 0)
                {
                    current.Append(' ');
                    currentLen++;
                }

                current.Append(word);
                currentLen += wLen;
                UpdateActiveAnsi(word, activeAnsi);
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }
        }
        return result;
    }

    private static List<string> SplitWordToWidth(string word, int width, List<string> activeAnsi)
    {
        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        int currentVisible = 0;

        int i = 0;
        while (i < word.Length)
        {
            if (word[i] == '\x1b' && i + 1 < word.Length && word[i + 1] == '[')
            {
                int end = i + 2;
                while (end < word.Length && word[end] != 'm')
                    end++;
                if (end < word.Length && word[end] == 'm')
                {
                    var ansiSeq = word.Substring(i, end - i + 1);
                    currentChunk.Append(ansiSeq);
                    UpdateActiveAnsi(ansiSeq, activeAnsi);
                    i = end + 1;
                    continue;
                }
            }

            currentChunk.Append(word[i]);
            currentVisible++;
            i++;

            if (currentVisible >= width && i < word.Length)
            {
                if (activeAnsi.Count > 0)
                    currentChunk.Append("\x1b[0m");
                chunks.Add(currentChunk.ToString());
                currentChunk.Clear();
                currentVisible = 0;
                if (activeAnsi.Count > 0)
                {
                    foreach (var ansi in activeAnsi)
                        currentChunk.Append(ansi);
                }
            }
        }

        if (currentChunk.Length > 0)
            chunks.Add(currentChunk.ToString());

        return chunks;
    }

    private static void UpdateActiveAnsi(string s, List<string> activeAnsi)
    {
        var matches = AnsiRegex().Matches(s);
        foreach (Match match in matches)
        {
            var code = match.Value;
            if (code == "\x1b[0m" || code == "\x1b[m")
            {
                activeAnsi.Clear();
            }
            else if (code == "\x1b[22m")
            {
                activeAnsi.RemoveAll(a => a.Contains(";1m") || a == "\x1b[1m");
            }
            else if (code == "\x1b[23m")
            {
                activeAnsi.RemoveAll(a => a == "\x1b[3m");
            }
            else if (code == "\x1b[29m")
            {
                activeAnsi.RemoveAll(a => a == "\x1b[9m");
            }
            else
            {
                activeAnsi.Add(code);
            }
        }
    }
}


record ColorScheme(string Name, string Heading1, string Heading2, string Heading3, string Heading4,
    string Keyword, string String, string Comment, string Link, string InlineCode, string Border, string Bold);

record FilePickerItem(string Path, string DisplayText, bool IsDirectory);
