using System.Diagnostics;
using System.Globalization;
using TailwindXamlThemeConverter;
using TwToXaml;

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
    {
        Console.WriteLine("Tailwind2Xaml - Tailwind OKLCH theme to XAML ResourceDictionary converter");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet tailwind2xaml <inputFile> [outputFile]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  <inputFile>    Required. Path to Tailwind theme file with color definitions.");
        Console.WriteLine("  [outputFile]   Optional. Path for the generated XAML file. Defaults to 'Colors.xaml' in input file's directory.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help, -h     Show this help message and exit.");
        Console.WriteLine("  --target, -t   Target XAML framework. Options: maui (default), wpf, uwp, uno, avalonia.");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  dotnet tailwind2xaml theme.txt MyColors.xaml");
        return;
    }

var inputPath = args[0];

if (!File.Exists(inputPath))
{
    Console.WriteLine($"❌ File not found: {inputPath}");
    return;
}

var outputPath = string.Empty;

if (args.Length >= 2)
{
    for (int n = 1; n < args.Length; n++)
    {
        var arg = args[n];
        var previousArg = n > 1 ? args[n - 1] : string.Empty;
        if (previousArg == "--target" || previousArg == "-t")
        {
            // Skip this argument, it's handled by the target option
            continue;
        }
        else if (arg.StartsWith("--") || arg.StartsWith("-"))
        {
            // This is an option, skip it
            continue;
        }
        else
        {
            outputPath = arg;
            break; // Stop after the first non-option argument
        }
    }
}

if (string.IsNullOrEmpty(outputPath))
{
    outputPath = Path.Combine(Path.GetDirectoryName(inputPath) ?? ".", "Colors.xaml");
}
else if (Path.GetExtension(outputPath).ToLower() != ".xaml")
{
    Console.WriteLine($"❌ Invalid output file extension: {outputPath}. Expected .xaml");
    return;
}

// Defaults
string target = "maui"; // sensible default

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--target" || args[i] == "-t")
    {
        if (i + 1 < args.Length)
        {
            target = args[i + 1].ToLower();
        }
    }
}

var validTargets = new[] { "maui", "wpf", "uwp", "uno", "avalonia" };

if (!validTargets.Contains(target))
{
    Console.WriteLine($"❌ Invalid target: {target}");
    Console.WriteLine("Valid targets: maui, wpf, uwp, uno, avalonia");
    return;
}

string header = target switch
{
    "maui" => Headers.Maui,
    "wpf" => Headers.Wpf,
    "uwp" => Headers.Uwp,
    "uno" => Headers.Uno,
    "avalonia" => Headers.Avalonia,
    _ => Headers.Maui
};


var raw = File.ReadAllText(inputPath);
var lines = raw.Split('\n');

// Two output layers, emitted into a single dictionary:
//   palette  -> distinct literal <Color> resources (paletteKey -> hex)
//   brushes  -> semantic <SolidColorBrush> resources that reference the palette
//               via DynamicResource (brushKey -> paletteKey)
var palette = new Dictionary<string, string>();
var brushes = new Dictionary<string, string>();

// First pass: collect every custom property definition (--name: value) into a
// symbol table so that var(...) references can be resolved regardless of where
// they appear relative to their definition.
var symbols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

foreach (var line in lines)
{
    var trimmed = line.Trim();

    if (!trimmed.StartsWith("--"))
        continue;

    var defParts = trimmed.Split(':', 2);
    if (defParts.Length < 2)
        continue;

    var name = defParts[0].Trim().TrimStart('-');
    var defValue = defParts[1].Trim().TrimEnd(';').Trim();

    // Last definition wins, mirroring the CSS cascade.
    symbols[name] = defValue;
}

// Recursively resolve a value down to a concrete color, following var(...)
// references (including chained ones) and honouring var(--x, fallback) syntax.
string ResolveVariables(string value, int depth = 0)
{
    value = value.Trim();

    if (depth > 16 || !value.StartsWith("var("))
        return value;

    var inner = value.Substring("var(".Length);
    var close = inner.LastIndexOf(')');
    if (close >= 0)
        inner = inner.Substring(0, close);
    inner = inner.Trim();

    var reference = inner;
    string? fallback = null;

    var comma = inner.IndexOf(',');
    if (comma >= 0)
    {
        reference = inner.Substring(0, comma).Trim();
        fallback = inner.Substring(comma + 1).Trim();
    }

    reference = reference.TrimStart('-');

    if (symbols.TryGetValue(reference, out var resolved))
        return ResolveVariables(resolved, depth + 1);

    if (fallback is not null)
        return ResolveVariables(fallback, depth + 1);

    Console.WriteLine($"⚠️ Warning: Unresolved variable reference 'var(--{reference})'.");

    return value;
}

// The immediate variable a value references (e.g. "var(--primary)" -> "primary"),
// or null if the value is a literal. This becomes the palette key, so the semantic
// brush keeps pointing at the named base color rather than an anonymous literal.
string? FirstVarReference(string value)
{
    value = value.Trim();

    if (!value.StartsWith("var("))
        return null;

    var inner = value.Substring("var(".Length);
    var close = inner.LastIndexOf(')');
    if (close >= 0)
        inner = inner.Substring(0, close);

    var comma = inner.IndexOf(',');
    if (comma >= 0)
        inner = inner.Substring(0, comma);

    return inner.Trim().TrimStart('-');
}

