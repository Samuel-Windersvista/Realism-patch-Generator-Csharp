using RealismPatchGenerator.Core;
using PatchGenerator = RealismPatchGenerator.Core.RealismPatchGenerator;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var command = args.Length > 0 && IsCommand(args[0]) ? args[0].ToLowerInvariant() : "generate";
var commandArgs = command == "generate" && args.Length > 0 && !IsCommand(args[0])
	? args
	: args.Skip(1).ToArray();

var requestedBasePath = commandArgs.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
var applicationRoot = WorkspaceLocator.FindApplicationRoot(
	Directory.GetCurrentDirectory(),
	AppContext.BaseDirectory,
	Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

var repositoryRoot = WorkspaceLocator.FindDataRoot(
	requestedBasePath,
	applicationRoot);

if (repositoryRoot is null)
{
	Console.Error.WriteLine($"未找到 C# 程序数据目录。请在 {(applicationRoot ?? "当前目录")} 下放置 input 和 现实主义物品模板，或显式传入该目录路径。");
	return 1;
}

try
{
	return command switch
	{
		"audit" => RunAudit(repositoryRoot, commandArgs),
		_ => RunGenerate(repositoryRoot, commandArgs),
	};
}
catch (Exception ex)
{
	Console.Error.WriteLine($"生成失败: {ex.Message}");
	return 1;
}

static int RunGenerate(string repositoryRoot, string[] args)
{
	var outputDirectory = GetOptionValue(args, "--output-dir");
	if (!TryGetUIntOption(args, "--seed", out var seedValue, out var seedProvided))
	{
		Console.Error.WriteLine("参数错误: --seed 必须是 0 到 4294967295 之间的无符号整数。");
		return 1;
	}

	Console.WriteLine("============================================================");
	Console.WriteLine("EFT 现实主义数值生成器 C# 版");
	Console.WriteLine("============================================================");
	Console.WriteLine($"数据目录: {repositoryRoot}");
	if (seedProvided)
	{
		Console.WriteLine($"指定随机种子: {seedValue}");
	}

	var generator = seedProvided
		? new PatchGenerator(repositoryRoot, seedValue)
		: new PatchGenerator(repositoryRoot);
	var result = generator.Generate(outputDirectory);

	foreach (var log in result.Logs)
	{
		Console.WriteLine(log);
	}

	Console.WriteLine();
	Console.WriteLine("生成统计:");
	Console.WriteLine($"  武器补丁: {result.Statistics.WeaponCount}");
	Console.WriteLine($"  配件补丁: {result.Statistics.AttachmentCount}");
	Console.WriteLine($"  子弹补丁: {result.Statistics.AmmoCount}");
	Console.WriteLine($"  装备补丁: {result.Statistics.GearCount}");
	Console.WriteLine($"  消耗品补丁: {result.Statistics.ConsumableCount}");
	Console.WriteLine($"  总计: {result.Statistics.TotalCount}");
	Console.WriteLine($"输出目录: {result.OutputPath}");
	return 0;
}

static int RunAudit(string repositoryRoot, string[] args)
{
	var outputDirectory = GetOptionValue(args, "--output-dir");
	var reportFile = GetOptionValue(args, "--report-file") ?? Path.Combine("audit_reports", "output_rule_audit.json");
	var includeOk = HasFlag(args, "--include-ok");
	var includeTemplateExports = HasFlag(args, "--include-template-exports");
	var failOnViolations = HasFlag(args, "--fail-on-violations");
	var summaryLimit = TryGetIntOption(args, "--summary-limit", 30);

	Console.WriteLine("============================================================");
	Console.WriteLine("EFT 现实主义输出审计 C# 版");
	Console.WriteLine("============================================================");
	Console.WriteLine($"数据目录: {repositoryRoot}");

	var auditor = new OutputRuleAuditor(repositoryRoot, new OutputAuditOptions
	{
		OutputDirectory = outputDirectory is null ? null : ResolvePath(repositoryRoot, outputDirectory),
		IncludeOk = includeOk,
		IncludeTemplateExports = includeTemplateExports,
	});
	var report = auditor.Audit();
	Console.WriteLine(OutputRuleAuditor.BuildConsoleSummary(report, summaryLimit));

	var resolvedReportFile = ResolvePath(repositoryRoot, reportFile);
	Directory.CreateDirectory(Path.GetDirectoryName(resolvedReportFile)!);
	File.WriteAllText(resolvedReportFile, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
	{
		WriteIndented = true,
	}));
	Console.WriteLine($"报告已写入: {resolvedReportFile}");

	if (failOnViolations && report.ViolationCount > 0)
	{
		return 2;
	}

	return 0;
}

static bool IsCommand(string value)
{
	return string.Equals(value, "generate", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(value, "audit", StringComparison.OrdinalIgnoreCase);
}

static bool HasFlag(IEnumerable<string> args, string flag)
{
	return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
}

static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
{
	for (var index = 0; index < args.Count - 1; index++)
	{
		if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
		{
			return args[index + 1];
		}
	}

	return null;
}

static int TryGetIntOption(IReadOnlyList<string> args, string optionName, int fallback)
{
	var rawValue = GetOptionValue(args, optionName);
	return int.TryParse(rawValue, out var parsed) ? parsed : fallback;
}

static bool TryGetUIntOption(IReadOnlyList<string> args, string optionName, out uint value, out bool provided)
{
	var rawValue = GetOptionValue(args, optionName);
	provided = !string.IsNullOrWhiteSpace(rawValue);
	if (!provided)
	{
		value = default;
		return true;
	}

	return uint.TryParse(rawValue, out value);
}

static string ResolvePath(string repositoryRoot, string path)
{
	return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(repositoryRoot, path));
}
