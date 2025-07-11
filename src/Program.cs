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
var dict = new Dictionary<string, string>();

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

    var key = string.Join("", keyRaw.Split('-').Select(s => char.ToUpper(s[0]) + s.Substring(1)));

    var value = parts[1].Trim().TrimEnd(';');

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

        var nums = oklchStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        double l = double.Parse(nums[0], CultureInfo.InvariantCulture);
        double c = double.Parse(nums[1], CultureInfo.InvariantCulture);
        double h = double.Parse(nums[2], CultureInfo.InvariantCulture);

        var hex = ColorConverter.OklchToHex(l, c, h);

        Debug.WriteLine($"Converted OKLCH to hex: {hex}");

        hexValue = hex;
    }

    var existingKey = dict.FirstOrDefault(kvp => kvp.Value == hexValue).Key;

    if (!string.IsNullOrEmpty(existingKey))
    {
        Console.WriteLine($"⚠️ Warning: Duplicate color detected for key '{key}'. Using existing key '{existingKey}' with value '{hexValue}'.");
    }
    
    dict.TryAdd(key, hexValue);
}

dict.TryAdd("Black", "#000000");
dict.TryAdd("White", "#FFFFFF");

var sb = new System.Text.StringBuilder();
sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
sb.AppendLine("<!--");
sb.AppendLine("    Tailwind → XAML Color Resource Dictionary");
sb.AppendLine($"    Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
sb.AppendLine("    Generated by tailwind2xaml tool (example).");
sb.AppendLine("-->");
sb.AppendLine(header);
sb.AppendLine();

foreach (var kvp in dict.OrderBy(k => k.Key))
{
    var line = $"  <Color x:Key=\"{kvp.Key}\">{kvp.Value}</Color>";

    Debug.WriteLine($"Adding line: {line}");

    sb.AppendLine(line);
}

sb.AppendLine();
sb.AppendLine("</ResourceDictionary>");

File.WriteAllText(outputPath, sb.ToString());

Console.WriteLine($"✅ Colors.xaml generated at: {outputPath}");