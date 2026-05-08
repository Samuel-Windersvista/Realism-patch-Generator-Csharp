using PatchGenerator = RealismPatchGenerator.Core.RealismPatchGenerator;

var arguments = args;
if (arguments.Any(IsHelpArgument))
{
    PrintUsage();
    return 0;
}

var parseResult = TryParseArguments(arguments, out var options, out var errorMessage);
if (!parseResult)
{
    Console.Error.WriteLine(errorMessage);
    Console.Error.WriteLine();
    PrintUsage();
    return 1;
}

try
{
    var generator = new PatchGenerator(options.BasePath, options.Seed);
    var result = generator.Generate(options.OutputPath, CreateInputPathFilter(options.IncludePatterns));

    Console.WriteLine($"BasePath: {result.BasePath}");
    Console.WriteLine($"OutputPath: {result.OutputPath}");
    Console.WriteLine($"Seed: {result.UsedSeed}");
    if (options.IncludePatterns.Count > 0)
    {
        Console.WriteLine($"Includes: {string.Join(", ", options.IncludePatterns)}");
    }
    Console.WriteLine($"Weapons: {result.Statistics.WeaponCount}");
    Console.WriteLine($"Attachments: {result.Statistics.AttachmentCount}");
    Console.WriteLine($"Ammo: {result.Statistics.AmmoCount}");
    Console.WriteLine($"Gear: {result.Statistics.GearCount}");
    Console.WriteLine($"Consumables: {result.Statistics.ConsumableCount}");
    Console.WriteLine($"Total: {result.Statistics.TotalCount}");
    Console.WriteLine($"InputFiles: {result.Performance.InputFileCount}");
    Console.WriteLine($"ProcessedFiles: {result.Performance.ProcessedFileCount}");
    Console.WriteLine($"TotalMs: {result.Performance.TotalDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"TemplateLoadMs: {result.Performance.TemplateLoadDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"InputDiscoveryMs: {result.Performance.InputDiscoveryDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"FileProcessingMs: {result.Performance.FileProcessingDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"RuleApplicationMs: {result.Performance.RuleApplicationDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"OutputWriteMs: {result.Performance.OutputWriteDuration.TotalMilliseconds:F2}");
    Console.WriteLine($"AllocatedMB: {result.Performance.AllocatedBytes / 1024d / 1024d:F2}");
    Console.WriteLine($"Gen0Collections: {result.Performance.Gen0Collections}");
    Console.WriteLine($"Gen1Collections: {result.Performance.Gen1Collections}");
    Console.WriteLine($"Gen2Collections: {result.Performance.Gen2Collections}");
    if (!string.IsNullOrWhiteSpace(result.Performance.SlowestInputFile))
    {
        Console.WriteLine($"SlowestInputFile: {result.Performance.SlowestInputFile}");
        Console.WriteLine($"SlowestInputFileMs: {result.Performance.SlowestInputFileDuration.TotalMilliseconds:F2}");
    }

    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine("生成失败:");
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static bool TryParseArguments(string[] arguments, out CliOptions options, out string errorMessage)
{
    options = new CliOptions
    {
        BasePath = ".",
    };
    errorMessage = string.Empty;

    var positional = new List<string>();
    for (var index = 0; index < arguments.Length; index++)
    {
        var argument = arguments[index];
        switch (argument)
        {
            case "--seed":
            case "-s":
                if (!TryReadValue(arguments, ref index, out var seedValue))
                {
                    errorMessage = "--seed 缺少数值。";
                    return false;
                }

                if (!uint.TryParse(seedValue, out var seed))
                {
                    errorMessage = $"无效的 seed: {seedValue}";
                    return false;
                }

                options.Seed = seed;
                break;

            case "--include":
            case "-i":
                if (!TryReadValue(arguments, ref index, out var includeValue))
                {
                    errorMessage = "--include 缺少路径片段。";
                    return false;
                }

                options.IncludePatterns.Add(NormalizeIncludePattern(includeValue));
                break;

            default:
                if (argument.StartsWith("-", StringComparison.Ordinal))
                {
                    errorMessage = $"未知参数: {argument}";
                    return false;
                }

                positional.Add(argument);
                break;
        }
    }

    if (positional.Count > 2)
    {
        errorMessage = "位置参数过多，最多只接受 basePath 和 outputPath。";
        return false;
    }

    if (positional.Count >= 1)
    {
        options.BasePath = positional[0];
    }

    if (positional.Count >= 2)
    {
        options.OutputPath = positional[1];
    }

    return true;
}

static bool TryReadValue(string[] arguments, ref int index, out string value)
{
    if (index + 1 < arguments.Length)
    {
        index++;
        value = arguments[index];
        return true;
    }

    value = string.Empty;
    return false;
}

static bool IsHelpArgument(string argument)
{
    return argument.Equals("--help", StringComparison.OrdinalIgnoreCase)
        || argument.Equals("-h", StringComparison.OrdinalIgnoreCase)
        || argument.Equals("/?", StringComparison.OrdinalIgnoreCase);
}

static Func<string, bool>? CreateInputPathFilter(IReadOnlyList<string> includePatterns)
{
    if (includePatterns.Count == 0)
    {
        return null;
    }

    return path =>
    {
        var normalized = NormalizeIncludePattern(path);
        return includePatterns.Any(pattern => normalized.Equals(pattern, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase));
    };
}

static string NormalizeIncludePattern(string value)
{
    return value.Replace('\\', '/').Trim().TrimStart('.', '/');
}

static void PrintUsage()
{
    Console.WriteLine("用法:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- [basePath] [outputPath] [--seed <uint>] [--include <path>]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output --seed 123456");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\artifacts\\perf-small --seed 123456 --include user_templates/file.json");
}

file sealed class CliOptions
{
    public string BasePath { get; set; } = ".";
    public string? OutputPath { get; set; }
    public uint? Seed { get; set; }
    public List<string> IncludePatterns { get; } = [];
}