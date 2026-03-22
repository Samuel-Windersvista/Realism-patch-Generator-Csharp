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
        Directory.CreateDirectory(RuleWorkspace.GetTemplatesDirectory(basePath));
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

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true, IncludeTemplateExports = true });
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

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true, IncludeTemplateExports = true });
        var report = auditor.Audit();

        var file = Assert.Single(report.Files);
        Assert.Contains(file.Items, item => item.ItemId == "shell-12g" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_12g");
        Assert.Contains(file.Items, item => item.ItemId == "shell-20g" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_20g");
        Assert.Contains(file.Items, item => item.ItemId == "shell-23x75" && item.Context["ammo_profile"]?.GetValue<string>() == "shotgun_shell_23x75");
    }

    [Fact]
    public void Audit_FlagsAmmoStructureDeviation()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "ammo"));
        var patch = new JsonObject
        {
            ["ammo-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.Ammo, RealismMod",
                ["Name"] = "patron_127x33_jsp",
                ["Damage"] = 100,
                ["PenetrationPower"] = 50,
                ["LoyaltyLevel"] = 1,
                ["BasePriceModifier"] = 1,
                ["ItemID"] = "ammo-test",
                ["InitialSpeed"] = 800,
                ["BulletMassGram"] = 10,
                ["BallisticCoeficient"] = 0.5,
                ["Prefab"] = new JsonObject
                {
                    ["path"] = "Ammo/custom.bundle",
                    ["rcid"] = "",
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "ammo", "test_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "ammo_structure" && violation.Field == "Weight");
        Assert.Contains(item.Violations, violation => violation.Rule == "ammo_structure" && violation.Field == "Prefab");
    }

    [Fact]
    public void Audit_FlagsMissingStockAdapterFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["stock-adapter-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "stock_adapter_test",
                ["ModType"] = "stock_adapter",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["CameraRecoil"] = 0,
                ["Convergence"] = 0,
                ["AimSpeed"] = 0,
                ["AimStability"] = 0,
                ["Handling"] = 0,
                ["Ergonomics"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_stock_adapter_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "stock_adapter_structure" && violation.Field == "DurabilityBurnModificator");
        Assert.Contains(item.Violations, violation => violation.Rule == "stock_adapter_structure" && violation.Field == "Loudness");
    }

    [Fact]
    public void Audit_FlagsMissingGasblockFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["gasblock-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "gasblock_test",
                ["ModType"] = "gasblock",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["Ergonomics"] = 0,
                ["HeatFactor"] = 1.0,
                ["CoolFactor"] = 1.0,
                ["DurabilityBurnModificator"] = 1.0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_gasblock_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "gasblock_structure" && violation.Field == "Loudness");
        Assert.Contains(item.Violations, violation => violation.Rule == "gasblock_structure" && violation.Field == "Velocity");
    }

    [Fact]
    public void Audit_FlagsMissingHandguardFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["handguard-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "handguard_m60_usord_m60e4_mod1",
                ["ModType"] = "handguard_medium",
                ["VerticalRecoil"] = -6,
                ["HorizontalRecoil"] = -3,
                ["HeatFactor"] = 1.0,
                ["CoolFactor"] = 1.0,
                ["AimStability"] = 8,
                ["AimSpeed"] = 2,
                ["Handling"] = 4,
                ["Ergonomics"] = 3,
                ["DurabilityBurnModificator"] = 1.0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_handguard_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "handguard_structure" && violation.Field == "Accuracy");
        Assert.Contains(item.Violations, violation => violation.Rule == "handguard_structure" && violation.Field == "Dispersion");
    }

    [Fact]
    public void Audit_FlagsMissingForegripAccuracy()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["foregrip-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "foregrip_sr3m_tochmash_sr3m_std",
                ["ModType"] = "foregrip",
                ["VerticalRecoil"] = -5,
                ["HorizontalRecoil"] = -3,
                ["Dispersion"] = 0,
                ["AimSpeed"] = 4,
                ["Ergonomics"] = 7,
                ["Handling"] = 13,
                ["CameraRecoil"] = -4,
                ["Convergence"] = -1,
                ["AimStability"] = 11,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_foregrip_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "foregrip_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingIronSightAccuracy()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["iron-sight-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "sight_rear_ak_tactica_tula_tt01",
                ["ModType"] = "iron_sight",
                ["AimSpeed"] = 1,
                ["Ergonomics"] = 1,
                ["Weight"] = 0.08,
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["LoyaltyLevel"] = 1,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_iron_sight_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "iron_sight_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingPistolGripDispersion()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["pistol-grip-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "pistolgrip_deagle_hogue_rubber_grips_ergo",
                ["ModType"] = "pistol_grip",
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["AimSpeed"] = 2,
                ["Ergonomics"] = 5,
                ["Accuracy"] = 0,
                ["Weight"] = 0.12,
                ["Handling"] = 7,
                ["LoyaltyLevel"] = 3,
                ["AimStability"] = 3,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_pistol_grip_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "pistol_grip_structure" && violation.Field == "Dispersion");
    }

    [Fact]
    public void Audit_FlagsMissingUbglFieldsInsteadOfUnsupportedWarning()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "attatchments"));
        var patch = new JsonObject
        {
            ["ubgl-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "launcher_ar15_colt_m203_40x46",
                ["ModType"] = "UBGL",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["Dispersion"] = 0,
                ["AimSpeed"] = 0,
                ["Ergonomics"] = 0,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["ModMalfunctionChance"] = 0,
                ["Weight"] = 1.36,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["LoyaltyLevel"] = 4,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "attatchments", "UBGLTempaltes.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true, IncludeTemplateExports = true });
        var report = auditor.Audit();

        var file = Assert.Single(report.Files.Where(file => file.File.EndsWith("UBGLTempaltes.json", StringComparison.OrdinalIgnoreCase)));
        var item = Assert.Single(file.Items);
        Assert.Equal("violation", item.Status);
        Assert.DoesNotContain(item.WarningDetails, detail => detail.Category == "unsupported_ubgl");
        Assert.Contains(item.Violations, violation => violation.Rule == "ubgl_structure" && violation.Field == "Accuracy");
        Assert.Contains(item.Violations, violation => violation.Rule == "ubgl_structure" && violation.Field == "CameraRecoil");
        Assert.Contains(item.Violations, violation => violation.Rule == "ubgl_structure" && violation.Field == "HasShoulderContact");
        Assert.Contains(item.Violations, violation => violation.Rule == "ubgl_structure" && violation.Field == "StockAllowADS");
    }

    [Fact]
    public void Audit_FlagsMissingReceiverChamberSpeed()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["receiver-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "reciever_pl15_izhmash_pl15_std",
                ["ModType"] = "receiver",
                ["AutoROF"] = 1,
                ["SemiROF"] = 2,
                ["ModMalfunctionChance"] = 2,
                ["Accuracy"] = -1,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["Ergonomics"] = 6,
                ["Convergence"] = 8,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_receiver_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "receiver_structure" && violation.Field == "ChamberSpeed");
    }

    [Fact]
    public void Audit_FlagsMissingBayonetFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["bayonet-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "m9_bayonet",
                ["ModType"] = "bayonet",
                ["VerticalRecoil"] = -4,
                ["HorizontalRecoil"] = -2,
                ["Dispersion"] = 0,
                ["CameraRecoil"] = -1,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["ModMalfunctionChance"] = 0,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["DurabilityBurnModificator"] = 1,
                ["Velocity"] = 0,
                ["RecoilAngle"] = 0,
                ["Convergence"] = 0,
                ["LoyaltyLevel"] = 1,
                ["AimSpeed"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_bayonet_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "bayonet_structure" && violation.Field == "Accuracy");
        Assert.Contains(item.Violations, violation => violation.Rule == "bayonet_structure" && violation.Field == "MeleeDamage");
        Assert.Contains(item.Violations, violation => violation.Rule == "bayonet_structure" && violation.Field == "MeleePen");
        Assert.Contains(item.Violations, violation => violation.Rule == "bayonet_structure" && violation.Field == "Flash");
    }

    [Fact]
    public void Audit_FlagsMissingTriggerFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["trigger-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "trigger_m1911_caspian_trik_trigger",
                ["ModType"] = "trigger",
                ["ModMalfunctionChance"] = 1,
                ["Ergonomics"] = -1,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_trigger_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "trigger_structure" && violation.Field == "SemiROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "trigger_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingMountFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["mount-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "mount_mk16_pws_srx",
                ["ModType"] = "mount",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["Dispersion"] = 0,
                ["Ergonomics"] = 0,
                ["AimStability"] = 0,
                ["Handling"] = 0,
                ["Accuracy"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_mount_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "mount_structure" && violation.Field == "HeatFactor");
        Assert.Contains(item.Violations, violation => violation.Rule == "mount_structure" && violation.Field == "CoolFactor");
        Assert.Contains(item.Violations, violation => violation.Rule == "mount_structure" && violation.Field == "AimSpeed");
        Assert.Contains(item.Violations, violation => violation.Rule == "mount_structure" && violation.Field == "DurabilityBurnModificator");
    }

    [Fact]
    public void Audit_FlagsMissingHammerFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));
        var patch = new JsonObject
        {
            ["hammer-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "hammer_m1911_sti_hex",
                ["ModType"] = "hammer",
                ["ModMalfunctionChance"] = 1,
                ["Ergonomics"] = -1,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_hammer_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "hammer_structure" && violation.Field == "SemiROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "hammer_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingBarrelShotgunDispersion()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["barrel-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "barrel_m60_usord_e3_long_584mm_762x51",
                ["ModType"] = "barrel",
                ["VerticalRecoil"] = -6,
                ["HorizontalRecoil"] = -5,
                ["Dispersion"] = -8,
                ["CenterOfImpact"] = 0.01,
                ["Velocity"] = 15,
                ["Accuracy"] = 15,
                ["HeatFactor"] = 0.9,
                ["CoolFactor"] = 1.15,
                ["Convergence"] = -15,
                ["DurabilityBurnModificator"] = 0.7,
                ["RecoilAngle"] = -15,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_barrel_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "barrel_structure" && violation.Field == "ShotgunDispersion");
    }

    [Fact]
    public void Audit_FlagsMissingChargingHandleReloadSpeed()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["charging-handle-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "charge_ax_accuracy_internation_axmc_762x51_pb",
                ["ModType"] = "charging_handle",
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["ChamberSpeed"] = 18,
                ["ModMalfunctionChance"] = 1,
                ["Ergonomics"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_charging_handle_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "charging_handle_structure" && violation.Field == "ReloadSpeed");
    }

    [Fact]
    public void Audit_FlagsMissingFlashlightLaserRecoilFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["flashlight-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "flashlight_armytek_predator_pro_v3_xhp35_hi",
                ["ModType"] = "flashlight",
                ["Ergonomics"] = 0,
                ["Handling"] = -3,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_flashlight_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "flashlight_laser_structure" && violation.Field == "VerticalRecoil");
        Assert.Contains(item.Violations, violation => violation.Rule == "flashlight_laser_structure" && violation.Field == "HorizontalRecoil");
    }

    [Fact]
    public void Audit_FlagsMissingRedDotScopeAccuracy()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["red-dot-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "scope_base_trijicon_rmr",
                ["ModType"] = "sight",
                ["AimSpeed"] = 4,
                ["AimStability"] = 2,
                ["Ergonomics"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_scope_red_dot_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "scope_red_dot_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingMagnifiedScopeAccuracy()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["magnified-scope-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "scope_all_trijicon_acog_ta01nsn_4x32",
                ["ModType"] = "sight",
                ["AimSpeed"] = -4,
                ["AimStability"] = 8,
                ["Ergonomics"] = -6,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_scope_magnified_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "scope_magnified_structure" && violation.Field == "Accuracy");
    }

    [Fact]
    public void Audit_FlagsMissingBoosterFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["booster-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "muzzle_aks74u_izhmash_std_545x39",
                ["ModType"] = "booster",
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_booster_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "booster_structure" && violation.Field == "AutoROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "booster_structure" && violation.Field == "SemiROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "booster_structure" && violation.Field == "ModMalfunctionChance");
    }

    [Fact]
    public void Audit_FlagsMissingCatchFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["catch-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "catch_m1911_wilson_extended_slide_stop",
                ["ModType"] = "catch",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["AimSpeed"] = 0,
                ["Ergonomics"] = 0,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["DurabilityBurnModificator"] = 1,
                ["LoyaltyLevel"] = 1,
                ["ModMalfunctionChance"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_catch_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "catch_structure" && violation.Field == "ReloadSpeed");
        Assert.Contains(item.Violations, violation => violation.Rule == "catch_structure" && violation.Field == "ChamberSpeed");
        Assert.Contains(item.Violations, violation => violation.Rule == "catch_structure" && violation.Field == "Accuracy");
        Assert.Contains(item.Violations, violation => violation.Rule == "catch_structure" && violation.Field == "FixSpeed");
    }

    [Fact]
    public void Audit_FlagsMissingBipodSupportFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["bipod-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "bipod_sv-98_izhmash_bipod_std",
                ["ModType"] = "bipod",
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["AimSpeed"] = 0,
                ["Ergonomics"] = -2,
                ["Accuracy"] = 3,
                ["LoyaltyLevel"] = 1,
                ["AimStability"] = 1,
                ["Handling"] = -3,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_bipod_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "bipod_structure" && violation.Field == "AutoROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "bipod_structure" && violation.Field == "SemiROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "bipod_structure" && violation.Field == "ModMalfunctionChance");
        Assert.Contains(item.Violations, violation => violation.Rule == "bipod_structure" && violation.Field == "ReloadSpeed");
        Assert.Contains(item.Violations, violation => violation.Rule == "bipod_structure" && violation.Field == "FixSpeed");
    }

    [Fact]
    public void Audit_FlagsMissingBufferAdapterStructureFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["buffer-adapter-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "stock_590_mesa_leo_stock_adapter_gen1",
                ["ModType"] = "buffer_adapter",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 1,
                ["AimSpeed"] = 0,
                ["Ergonomics"] = 2,
                ["Accuracy"] = 0,
                ["LoyaltyLevel"] = 2,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_buffer_adapter_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "Dispersion");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "CameraRecoil");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "HasShoulderContact");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "BlocksFolding");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "AutoROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "SemiROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "ModMalfunctionChance");
        Assert.Contains(item.Violations, violation => violation.Rule == "buffer_adapter_structure" && violation.Field == "StockAllowADS");
    }

    [Fact]
    public void Audit_FlagsMissingHydraulicBufferExtraStructureFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["hydraulic-buffer-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "stock_ar15_mesa_crosshair_hydraulic_buffer_tube",
                ["ModType"] = "hydraulic_buffer",
                ["VerticalRecoil"] = -8,
                ["HorizontalRecoil"] = -5,
                ["Dispersion"] = 20,
                ["CameraRecoil"] = -9,
                ["AimSpeed"] = -3,
                ["Ergonomics"] = 7,
                ["Accuracy"] = 0,
                ["HasShoulderContact"] = false,
                ["BlocksFolding"] = false,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["ModMalfunctionChance"] = 40,
                ["StockAllowADS"] = false,
                ["LoyaltyLevel"] = 4,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_hydraulic_buffer_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "hydraulic_buffer_structure" && violation.Field == "DurabilityBurnModificator");
        Assert.Contains(item.Violations, violation => violation.Rule == "hydraulic_buffer_structure" && violation.Field == "Convergence");
    }

    [Fact]
    public void Audit_FlagsOutOfRangeBipodSupportFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["bipod-range-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "bipod_sv-98_izhmash_bipod_std",
                ["ModType"] = "bipod",
                ["AutoROF"] = 1,
                ["SemiROF"] = 1,
                ["ModMalfunctionChance"] = 1,
                ["ReloadSpeed"] = 1,
                ["FixSpeed"] = 1,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_bipod_range_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "mod_special" && violation.Field == "AutoROF");
        Assert.Contains(item.Violations, violation => violation.Rule == "mod_special" && violation.Field == "FixSpeed");
    }

    [Fact]
    public void Audit_FlagsMissingBarrel2SlotModShotDispersion()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["barrel-2slot-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "muzzle_aug_steyr_a1_closed_flash_hider_556x45",
                ["ModType"] = "barrel_2slot",
                ["VerticalRecoil"] = -3,
                ["HorizontalRecoil"] = -1,
                ["Dispersion"] = 0,
                ["CameraRecoil"] = -1,
                ["Ergonomics"] = -3,
                ["Loudness"] = 7,
                ["Flash"] = -63,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_barrel_2slot_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "barrel_2slot_structure" && violation.Field == "ModShotDispersion");
    }

    [Fact]
    public void Audit_FlagsOutOfRangeBarrel2SlotModShotDispersion()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "output", "user_templates"));

        var patch = new JsonObject
        {
            ["barrel-2slot-range-test"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["Name"] = "muzzle_aug_steyr_a1_closed_flash_hider_556x45",
                ["ModType"] = "barrel_2slot",
                ["ModShotDispersion"] = 5,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "user_templates", "test_barrel_2slot_range_realism_patch.json"),
            patch.ToJsonString());

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("violation", item.Status);
        Assert.Contains(item.Violations, violation => violation.Rule == "mod_special" && violation.Field == "ModShotDispersion");
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