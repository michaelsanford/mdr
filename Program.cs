using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Spectre.Console;
using System.Text;
using System.Text.RegularExpressions;

var schemes = new ColorScheme[]
{
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
};

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
if (!AnsiConsole.Console.Profile.Capabilities.Interactive || lines.Count <= Console.WindowHeight - 2)
{
    foreach (var line in lines)
        Console.WriteLine(line);
    return 0;
}

// --- Interactive pager ---
var pageHeight = Console.WindowHeight - 2;
int offset = 0;
int maxOffset = Math.Max(0, lines.Count - pageHeight);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.Clear();
    Console.CursorVisible = true;
    Environment.Exit(0);
};

Console.CursorVisible = false;
Console.Clear();
DrawPage(lines, offset, pageHeight, fileName, width, schemes[schemeIndex]);

int lastWidth = Console.WindowWidth;
int lastHeight = Console.WindowHeight;

while (true)
{
    while (!Console.KeyAvailable)
    {
        Thread.Sleep(50);
        int currentWidth = Console.WindowWidth;
        int currentHeight = Console.WindowHeight;
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
            Console.Clear();
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
            Console.Clear();
            Console.CursorVisible = true;
            return 0;
    }

    int w = Console.WindowWidth;
    int h = Console.WindowHeight;
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
        Console.Clear();
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
    var keys = new[] { "↑↓", "PgUp", "PgDn", "Home", "End", "t", "q" };
    var labels = new[] { "scroll", "page up", "page down", "top", "bottom", scheme.Name, "quit" };
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

        Console.Clear();
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

// --- Renderer ---
class TerminalRenderer(int width, ColorScheme cs)
{
    // Compiled once at class load — reused across all rendering calls
    private static readonly Regex AnsiRegex    = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);
    private static readonly Regex StringRegex  = new(@"""[^""]*""",      RegexOptions.Compiled);
    private static readonly Regex CommentRegex = new(@"(//.*|#\s.*)",    RegexOptions.Compiled);
    private static readonly Dictionary<string, Regex?> KeywordRegexCache = new();

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
                var codeText = string.Join('\n', code.Lines.Lines.Take(code.Lines.Count).Select(l => l.ToString()));
                RenderCodeBlock(codeText, lang, output);
                output.Add("");
                break;

            case CodeBlock code2:
                var plainCode = string.Join('\n', code2.Lines.Lines.Take(code2.Lines.Count).Select(l => l.ToString()));
                RenderCodeBlock(plainCode, "", output);
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
                        output.Add($"\x1b[{cs.Border}m│\x1b[0m \x1b[3m{qText}\x1b[0m");
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

    private void RenderTable(Markdig.Extensions.Tables.Table table, List<string> output)
    {
        var rows = new List<List<string>>();
        foreach (var rowObj in table)
            if (rowObj is Markdig.Extensions.Tables.TableRow row)
            {
                var cells = new List<string>();
                foreach (var cell in row)
                    if (cell is Markdig.Extensions.Tables.TableCell tc)
                        cells.Add(GetBlockText(tc));
                rows.Add(cells);
            }

        if (rows.Count == 0) return;
        var colCount = rows.Max(r => r.Count);
        foreach (var row in rows) while (row.Count < colCount) row.Add("");

        var colWidths = new int[colCount];
        foreach (var row in rows)
            for (int i = 0; i < colCount; i++)
                colWidths[i] = Math.Max(colWidths[i], row[i].Length);

        var totalContent = colWidths.Sum();
        var borderChars = 1 + colCount * 3;
        var available = width - borderChars;
        if (totalContent > available && available > 0)
        {
            var scale = (double)available / totalContent;
            for (int i = 0; i < colCount; i++)
                colWidths[i] = Math.Max(3, (int)(colWidths[i] * scale));
        }

        var b = cs.Border;
        var topBorder = "╭" + string.Join("┬", colWidths.Select(w => new string('─', w + 2))) + "╮";
        var midBorder = "├" + string.Join("┼", colWidths.Select(w => new string('─', w + 2))) + "┤";
        var botBorder = "╰" + string.Join("┴", colWidths.Select(w => new string('─', w + 2))) + "╯";

        output.Add($"\x1b[{b}m{topBorder}\x1b[0m");
        for (int r = 0; r < rows.Count; r++)
        {
            var sb = new StringBuilder();
            sb.Append($"\x1b[{b}m│\x1b[0m");
            for (int c = 0; c < colCount; c++)
            {
                var cell = rows[r][c];
                if (cell.Length > colWidths[c]) cell = cell[..(colWidths[c] - 1)] + "…";
                var padded = cell.PadRight(colWidths[c]);
                if (r == 0)
                    sb.Append($" \x1b[{cs.Bold}m{padded}\x1b[0m \x1b[{b}m│\x1b[0m");
                else
                    sb.Append($" {padded} \x1b[{b}m│\x1b[0m");
            }
            output.Add(sb.ToString());
            if (r == 0) output.Add($"\x1b[{b}m{midBorder}\x1b[0m");
        }
        output.Add($"\x1b[{b}m{botBorder}\x1b[0m");
    }

    private void RenderCodeBlock(string code, string lang, List<string> output)
    {
        var innerWidth = width - 4;
        var codeLines = code.Split('\n');
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

    private string HighlightLine(string line, string lang)
    {
        if (string.IsNullOrEmpty(lang)) return line;
        
        var lowerLang = lang.ToLowerInvariant();
        if (!KeywordRegexCache.TryGetValue(lowerLang, out var keywordRegex))
        {
            var keywords = GetKeywords(lowerLang);
            if (keywords.Length > 0)
            {
                var pattern = $@"\b(?:{string.Join("|", keywords.Select(Regex.Escape))})\b";
                keywordRegex = new Regex(pattern, RegexOptions.Compiled);
            }
            else
            {
                keywordRegex = null;
            }
            KeywordRegexCache[lowerLang] = keywordRegex;
        }

        var result = line;
        if (keywordRegex != null)
        {
            result = keywordRegex.Replace(result, m => $"\x1b[{cs.Keyword}m{m.Value}\x1b[0m");
        }

        // String literals and comments are applied after and take visual priority —
        // their spans re-colour anything (including keyword highlights) they contain.
        result = StringRegex.Replace(result,  m => $"\x1b[{cs.String}m{m.Value}\x1b[0m");
        result = CommentRegex.Replace(result, m => $"\x1b[{cs.Comment}m{m.Value}\x1b[0m");
        return result;
    }

    private static int VisibleLength(string s) => s.Contains('\x1b') ? AnsiRegex.Replace(s, "").Length : s.Length;

    private static string[] GetKeywords(string lang) => lang.ToLowerInvariant() switch
    {
        "csharp" or "cs" or "c#" => ["using", "namespace", "class", "struct", "interface", "enum", "public", "private", "protected", "internal", "static", "void", "int", "string", "bool", "var", "new", "return", "if", "else", "for", "foreach", "while", "switch", "case", "break", "async", "await", "null", "true", "false", "readonly", "override", "virtual", "abstract", "sealed"],
        "javascript" or "js" or "typescript" or "ts" => ["const", "let", "var", "function", "return", "if", "else", "for", "while", "class", "import", "export", "from", "async", "await", "new", "null", "undefined", "true", "false", "this", "typeof", "interface", "type"],
        "python" or "py" => ["def", "class", "import", "from", "return", "if", "elif", "else", "for", "while", "in", "not", "and", "or", "None", "True", "False", "self", "with", "as", "try", "except", "raise", "yield", "async", "await", "lambda"],
        "rust" or "rs" => ["fn", "let", "mut", "pub", "struct", "enum", "impl", "trait", "use", "mod", "return", "if", "else", "for", "while", "match", "self", "Self", "true", "false", "async", "await", "where", "type"],
        "go" => ["func", "package", "import", "var", "const", "type", "struct", "interface", "return", "if", "else", "for", "range", "switch", "case", "defer", "go", "chan", "map", "nil", "true", "false"],
        _ => []
    };

    private string RenderInlines(ContainerInline? inlines)
    {
        if (inlines == null) return "";
        var sb = new StringBuilder();
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content.ToString());
                    break;
                case EmphasisInline em:
                    var inner = RenderInlines(em);
                    sb.Append(em.DelimiterCount == 2
                        ? $"\x1b[{cs.Bold}m{inner}\x1b[22m"
                        : $"\x1b[3m{inner}\x1b[23m");
                    break;
                case CodeInline code:
                    sb.Append($"\x1b[{cs.InlineCode}m{code.Content}\x1b[0m");
                    break;
                case LinkInline link:
                    var linkText = RenderInlines(link);
                    sb.Append($"\x1b[{cs.Link};4m{linkText}\x1b[0m \x1b[{cs.Comment}m({link.Url})\x1b[0m");
                    break;
                case LineBreakInline:
                    sb.Append('\n');
                    break;
                default:
                    sb.Append(inline);
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
            if (inline is LiteralInline lit) sb.Append(lit.Content.ToString());
            else if (inline is ContainerInline container) sb.Append(GetInlineText(container));
            else sb.Append(inline);
        }
        return sb.ToString();
    }

    private static string GetBlockText(Markdig.Extensions.Tables.TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var block in cell)
            if (block is ParagraphBlock { Inline: not null } p)
                sb.Append(GetInlineText(p.Inline));
        return sb.ToString();
    }

    private static List<string> WrapText(string text, int width)
    {
        if (width <= 0) return [text];
        var result = new List<string>();
        foreach (var segment in text.Split('\n'))
        {
            if (VisibleLength(segment) <= width) { result.Add(segment); continue; }
            var words = segment.Split(' ');
            var current = new StringBuilder();
            int currentLen = 0;
            foreach (var word in words)
            {
                var wLen = VisibleLength(word);
                if (currentLen + wLen + (currentLen > 0 ? 1 : 0) > width && currentLen > 0)
                { result.Add(current.ToString()); current.Clear(); currentLen = 0; }
                if (currentLen > 0) { current.Append(' '); currentLen++; }
                current.Append(word); currentLen += wLen;
            }
            if (current.Length > 0) result.Add(current.ToString());
        }
        return result;
    }
}


record ColorScheme(string Name, string Heading1, string Heading2, string Heading3, string Heading4,
    string Keyword, string String, string Comment, string Link, string InlineCode, string Border, string Bold);

record FilePickerItem(string Path, string DisplayText, bool IsDirectory);