// Convert a kebab-case custom-property name into a PascalCase resource key.
string Pascal(string raw) =>
    string.Join("", raw.Split('-')
        .Where(s => s.Length > 0)
        .Select(s => char.ToUpper(s[0]) + s.Substring(1)));

// Second pass: emit colors, resolving any variable references as we go.
foreach (var line in lines)
{
    var trimmed = line.Trim();

    Debug.WriteLine($"Processing: {trimmed}");

    if (!trimmed.StartsWith("--color-"))
    {
        Debug.WriteLine($"Skipping line: {trimmed} (does not start with --color-)");

        continue;
    }

    var parts = trimmed.Split(':', 2);
    if (parts.Length < 2)
    {
        Debug.WriteLine($"Skipping line: {trimmed} (does not contain a colon)");

        continue;
    }

    var keyRaw = parts[0].Replace("--color-", "").Trim();

    Debug.WriteLine($"Key raw: {keyRaw}");

    var key = Pascal(keyRaw);

    var rawValue = parts[1].Trim().TrimEnd(';');
    var baseRef = FirstVarReference(rawValue);
    var value = ResolveVariables(rawValue);

    var hexValue = string.Empty;

    Debug.WriteLine($"Key: {key}, Value: {value}");

    if (value.StartsWith('#'))
    {
        Debug.WriteLine($"Hex color detected: {value}");

        hexValue = value.ToUpper();
    }
    else if (value.StartsWith("oklch"))
    {
        Debug.WriteLine($"OKLCH color detected: {value}");

        var oklchStr = value.Replace("oklch(", "").Replace(")", "").Trim();

        Debug.WriteLine($"OKLCH string: {oklchStr}");

        // Split off an optional alpha component, e.g. "1 0 0 / 10%" or "1 0 0 / 0.1".
        double alpha = 1.0;
        var slashIndex = oklchStr.IndexOf('/');
        if (slashIndex >= 0)
        {
            var alphaStr = oklchStr.Substring(slashIndex + 1).Trim();
            oklchStr = oklchStr.Substring(0, slashIndex).Trim();

            alpha = alphaStr.EndsWith("%")
                ? double.Parse(alphaStr.TrimEnd('%'), CultureInfo.InvariantCulture) / 100.0
                : double.Parse(alphaStr, CultureInfo.InvariantCulture);

            Debug.WriteLine($"OKLCH alpha: {alpha}");
        }

        var nums = oklchStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        double l = double.Parse(nums[0], CultureInfo.InvariantCulture);
        double c = double.Parse(nums[1], CultureInfo.InvariantCulture);
        double h = double.Parse(nums[2], CultureInfo.InvariantCulture);

        var hex = ColorConverter.OklchToHex(l, c, h, alpha);

        Debug.WriteLine($"Converted OKLCH to hex: {hex}");

        hexValue = hex;
    }

    // Palette key: the named base color when this was a var() reference (so distinct
    // semantic roles that share a base resolve to one palette slot), otherwise the
    // color's own name when it's a direct literal (e.g. a raw Tailwind scale entry).
    var paletteKey = baseRef is not null ? Pascal(baseRef) : key;

    if (palette.TryGetValue(paletteKey, out var existing) && existing != hexValue)
    {
        Console.WriteLine($"⚠️ Warning: Palette key '{paletteKey}' redefined ('{existing}' -> '{hexValue}'). Keeping first value.");
    }
    else
    {
        palette[paletteKey] = hexValue;
    }

    // Semantic brush points at the palette color via DynamicResource.
    brushes[$"{key}Brush"] = paletteKey;
}

palette.TryAdd("Black", "#000000");
palette.TryAdd("White", "#FFFFFF");

var sb = new System.Text.StringBuilder();
sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
sb.AppendLine("<!--");
sb.AppendLine("    Tailwind → XAML Color Resource Dictionary");
sb.AppendLine($"    Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine("    Generated by tailwind2xaml tool (example).");
sb.AppendLine("-->");
sb.AppendLine(header);
sb.AppendLine();

// Palette layer: the raw, individually-tweakable colors.
sb.AppendLine("  <!-- Palette: raw colors. Tweak these to retune the theme. -->");

foreach (var kvp in palette.OrderBy(k => k.Key))
{
    var line = $"  <Color x:Key=\"{kvp.Key}\">{kvp.Value}</Color>";

    Debug.WriteLine($"Adding line: {line}");

    sb.AppendLine(line);
}

// Semantic layer: named roles as brushes that reference the palette via
// DynamicResource, so swapping a palette color at runtime retints every role.
if (brushes.Count > 0)
{
    sb.AppendLine();
    sb.AppendLine("  <!-- Semantic brushes: bind to these; they follow the palette above. -->");

    foreach (var kvp in brushes.OrderBy(k => k.Key))
    {
        var line = $"  <SolidColorBrush x:Key=\"{kvp.Key}\" Color=\"{{DynamicResource {kvp.Value}}}\" />";

        Debug.WriteLine($"Adding line: {line}");

        sb.AppendLine(line);
    }
}

sb.AppendLine();
sb.AppendLine("</ResourceDictionary>");

File.WriteAllText(outputPath, sb.ToString());

Console.WriteLine($"✅ Resource dictionary generated at: {outputPath}");