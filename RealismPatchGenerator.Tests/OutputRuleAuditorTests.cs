using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class OutputRuleAuditorTests : IDisposable
{
    private readonly string basePath;

    public OutputRuleAuditorTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-audit-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "input"));
        Directory.CreateDirectory(Path.Combine(basePath, "output"));
        Directory.CreateDirectory(Path.Combine(basePath, "现实主义物品模板"));
    }

    [Fact]
    public void Audit_ReturnsWarning_ForInvalidJsonFile()
    {
        File.WriteAllText(Path.Combine(basePath, "output", "broken_realism_patch.json"), "{not valid json");

        var auditor = new OutputRuleAuditor(basePath);
        var report = auditor.Audit();

        Assert.Equal(1, report.FileCount);
        Assert.Equal(1, report.WarningCount);
        Assert.Equal(0, report.ViolationCount);
        Assert.Contains(report.Files, file => file.Warnings.Contains("文件无法解析为 JSON，已跳过"));
    }

    [Fact]
    public void Audit_FlagsWeaponViolations_ForOutOfRangeAndPistolRules()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "weapons"));
        var patch = new JsonObject
        {
            ["test-item"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["Name"] = "Unit Test Pistol",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = true,
                ["RecoilAngle"] = 10,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "weapons", "test_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        Assert.Equal(1, report.FileCount);
        Assert.Equal(1, report.ItemCount);
        Assert.True(report.ViolationCount >= 2);

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Field == "RecoilAngle");
        Assert.Contains(item.Violations, violation => violation.Field == "HasShoulderContact");
        Assert.Equal("pistol", item.Context["weapon_profile"]?.GetValue<string>());
    }

    [Fact]
    public void Audit_IgnoresNonPatchJson_UnlessIncludeTemplateExportsEnabled()
    {
        File.WriteAllText(Path.Combine(basePath, "output", "plain.json"), "{}");

        var defaultAuditor = new OutputRuleAuditor(basePath);
        var defaultReport = defaultAuditor.Audit();

        Assert.Equal(0, defaultReport.FileCount);

        var expandedAuditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeTemplateExports = true });
        var expandedReport = expandedAuditor.Audit();

        Assert.Equal(1, expandedReport.FileCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }
}