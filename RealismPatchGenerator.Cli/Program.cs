using PatchGenerator = RealismPatchGenerator.Core.RealismPatchGenerator;

var arguments = args;
if (arguments.Length == 0 || arguments.Any(IsHelpArgument))
{
    PrintUsage();
    return arguments.Length == 0 ? 1 : 0;
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
    var inputPathFilter = CreateInputPathFilter(options);
    var result = generator.Generate(options.OutputPath, inputPathFilter);

    Console.WriteLine($"BasePath: {result.BasePath}");
    Console.WriteLine($"OutputPath: {result.OutputPath}");
    Console.WriteLine($"Seed: {result.UsedSeed}");
    Console.WriteLine($"Weapons: {result.Statistics.WeaponCount}");
    Console.WriteLine($"Attachments: {result.Statistics.AttachmentCount}");
    Console.WriteLine($"Ammo: {result.Statistics.AmmoCount}");
    Console.WriteLine($"Gear: {result.Statistics.GearCount}");
    Console.WriteLine($"Consumables: {result.Statistics.ConsumableCount}");
    Console.WriteLine($"Total: {result.Statistics.TotalCount}");

    if (options.PrintLogs)
    {
        Console.WriteLine();
        Console.WriteLine("Logs:");
        foreach (var log in result.Logs)
        {
            Console.WriteLine(log);
        }
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
    options = new CliOptions();
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

            case "--logs":
            case "-l":
                options.PrintLogs = true;
                break;

            case "--input-file":
            case "-f":
                if (!TryReadValue(arguments, ref index, out var inputFile))
                {
                    errorMessage = "--input-file 缺少路径。";
                    return false;
                }

                options.InputFiles.Add(inputFile);
                break;

            case "--input-dir":
            case "-d":
                if (!TryReadValue(arguments, ref index, out var inputDirectory))
                {
                    errorMessage = "--input-dir 缺少路径。";
                    return false;
                }

                options.InputDirectories.Add(inputDirectory);
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

    if (positional.Count == 0)
    {
        errorMessage = "缺少 basePath 参数。";
        return false;
    }

    if (positional.Count > 2)
    {
        errorMessage = "位置参数过多，最多只接受 basePath 和 outputPath。";
        return false;
    }

    options.BasePath = positional[0];
    options.OutputPath = positional.Count > 1 ? positional[1] : null;
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

static void PrintUsage()
{
    Console.WriteLine("用法:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- <basePath> [outputPath] [--seed <uint>] [--logs] [--input-file <path>] [--input-dir <path>]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output --seed 123456 --logs");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output --input-file user_templates/[3]新武器-SIG_MCX_VIRTUS_items.json");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output --input-dir user_templates");
}

static Func<string, bool>? CreateInputPathFilter(CliOptions options)
{
    if (options.InputFiles.Count == 0 && options.InputDirectories.Count == 0)
    {
        return null;
    }

    var normalizedFiles = options.InputFiles
        .Select(NormalizeInputFilterPath)
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    var normalizedDirectories = options.InputDirectories
        .Select(NormalizeInputFilterPath)
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Select(path => path!.TrimEnd('/'))
        .ToList();

    return relativePath =>
    {
        var normalizedRelativePath = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalizedFiles.Contains(normalizedRelativePath))
        {
            return true;
        }

        foreach (var directory in normalizedDirectories)
        {
            if (normalizedRelativePath.StartsWith(directory + "/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedRelativePath, directory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    };
}

static string NormalizeInputFilterPath(string path)
{
    var normalized = path.Replace('\\', '/').Trim();
    if (normalized.StartsWith("./", StringComparison.Ordinal))
    {
        normalized = normalized[2..];
    }

    if (normalized.StartsWith("input/", StringComparison.OrdinalIgnoreCase))
    {
        normalized = normalized["input/".Length..];
    }

    return normalized.TrimStart('/');
}

file sealed class CliOptions
{
    public string BasePath { get; set; } = string.Empty;
    public string? OutputPath { get; set; }
    public uint? Seed { get; set; }
    public bool PrintLogs { get; set; }
    public List<string> InputFiles { get; } = [];
    public List<string> InputDirectories { get; } = [];
}