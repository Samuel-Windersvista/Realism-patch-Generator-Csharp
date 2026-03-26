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
    var result = generator.Generate(options.OutputPath);

    Console.WriteLine($"BasePath: {result.BasePath}");
    Console.WriteLine($"OutputPath: {result.OutputPath}");
    Console.WriteLine($"Seed: {result.UsedSeed}");
    Console.WriteLine($"Weapons: {result.Statistics.WeaponCount}");
    Console.WriteLine($"Attachments: {result.Statistics.AttachmentCount}");
    Console.WriteLine($"Ammo: {result.Statistics.AmmoCount}");
    Console.WriteLine($"Gear: {result.Statistics.GearCount}");
    Console.WriteLine($"Consumables: {result.Statistics.ConsumableCount}");
    Console.WriteLine($"Total: {result.Statistics.TotalCount}");

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

static void PrintUsage()
{
    Console.WriteLine("用法:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- [basePath] [outputPath] [--seed <uint>]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output");
    Console.WriteLine("  dotnet run --project .\\RealismPatchGenerator.Cli -- . .\\output --seed 123456");
}

file sealed class CliOptions
{
    public string BasePath { get; set; } = ".";
    public string? OutputPath { get; set; }
    public uint? Seed { get; set; }
}