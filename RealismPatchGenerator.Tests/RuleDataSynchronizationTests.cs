using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class RuleDataSynchronizationTests : IDisposable
{
    private readonly string basePath;

    public RuleDataSynchronizationTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-rule-sync-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
    }

    [Fact]
    public void DefaultRuleData_StaysInSyncWithRealismItemRules()
    {
        RuleWorkspace.EnsureInitialized(basePath);

        var repoRoot = FindRepositoryRoot();
        var actualRulesDirectory = RuleWorkspace.GetRulesDirectory(repoRoot);
        var generatedRulesDirectory = RuleWorkspace.GetRulesDirectory(basePath);
        var mismatches = new List<string>();

        foreach (var ruleFile in RuleWorkspace.RuleFileNames)
        {
            var actualPath = Path.Combine(actualRulesDirectory, ruleFile);
            var generatedPath = Path.Combine(generatedRulesDirectory, ruleFile);

            Assert.True(File.Exists(actualPath), $"缺少规则文件: {actualPath}");
            Assert.True(File.Exists(generatedPath), $"缺少生成规则文件: {generatedPath}");

            var actualText = File.ReadAllText(actualPath);
            var generatedText = File.ReadAllText(generatedPath);

            Assert.True(
                RuleWorkspace.TryNormalizeRuleFile(ruleFile, actualText, out var normalizedActual, out var actualError),
                $"无法规范化当前规则文件 {ruleFile}: {actualError}");
            Assert.True(
                RuleWorkspace.TryNormalizeRuleFile(ruleFile, generatedText, out var normalizedGenerated, out var generatedError),
                $"无法规范化默认规则文件 {ruleFile}: {generatedError}");

            var actualNode = JsonNode.Parse(normalizedActual)?.AsObject();
            var generatedNode = JsonNode.Parse(normalizedGenerated)?.AsObject();
            if (!JsonNode.DeepEquals(actualNode, generatedNode))
            {
                var differingSections = generatedNode!
                    .Where(pair => !JsonNode.DeepEquals(actualNode?[pair.Key], pair.Value))
                    .Select(pair => pair.Key)
                    .ToArray();
                mismatches.Add($"{ruleFile}: {string.Join(", ", differingSections)}");
            }
        }

        Assert.True(mismatches.Count == 0, "默认 RuleData 与 RealismItemRules 不同步: " + string.Join(" | ", mismatches));
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "RealismPatchGenerator.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("无法定位仓库根目录。");
    }
}