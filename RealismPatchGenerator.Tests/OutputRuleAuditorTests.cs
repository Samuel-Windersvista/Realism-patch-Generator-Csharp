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

    [Fact]
    public void Audit_InfersSplitShotgunAmmoProfiles()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "ammo"));
        var patch = new JsonObject
        {
            ["shell-12g"] = new JsonObject
            {
                ["$type"] = "RealismMod.Ammo, RealismMod",
                ["Name"] = "patron_12x70_slug",
                ["Caliber"] = "12x70",
            },
            ["shell-20g"] = new JsonObject
            {
                ["$type"] = "RealismMod.Ammo, RealismMod",
                ["Name"] = "patron_20x70_buckshot",
                ["Caliber"] = "20x70",
            },
            ["shell-23x75"] = new JsonObject
            {
                ["$type"] = "RealismMod.Ammo, RealismMod",
                ["Name"] = "patron_23x75_shrapnel_10",
                ["Caliber"] = "23x75",
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "ammo", "test_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var file = Assert.Single(report.Files);
        Assert.Contains(file.Items, item => item.ItemId == "shell-12g" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_12g");
        Assert.Contains(file.Items, item => item.ItemId == "shell-20g" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_20g");
        Assert.Contains(file.Items, item => item.ItemId == "shell-23x75" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_23x75");
    }

    [Fact]
    public void Audit_InfersSplitShotgunWeaponCaliberProfiles()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "weapons"));
        var patch = new JsonObject
        {
            ["weapon-12g"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["Name"] = "weapon_mr133_12g",
                ["WeapType"] = "shotgun",
            },
            ["weapon-20g"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["Name"] = "weapon_toz_toz-106_20g",
                ["WeapType"] = "shotgun",
            },
            ["weapon-23x75"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["Name"] = "weapon_toz_ks23m_23x75",
                ["WeapType"] = "shotgun",
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "weapons", "test_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var file = Assert.Single(report.Files);
        Assert.Contains(file.Items, item => item.ItemId == "weapon-12g" && item.Context["caliber_profile"]?.GetValue<string>() == "shotgun_shell_12g");
        Assert.Contains(file.Items, item => item.ItemId == "weapon-20g" && item.Context["caliber_profile"]?.GetValue<string>() == "shotgun_shell_20g");
        Assert.Contains(file.Items, item => item.ItemId == "weapon-23x75" && item.Context["caliber_profile"]?.GetValue<string>() == "shotgun_shell_23x75");
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }
}