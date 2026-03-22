using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class ItemExceptionIntegrationTests : IDisposable
{
    private readonly string basePath;

    public ItemExceptionIntegrationTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-exception-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "input", "weapons"));
        Directory.CreateDirectory(Path.Combine(basePath, "output"));
        Directory.CreateDirectory(RuleWorkspace.GetTemplatesDirectory(basePath));
    }

    [Fact]
    public void Generate_AppliesItemExceptionOverridesAfterRuleGeneration()
    {
        var input = new JsonObject
        {
            ["test-item"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "test-item",
                ["Name"] = "Unit Test Pistol",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = false,
                ["RecoilAngle"] = 90,
            },
        };

        File.WriteAllText(Path.Combine(basePath, "input", "weapons", "test.json"), input.ToJsonString());
        WriteItemExceptions("test-item", new JsonObject
        {
            ["RecoilAngle"] = 12,
            ["HasShoulderContact"] = true,
        });

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "weapons", "test.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = root["test-item"]!.AsObject();

        Assert.Equal(12, patch["RecoilAngle"]!.GetValue<int>());
        Assert.True(patch["HasShoulderContact"]!.GetValue<bool>());
    }

    [Fact]
    public void Audit_ExemptsOnlyConfiguredExceptionFields()
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
        WriteItemExceptions("test-item", new JsonObject
        {
            ["HasShoulderContact"] = true,
            ["RecoilAngle"] = 10,
        });

        var auditor = new OutputRuleAuditor(basePath, new OutputAuditOptions { IncludeOk = true });
        var report = auditor.Audit();

        var item = Assert.Single(Assert.Single(report.Files).Items);
        Assert.Equal("ok", item.Status);
        Assert.Empty(item.Violations);
        Assert.Equal(2, item.Context["exception_fields"]!.AsArray().Count);
    }

    [Fact]
    public void Generate_KeepsTemplateStyleForStandardGearInput()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "gear"));

        var templateRoot = new JsonObject
        {
            ["template-mask"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gear, RealismMod",
                ["ItemID"] = "template-mask",
                ["Name"] = "item_equipment_atomic",
                ["AllowADS"] = true,
                ["LoyaltyLevel"] = 2,
                ["ArmorClass"] = "NIJ IIIA",
                ["CanSpall"] = false,
                ["SpallReduction"] = 1,
                ["ReloadSpeedMulti"] = 1,
                ["BlocksMouth"] = true,
                ["MaskToUse"] = "atomic",
                ["TemplateType"] = "gear",
                ["Price"] = 12345,
                ["IsGasMask"] = true,
                ["GasProtection"] = 0.82,
                ["RadProtection"] = 0.65,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "gear", "armorMasksTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-mask"] = new JsonObject
            {
                ["itemTplToClone"] = "template-mask",
                ["parentId"] = "5a341c4686f77469e155819e",
                ["overrideProperties"] = new JsonObject
                {
                    ["AllowADS"] = true,
                    ["LoyaltyLevel"] = 4,
                    ["ReloadSpeedMulti"] = 1,
                    ["BlocksMouth"] = true,
                    ["MaskToUse"] = "atomic",
                    ["Prefab"] = new JsonObject
                    {
                        ["path"] = "custom.bundle",
                        ["rcid"] = "",
                    },
                    ["description"] = "should not be copied into template-style output",
                    ["mousePenalty"] = -3,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Mask",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_mask.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_mask_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = root["custom-mask"]!.AsObject();

        Assert.Equal("Custom Mask", patch["Name"]!.GetValue<string>());
        Assert.Equal(4, patch["LoyaltyLevel"]!.GetValue<int>());
        Assert.False(patch.ContainsKey("Prefab"));
        Assert.False(patch.ContainsKey("description"));
        Assert.False(patch.ContainsKey("mousePenalty"));
        Assert.True(patch.ContainsKey("MaskToUse"));
        Assert.Equal("gear", patch["TemplateType"]!.GetValue<string>());
        Assert.Equal(12345, patch["Price"]!.GetValue<int>());
        Assert.True(patch["IsGasMask"]!.GetValue<bool>());
        Assert.Equal(0.82, patch["GasProtection"]!.GetValue<double>());
        Assert.Equal(0.65, patch["RadProtection"]!.GetValue<double>());
    }

    [Fact]
    public void Generate_PreservesWeaponTemplateOnlyFieldsFromCloneTemplate()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "weapons"));

        var templateRoot = new JsonObject
        {
            ["template-weapon"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "template-weapon",
                ["Name"] = "weapon_test_burst",
                ["WeapType"] = "rifle",
                ["VerticalRecoil"] = 92,
                ["HorizontalRecoil"] = 160,
                ["Dispersion"] = 6,
                ["VisualMulti"] = 1.1,
                ["Ergonomics"] = 88,
                ["RecoilIntensity"] = 0.18,
                ["Weight"] = 3.4,
                ["LoyaltyLevel"] = 2,
                ["BurstShotsCount"] = 2,
                ["weapFireType"] = new JsonArray("single", "burst", "fullauto"),
                ["MinReloadSpeed"] = 0.8,
                ["MaxReloadSpeed"] = 1.05,
                ["EnableBSGVisRecoil"] = true,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "weapons", "AssaultCarbineTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-weapon"] = new JsonObject
            {
                ["itemTplToClone"] = "template-weapon",
                ["parentId"] = "5447b5fc4bdc2d87278b4567",
                ["overrideProperties"] = new JsonObject
                {
                    ["Ergonomics"] = 90,
                    ["description"] = "should not be copied into template-style output",
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Burst Weapon",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_weapon_template_fields.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_weapon_template_fields_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = root["custom-weapon"]!.AsObject();

        Assert.Equal("Custom Burst Weapon", patch["Name"]!.GetValue<string>());
        Assert.Equal(2, patch["BurstShotsCount"]!.GetValue<int>());
        Assert.Equal(0.8, patch["MinReloadSpeed"]!.GetValue<double>());
        Assert.Equal(1.05, patch["MaxReloadSpeed"]!.GetValue<double>());
        Assert.True(patch["EnableBSGVisRecoil"]!.GetValue<bool>());
        Assert.Equal(3, patch["weapFireType"]!.AsArray().Count);
        Assert.False(patch.ContainsKey("description"));
    }

    [Fact]
    public void Generate_UsesExpandedWeaponFallbackSkeletonWhenTemplateIsMissing()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "weapons"));

        var inputRoot = new JsonObject
        {
            ["custom-fallback-weapon"] = new JsonObject
            {
                ["parentId"] = "5447b5fc4bdc2d87278b4567",
                ["overrideProperties"] = new JsonObject
                {
                    ["Ergonomics"] = 91,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Fallback Weapon",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "weapons", "test_weapon_fallback.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "weapons", "test_weapon_fallback_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = root["custom-fallback-weapon"]!.AsObject();

        Assert.Equal("Fallback Weapon", patch["Name"]!.GetValue<string>());
        Assert.Equal(0, patch["BurstShotsCount"]!.GetValue<int>());
        Assert.Equal(0, patch["DoubleActionAccuracyPenalty"]!.GetValue<int>());
        Assert.False(patch["EnableBSGVisRecoil"]!.GetValue<bool>());
        Assert.False(patch["ReduceBSGVisRecoil"]!.GetValue<bool>());
        Assert.Equal(0.7, patch["MinReloadSpeed"]!.GetValue<double>());
        Assert.Equal(1.4, patch["MaxReloadSpeed"]!.GetValue<double>());
        Assert.Equal("single", patch["weapFireType"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public void Generate_PreservesStockAdapterRequiredFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-stock-adapter"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-stock-adapter",
                ["Name"] = "stock_adapter_template",
                ["ModType"] = "stock_adapter",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["CameraRecoil"] = 0,
                ["Ergonomics"] = 0,
                ["Weight"] = 0.2,
                ["LoyaltyLevel"] = 1,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "StockTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-stock-adapter"] = new JsonObject
            {
                ["itemTplToClone"] = "template-stock-adapter",
                ["parentId"] = "55818a594bdc2db9688b456a",
                ["overrideProperties"] = new JsonObject
                {
                    ["ModType"] = "stock_adapter",
                    ["Weight"] = 0.232,
                    ["DurabilityBurnModificator"] = 1,
                    ["Loudness"] = 0,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Stock Adapter",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_stock_adapter.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_stock_adapter_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("DurabilityBurnModificator"));
        Assert.True(patch.ContainsKey("Loudness"));
        Assert.Equal(1d, patch["DurabilityBurnModificator"]!.GetValue<double>());
        Assert.Equal(0, patch["Loudness"]!.GetValue<int>());
    }

    [Fact]
    public void Generate_PreservesAndClampsGasblockFieldsFromSource()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-gasblock"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-gasblock",
                ["Name"] = "gasblock_template",
                ["ModType"] = string.Empty,
                ["VerticalRecoil"] = -2,
                ["HorizontalRecoil"] = -1,
                ["ModMalfunctionChance"] = 0,
                ["HeatFactor"] = 1.0,
                ["CoolFactor"] = 1.0,
                ["DurabilityBurnModificator"] = 1.0,
                ["Ergonomics"] = 1,
                ["Weight"] = 0.2,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "GasblockTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-gasblock"] = new JsonObject
            {
                ["itemTplToClone"] = "template-gasblock",
                ["parentId"] = "GAS_BLOCK",
                ["overrideProperties"] = new JsonObject
                {
                    ["Weight"] = 2.3,
                    ["Loudness"] = 10,
                    ["Velocity"] = 14,
                    ["Ergonomics"] = 18,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Gasblock",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_gasblock.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_gasblock_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Loudness"));
        Assert.True(patch.ContainsKey("Velocity"));
        Assert.Equal(10, patch["Loudness"]!.GetValue<int>());
        Assert.Equal(2d, patch["Velocity"]!.GetValue<double>());
    }

    [Fact]
    public void Generate_PreservesAndClampsHandguardFieldsFromSource()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-handguard"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-handguard",
                ["Name"] = "handguard_m60_usord_m60e4_mod1",
                ["ModType"] = string.Empty,
                ["VerticalRecoil"] = -6,
                ["HorizontalRecoil"] = -3,
                ["HeatFactor"] = 1.0,
                ["CoolFactor"] = 1.0,
                ["AimStability"] = 8,
                ["AimSpeed"] = 2,
                ["Handling"] = 4,
                ["Ergonomics"] = 3,
                ["Weight"] = 0.2,
                ["Accuracy"] = 0,
                ["Dispersion"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "HandguardTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-handguard"] = new JsonObject
            {
                ["itemTplToClone"] = "template-handguard",
                ["parentId"] = "55818a104bdc2db9688b4569",
                ["overrideProperties"] = new JsonObject
                {
                    ["Weight"] = 2.3,
                    ["Accuracy"] = 22.5,
                    ["Dispersion"] = -4,
                    ["Ergonomics"] = 18,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Handguard",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_handguard.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_handguard_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Accuracy"));
        Assert.True(patch.ContainsKey("Dispersion"));
        Assert.Equal(7d, patch["Accuracy"]!.GetValue<double>());
        Assert.Equal(-3d, patch["Dispersion"]!.GetValue<double>());
    }

    [Fact]
    public void Generate_ClampsForegripAccuracyToRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-foregrip"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-foregrip",
                ["Name"] = "foregrip_sr3m_tochmash_sr3m_std",
                ["ModType"] = string.Empty,
                ["VerticalRecoil"] = -5,
                ["HorizontalRecoil"] = -3,
                ["Dispersion"] = 0,
                ["AimSpeed"] = 4,
                ["Ergonomics"] = 7,
                ["Accuracy"] = 0,
                ["Handling"] = 13,
                ["CameraRecoil"] = -4,
                ["Convergence"] = -1,
                ["AimStability"] = 11,
                ["Weight"] = 0.08,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "ForegripTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-foregrip"] = new JsonObject
            {
                ["itemTplToClone"] = "template-foregrip",
                ["parentId"] = "55818af64bdc2d5b648b4570",
                ["overrideProperties"] = new JsonObject
                {
                    ["Accuracy"] = 5,
                    ["Ergonomics"] = 9,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Foregrip",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_foregrip.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_foregrip_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Accuracy"));
        Assert.Equal(0, patch["Accuracy"]!.GetValue<int>());
    }

    [Fact]
    public void Generate_PreservesAndClampsIronSightAccuracyFromSource()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-iron-sight"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-iron-sight",
                ["Name"] = "sight_rear_ak_tactica_tula_tt01",
                ["ModType"] = string.Empty,
                ["AimSpeed"] = 1,
                ["Accuracy"] = 0,
                ["Ergonomics"] = 1,
                ["Weight"] = 0.08,
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["LoyaltyLevel"] = 1,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "IronSightTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-iron-sight"] = new JsonObject
            {
                ["itemTplToClone"] = "template-iron-sight",
                ["parentId"] = "55818ac54bdc2d5b648b456e",
                ["overrideProperties"] = new JsonObject
                {
                    ["AimSpeed"] = 2,
                    ["Accuracy"] = -30,
                    ["Ergonomics"] = 2,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Iron Sight",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_iron_sight.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_iron_sight_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Accuracy"));
        Assert.Equal(-15d, patch["Accuracy"]!.GetValue<double>());
    }

    [Fact]
    public void Generate_ConstrainsPistolGripDispersionWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-pistol-grip"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-pistol-grip",
                ["Name"] = "pistolgrip_deagle_hogue_rubber_grips_ergo",
                ["ModType"] = string.Empty,
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["Dispersion"] = 25,
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
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "PistolGripTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-pistol-grip"] = new JsonObject
            {
                ["itemTplToClone"] = "template-pistol-grip",
                ["parentId"] = "55818a684bdc2ddd698b456d",
                ["overrideProperties"] = new JsonObject
                {
                    ["Dispersion"] = 25,
                    ["Ergonomics"] = 6,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Pistol Grip",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_pistol_grip.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_pistol_grip_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Dispersion"));
        var dispersion = patch["Dispersion"]!.GetValue<int>();
        Assert.InRange(dispersion, -3, 3);
    }

    [Fact]
    public void Generate_ConstrainsUbglFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-ubgl"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-ubgl",
                ["Name"] = "launcher_ar15_colt_m203_40x46",
                ["ModType"] = "UBGL",
                ["VerticalRecoil"] = 4,
                ["HorizontalRecoil"] = 2,
                ["Dispersion"] = 5,
                ["CameraRecoil"] = 3,
                ["AimSpeed"] = 6,
                ["Ergonomics"] = 7,
                ["Accuracy"] = -30,
                ["HasShoulderContact"] = false,
                ["BlocksFolding"] = false,
                ["AutoROF"] = 10,
                ["SemiROF"] = 8,
                ["ModMalfunctionChance"] = 12,
                ["StockAllowADS"] = false,
                ["ConflictingItems"] = new JsonArray(),
                ["Weight"] = 1.36,
                ["HeatFactor"] = 2,
                ["CoolFactor"] = 2,
                ["LoyaltyLevel"] = 4,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "UBGLTempaltes.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-ubgl"] = new JsonObject
            {
                ["itemTplToClone"] = "template-ubgl",
                ["parentId"] = "55818b014bdc2ddc698b456b",
                ["overrideProperties"] = new JsonObject
                {
                    ["Ergonomics"] = 9,
                    ["Accuracy"] = -30,
                    ["AutoROF"] = 15,
                    ["SemiROF"] = 12,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom UBGL",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_ubgl.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_ubgl_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.Equal(0, patch["Ergonomics"]!.GetValue<int>());
        Assert.Equal(0, patch["AutoROF"]!.GetValue<int>());
        Assert.Equal(0, patch["SemiROF"]!.GetValue<int>());
        Assert.InRange(patch["Accuracy"]!.GetValue<int>(), -15, 0);
    }

    [Fact]
    public void Generate_ConstrainsReceiverChamberSpeedWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-receiver"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-receiver",
                ["Name"] = "reciever_pl15_izhmash_pl15_std",
                ["ModType"] = string.Empty,
                ["AutoROF"] = 1,
                ["SemiROF"] = 2,
                ["ModMalfunctionChance"] = 2,
                ["Accuracy"] = -1,
                ["ChamberSpeed"] = 60,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["Ergonomics"] = 6,
                ["Convergence"] = 8,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "ReceiverTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-receiver"] = new JsonObject
            {
                ["itemTplToClone"] = "template-receiver",
                ["parentId"] = "55818a304bdc2db5418b457d",
                ["overrideProperties"] = new JsonObject
                {
                    ["ChamberSpeed"] = 60,
                    ["Accuracy"] = 3,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Receiver",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_receiver.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_receiver_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("ChamberSpeed"));
        Assert.InRange(patch["ChamberSpeed"]!.GetValue<int>(), 0, 40);
    }

    [Fact]
    public void Generate_ConstrainsBayonetFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-bayonet"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-bayonet",
                ["Name"] = "m9_bayonet",
                ["ModType"] = "bayonet",
                ["VerticalRecoil"] = -10,
                ["HorizontalRecoil"] = 4,
                ["Dispersion"] = 3,
                ["CameraRecoil"] = 3,
                ["AutoROF"] = 7,
                ["SemiROF"] = 7,
                ["ModMalfunctionChance"] = 9,
                ["CanCycleSubs"] = false,
                ["Accuracy"] = -30,
                ["HeatFactor"] = 2,
                ["CoolFactor"] = 2,
                ["DurabilityBurnModificator"] = 2,
                ["Velocity"] = 4,
                ["RecoilAngle"] = 5,
                ["ConflictingItems"] = new JsonArray(),
                ["Ergonomics"] = -10,
                ["Weight"] = 0.45,
                ["Loudness"] = 20,
                ["Convergence"] = 3,
                ["LoyaltyLevel"] = 1,
                ["MeleeDamage"] = 200,
                ["MeleePen"] = 60,
                ["Flash"] = -70,
                ["AimSpeed"] = 5,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "MuzzleDeviceTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-bayonet"] = new JsonObject
            {
                ["itemTplToClone"] = "template-bayonet",
                ["parentId"] = "550aa4bf4bdc2dd6348b456b",
                ["overrideProperties"] = new JsonObject
                {
                    ["MeleeDamage"] = 200,
                    ["MeleePen"] = 60,
                    ["Accuracy"] = -30,
                    ["Loudness"] = 20,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Bayonet",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_bayonet.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_bayonet_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["MeleeDamage"]!.GetValue<int>(), 65, 112);
        Assert.InRange(patch["MeleePen"]!.GetValue<int>(), 18, 40);
        Assert.InRange(patch["Accuracy"]!.GetValue<int>(), -15, -12);
        Assert.InRange(patch["Loudness"]!.GetValue<int>(), 1, 8);
    }

    [Fact]
    public void Generate_ConstrainsTriggerFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-trigger"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-trigger",
                ["Name"] = "trigger_m1911_caspian_trik_trigger",
                ["ModType"] = "trigger",
                ["SemiROF"] = 8,
                ["ModMalfunctionChance"] = 1,
                ["Ergonomics"] = -1,
                ["Accuracy"] = 20,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "AuxiliaryModTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-trigger"] = new JsonObject
            {
                ["itemTplToClone"] = "template-trigger",
                ["parentId"] = "5448fe394bdc2d0d028b456c",
                ["overrideProperties"] = new JsonObject
                {
                    ["SemiROF"] = 8,
                    ["Accuracy"] = 20,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Trigger",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_trigger.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_trigger_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["SemiROF"]!.GetValue<int>(), 0, 5);
        Assert.InRange(patch["Accuracy"]!.GetValue<int>(), 0, 15);
    }

    [Fact]
    public void Generate_ConstrainsMountFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-mount"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-mount",
                ["Name"] = "mount_mk16_pws_srx",
                ["ModType"] = "mount",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["Dispersion"] = 0,
                ["Ergonomics"] = 0,
                ["AimStability"] = 0,
                ["Handling"] = 0,
                ["Accuracy"] = 0,
                ["HeatFactor"] = 1.2,
                ["CoolFactor"] = 0.5,
                ["AimSpeed"] = 7,
                ["DurabilityBurnModificator"] = 1.4,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "MountTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-mount"] = new JsonObject
            {
                ["itemTplToClone"] = "template-mount",
                ["parentId"] = "55818b224bdc2dde698b456f",
                ["overrideProperties"] = new JsonObject
                {
                    ["HeatFactor"] = 1.2,
                    ["CoolFactor"] = 0.5,
                    ["AimSpeed"] = 7,
                    ["DurabilityBurnModificator"] = 1.4,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Mount",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_mount.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_mount_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["HeatFactor"]!.GetValue<double>(), 0.95, 1.03);
        Assert.InRange(patch["CoolFactor"]!.GetValue<double>(), 0.92, 1.06);
        Assert.InRange(patch["AimSpeed"]!.GetValue<int>(), -5, 3);
        Assert.Equal(1d, patch["DurabilityBurnModificator"]!.GetValue<double>());
    }

    [Fact]
    public void Generate_ConstrainsHammerFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-hammer"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-hammer",
                ["Name"] = "hammer_m1911_sti_hex",
                ["ModType"] = "hammer",
                ["SemiROF"] = 9,
                ["ModMalfunctionChance"] = 1,
                ["Ergonomics"] = -1,
                ["Accuracy"] = 20,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "AuxiliaryModTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-hammer"] = new JsonObject
            {
                ["itemTplToClone"] = "template-hammer",
                ["parentId"] = "5448fe394bdc2d0d028b456c",
                ["overrideProperties"] = new JsonObject
                {
                    ["SemiROF"] = 9,
                    ["Accuracy"] = 20,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Hammer",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_hammer.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_hammer_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["SemiROF"]!.GetValue<double>(), 0, 7.5);
        Assert.InRange(patch["Accuracy"]!.GetValue<int>(), 0, 15);
    }

    [Fact]
    public void Generate_ConstrainsBarrelShotgunDispersionWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-barrel"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-barrel",
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
                ["ShotgunDispersion"] = 5,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "BarrelTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-barrel"] = new JsonObject
            {
                ["itemTplToClone"] = "template-barrel",
                ["parentId"] = "555ef6e44bdc2de9068b457e",
                ["overrideProperties"] = new JsonObject
                {
                    ["ShotgunDispersion"] = 5,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Barrel",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_barrel.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_barrel_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["ShotgunDispersion"]!.GetValue<double>(), 0.8, 2);
    }

    [Fact]
    public void Generate_ConstrainsChargingHandleFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-charging-handle"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-charging-handle",
                ["Name"] = "charge_ax_accuracy_internation_axmc_762x51_pb",
                ["ModType"] = "charging_handle",
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["ChamberSpeed"] = 45,
                ["ModMalfunctionChance"] = 4,
                ["Ergonomics"] = 1,
                ["ReloadSpeed"] = 8,
                ["FixSpeed"] = 5,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "ChargingHandleTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-charging-handle"] = new JsonObject
            {
                ["itemTplToClone"] = "template-charging-handle",
                ["parentId"] = "55818a6f4bdc2db9688b456b",
                ["overrideProperties"] = new JsonObject
                {
                    ["ChamberSpeed"] = 45,
                    ["ReloadSpeed"] = 8,
                    ["FixSpeed"] = 5,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Charging Handle",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_charging_handle.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_charging_handle_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.Equal(0, patch["ReloadSpeed"]!.GetValue<int>());
        Assert.Equal(0, patch["FixSpeed"]!.GetValue<int>());
    }

    [Fact]
    public void Generate_ConstrainsFlashlightLaserRecoilFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-flashlight"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-flashlight",
                ["Name"] = "flashlight_armytek_predator_pro_v3_xhp35_hi",
                ["ModType"] = "flashlight",
                ["VerticalRecoil"] = 3,
                ["HorizontalRecoil"] = -2,
                ["Ergonomics"] = 0,
                ["Handling"] = -3,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "FlashlightLaserTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-flashlight"] = new JsonObject
            {
                ["itemTplToClone"] = "template-flashlight",
                ["parentId"] = "55818b084bdc2d5b648b4571",
                ["overrideProperties"] = new JsonObject
                {
                    ["VerticalRecoil"] = 3,
                    ["HorizontalRecoil"] = -2,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Flashlight",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_flashlight.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_flashlight_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.Equal(0, patch["VerticalRecoil"]!.GetValue<int>());
        Assert.Equal(0, patch["HorizontalRecoil"]!.GetValue<int>());
    }

    [Fact]
    public void Generate_ConstrainsRedDotScopeAccuracyWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-red-dot"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-red-dot",
                ["Name"] = "scope_base_trijicon_rmr",
                ["ModType"] = "sight",
                ["AimSpeed"] = 4,
                ["Accuracy"] = 20.5,
                ["AimStability"] = 2,
                ["Ergonomics"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "ScopeTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-red-dot"] = new JsonObject
            {
                ["itemTplToClone"] = "template-red-dot",
                ["parentId"] = "55818ad54bdc2ddc698b4569",
                ["overrideProperties"] = new JsonObject
                {
                    ["Accuracy"] = 20.5,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Red Dot",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_scope_red_dot.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_scope_red_dot_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["Accuracy"]!.GetValue<double>(), -15, 15);
    }

    [Fact]
    public void Generate_ConstrainsBoosterFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-booster"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-booster",
                ["Name"] = "muzzle_aks74u_izhmash_std_545x39",
                ["ModType"] = "booster",
                ["AutoROF"] = 2.5,
                ["SemiROF"] = 2.25,
                ["ModMalfunctionChance"] = -40,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "MuzzleDeviceTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-booster"] = new JsonObject
            {
                ["itemTplToClone"] = "template-booster",
                ["parentId"] = "550aa4bf4bdc2dd6348b456b",
                ["overrideProperties"] = new JsonObject
                {
                    ["AutoROF"] = 2.5,
                    ["SemiROF"] = 2.25,
                    ["ModMalfunctionChance"] = -40,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Booster",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_booster.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_booster_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["AutoROF"]!.GetValue<double>(), 1.2, 1.5);
        Assert.InRange(patch["SemiROF"]!.GetValue<double>(), 1.2, 1.5);
        Assert.InRange(patch["ModMalfunctionChance"]!.GetValue<int>(), -25, -8);
    }

    [Fact]
    public void Generate_ConstrainsCatchFieldsWithinRuleRange()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-catch"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-catch",
                ["Name"] = "catch_m1911_wilson_extended_slide_stop",
                ["ModType"] = "catch",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 0,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["AimSpeed"] = 0,
                ["ReloadSpeed"] = 9,
                ["ChamberSpeed"] = 10,
                ["Ergonomics"] = 0,
                ["Accuracy"] = 4,
                ["FixSpeed"] = 3,
                ["HeatFactor"] = 1,
                ["CoolFactor"] = 1,
                ["DurabilityBurnModificator"] = 1,
                ["LoyaltyLevel"] = 4,
                ["ModMalfunctionChance"] = 1,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "AuxiliaryModTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-catch"] = new JsonObject
            {
                ["itemTplToClone"] = "template-catch",
                ["parentId"] = "5448fe394bdc2d0d028b456c",
                ["overrideProperties"] = new JsonObject
                {
                    ["ReloadSpeed"] = 9,
                    ["ChamberSpeed"] = 10,
                    ["Accuracy"] = 4,
                    ["FixSpeed"] = 3,
                    ["LoyaltyLevel"] = 4,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Catch",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_catch.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_catch_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.InRange(patch["ReloadSpeed"]!.GetValue<int>(), 0, 5);
        Assert.InRange(patch["ChamberSpeed"]!.GetValue<double>(), 2.5, 8.5);
        Assert.InRange(patch["Accuracy"]!.GetValue<int>(), 0, 1);
        Assert.Equal(0, patch["FixSpeed"]!.GetValue<int>());
        Assert.InRange(patch["LoyaltyLevel"]!.GetValue<int>(), 1, 3);
    }

    [Fact]
    public void Generate_PreservesBipodSupportFieldsStructure()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-bipod"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-bipod",
                ["Name"] = "bipod_sv-98_izhmash_bipod_std",
                ["ModType"] = "bipod",
                ["VerticalRecoil"] = -1,
                ["HorizontalRecoil"] = -1,
                ["AutoROF"] = 3,
                ["SemiROF"] = 4,
                ["ModMalfunctionChance"] = 5,
                ["AimSpeed"] = 0,
                ["ReloadSpeed"] = 6,
                ["Ergonomics"] = -2,
                ["Accuracy"] = 3,
                ["FixSpeed"] = 7,
                ["LoyaltyLevel"] = 1,
                ["AimStability"] = 1,
                ["Handling"] = -3,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "AuxiliaryModTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-bipod"] = new JsonObject
            {
                ["itemTplToClone"] = "template-bipod",
                ["parentId"] = "5448fe394bdc2d0d028b456c",
                ["overrideProperties"] = new JsonObject
                {
                    ["AutoROF"] = 3,
                    ["SemiROF"] = 4,
                    ["ModMalfunctionChance"] = 5,
                    ["ReloadSpeed"] = 6,
                    ["FixSpeed"] = 7,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Bipod",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_bipod.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_bipod_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        foreach (var fieldName in new[] { "AutoROF", "SemiROF", "ModMalfunctionChance", "ReloadSpeed", "FixSpeed" })
        {
            Assert.True(patch.ContainsKey(fieldName));
            Assert.Equal(0, patch[fieldName]!.GetValue<int>());
        }
    }

    [Fact]
    public void Generate_PreservesBufferAdapterStructureFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-buffer-adapter"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-buffer-adapter",
                ["Name"] = "stock_590_mesa_leo_stock_adapter_gen1",
                ["ModType"] = "buffer_adapter",
                ["VerticalRecoil"] = 0,
                ["HorizontalRecoil"] = 1,
                ["Dispersion"] = -3,
                ["CameraRecoil"] = 5,
                ["AimSpeed"] = 0,
                ["Ergonomics"] = 2,
                ["Accuracy"] = 0,
                ["HasShoulderContact"] = true,
                ["BlocksFolding"] = true,
                ["AutoROF"] = 4,
                ["SemiROF"] = 5,
                ["ModMalfunctionChance"] = 6,
                ["StockAllowADS"] = true,
                ["LoyaltyLevel"] = 2,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "StockTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-buffer-adapter"] = new JsonObject
            {
                ["itemTplToClone"] = "template-buffer-adapter",
                ["parentId"] = "55818a594bdc2db9688b456a",
                ["overrideProperties"] = new JsonObject
                {
                    ["Dispersion"] = -3,
                    ["CameraRecoil"] = 5,
                    ["HasShoulderContact"] = true,
                    ["BlocksFolding"] = true,
                    ["AutoROF"] = 4,
                    ["SemiROF"] = 5,
                    ["ModMalfunctionChance"] = 6,
                    ["StockAllowADS"] = true,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Buffer Adapter",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_buffer_adapter.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_buffer_adapter_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.Equal(-3, patch["Dispersion"]!.GetValue<int>());
        Assert.Equal(5, patch["CameraRecoil"]!.GetValue<int>());
        Assert.True(patch["HasShoulderContact"]!.GetValue<bool>());
        Assert.True(patch["BlocksFolding"]!.GetValue<bool>());
        Assert.Equal(4, patch["AutoROF"]!.GetValue<int>());
        Assert.Equal(5, patch["SemiROF"]!.GetValue<int>());
        Assert.Equal(6, patch["ModMalfunctionChance"]!.GetValue<int>());
        Assert.True(patch["StockAllowADS"]!.GetValue<bool>());
    }

    [Fact]
    public void Generate_PreservesHydraulicBufferStructureFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-hydraulic-buffer"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-hydraulic-buffer",
                ["Name"] = "stock_ar15_mesa_crosshair_hydraulic_buffer_tube",
                ["ModType"] = "hydraulic_buffer",
                ["VerticalRecoil"] = -8,
                ["HorizontalRecoil"] = -5,
                ["Dispersion"] = 20,
                ["CameraRecoil"] = -9,
                ["AimSpeed"] = -3,
                ["Ergonomics"] = 7,
                ["Accuracy"] = 0,
                ["Convergence"] = 5,
                ["HasShoulderContact"] = false,
                ["BlocksFolding"] = false,
                ["AutoROF"] = 0,
                ["SemiROF"] = 0,
                ["ModMalfunctionChance"] = 40,
                ["StockAllowADS"] = false,
                ["DurabilityBurnModificator"] = 1.2,
                ["LoyaltyLevel"] = 4,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "StockTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-hydraulic-buffer"] = new JsonObject
            {
                ["itemTplToClone"] = "template-hydraulic-buffer",
                ["parentId"] = "55818a594bdc2db9688b456a",
                ["overrideProperties"] = new JsonObject
                {
                    ["Convergence"] = 5,
                    ["DurabilityBurnModificator"] = 1.2,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Hydraulic Buffer",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_hydraulic_buffer.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_hydraulic_buffer_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("Convergence"));
        Assert.True(patch.ContainsKey("DurabilityBurnModificator"));
    }

    [Fact]
    public void Generate_PreservesShotPumpGripAdaptStructureFields()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-shot-pump-grip-adapt"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-shot-pump-grip-adapt",
                ["Name"] = "handguard_870_magpul_moe_870",
                ["ModType"] = "shot_pump_grip_adapt",
                ["VerticalRecoil"] = -5,
                ["HorizontalRecoil"] = -2,
                ["Dispersion"] = -2,
                ["AimSpeed"] = 2,
                ["ChamberSpeed"] = 17,
                ["Ergonomics"] = 5,
                ["Accuracy"] = 0,
                ["HeatFactor"] = 1.02,
                ["CoolFactor"] = 0.97,
                ["ReloadSpeed"] = 0,
                ["LoyaltyLevel"] = 3,
                ["AimStability"] = 7,
                ["Handling"] = 5,
                ["DurabilityBurnModificator"] = 1.0,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "HandguardTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-shot-pump-grip-adapt"] = new JsonObject
            {
                ["itemTplToClone"] = "template-shot-pump-grip-adapt",
                ["parentId"] = "55818a104bdc2db9688b4569",
                ["overrideProperties"] = new JsonObject
                {
                    ["ChamberSpeed"] = 17,
                    ["ReloadSpeed"] = 0,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Shot Pump Grip Adapt",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_shot_pump_grip_adapt.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_shot_pump_grip_adapt_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("ChamberSpeed"));
        Assert.True(patch.ContainsKey("ReloadSpeed"));
    }

    [Fact]
    public void Generate_PreservesBarrel2SlotModShotDispersionStructure()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments"));

        var templateRoot = new JsonObject
        {
            ["template-barrel-2slot"] = new JsonObject
            {
                ["$type"] = "RealismMod.WeaponMod, RealismMod",
                ["ItemID"] = "template-barrel-2slot",
                ["Name"] = "muzzle_aug_steyr_a1_closed_flash_hider_556x45",
                ["ModType"] = "barrel_2slot",
                ["VerticalRecoil"] = -3,
                ["HorizontalRecoil"] = -1,
                ["Dispersion"] = 0,
                ["CameraRecoil"] = -1,
                ["ModShotDispersion"] = 5,
                ["Ergonomics"] = -3,
                ["Loudness"] = 7,
                ["Flash"] = -63,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "attatchments", "MuzzleDeviceTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-barrel-2slot"] = new JsonObject
            {
                ["itemTplToClone"] = "template-barrel-2slot",
                ["parentId"] = "550aa4bf4bdc2dd6348b456b",
                ["overrideProperties"] = new JsonObject
                {
                    ["ModShotDispersion"] = 5,
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Barrel 2slot",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_barrel_2slot.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_barrel_2slot_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = Assert.Single(root).Value!.AsObject();

        Assert.True(patch.ContainsKey("ModShotDispersion"));
        Assert.Equal(0, patch["ModShotDispersion"]!.GetValue<int>());
    }

    [Fact]
    public void Generate_UsesAmmoOutputTemplateStructureForStandardAmmoInput()
    {
        Directory.CreateDirectory(Path.Combine(basePath, "input", "user_templates"));
        Directory.CreateDirectory(Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "ammo"));

        var templateRoot = new JsonObject
        {
            ["5e023e53d4353e3302577c4c"] = new JsonObject
            {
                ["$type"] = "RealismMod.Ammo, RealismMod",
                ["Name"] = "Template Ammo",
                ["Damage"] = 50,
                ["PenetrationPower"] = 20,
                ["LoyaltyLevel"] = 3,
                ["BasePriceModifier"] = 1,
                ["ItemID"] = "5e023e53d4353e3302577c4c",
                ["InitialSpeed"] = 0,
                ["BulletMassGram"] = 0,
                ["BallisticCoeficient"] = 0,
                ["Weight"] = 0,
                ["DurabilityBurnModificator"] = 1,
                ["ammoRec"] = 0,
                ["ammoAccr"] = 0,
                ["ArmorDamage"] = 1,
                ["HeatFactor"] = 1,
                ["HeavyBleedingDelta"] = 0,
                ["LightBleedingDelta"] = 0,
                ["MalfMisfireChance"] = 0,
                ["MisfireChance"] = 0,
                ["MalfFeedChance"] = 0,
            },
        };

        File.WriteAllText(
            Path.Combine(RuleWorkspace.GetTemplatesDirectory(basePath), "ammo", "ammoTemplates.json"),
            templateRoot.ToJsonString());

        var inputRoot = new JsonObject
        {
            ["custom-ammo"] = new JsonObject
            {
                ["itemTplToClone"] = "5e023e53d4353e3302577c4c",
                ["parentId"] = "5485a8684bdc2da71d8b4567",
                ["overrideProperties"] = new JsonObject
                {
                    ["Weight"] = 0.027,
                    ["InitialSpeed"] = 760,
                    ["BallisticCoeficient"] = 0.543,
                    ["BulletMassGram"] = 12.8,
                    ["BulletDiameterMilimeters"] = 7.92,
                    ["Damage"] = 84,
                    ["ammoAccr"] = 1,
                    ["ammoRec"] = 0,
                    ["PenetrationPower"] = 44,
                    ["PenetrationPowerDiviation"] = 0.19,
                    ["ArmorDamage"] = 1,
                    ["DurabilityBurnModificator"] = 1.32,
                    ["HeatFactor"] = 1.37,
                    ["HeavyBleedingDelta"] = 0.10,
                    ["LightBleedingDelta"] = 0.10,
                    ["MalfMisfireChance"] = 0.009,
                    ["MisfireChance"] = 0.011,
                    ["MalfFeedChance"] = 0.036,
                    ["Prefab"] = new JsonObject
                    {
                        ["path"] = "Ammo/custom.bundle",
                        ["rcid"] = "",
                    },
                    ["ExaminedByDefault"] = true,
                    ["Caliber"] = "Caliber792x57",
                },
                ["locales"] = new JsonObject
                {
                    ["en"] = new JsonObject
                    {
                        ["name"] = "Custom Ammo",
                    },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "user_templates", "test_ammo.json"),
            inputRoot.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "user_templates", "test_ammo_realism_patch.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var patch = root["custom-ammo"]!.AsObject();

        Assert.Equal(
            [
                "$type",
                "Name",
                "Damage",
                "PenetrationPower",
                "LoyaltyLevel",
                "BasePriceModifier",
                "ItemID",
                "InitialSpeed",
                "BulletMassGram",
                "BallisticCoeficient",
                "Weight",
                "DurabilityBurnModificator",
                "ammoRec",
                "ammoAccr",
                "ArmorDamage",
                "HeatFactor",
                "HeavyBleedingDelta",
                "LightBleedingDelta",
                "MalfMisfireChance",
                "MisfireChance",
                "MalfFeedChance",
            ],
            patch.Select(static pair => pair.Key).ToArray());

        Assert.Equal("Custom Ammo", patch["Name"]!.GetValue<string>());
        Assert.Equal(0.027, patch["Weight"]!.GetValue<double>());
        Assert.False(patch.ContainsKey("Prefab"));
        Assert.False(patch.ContainsKey("ExaminedByDefault"));
        Assert.False(patch.ContainsKey("BulletDiameterMilimeters"));
        Assert.False(patch.ContainsKey("PenetrationPowerDiviation"));
        Assert.False(patch.ContainsKey("Caliber"));
    }

    [Fact]
    public void Generate_PreservesInputItemOrderInOutput()
    {
        var input = new JsonObject
        {
            ["z-last"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "z-last",
                ["Name"] = "Z Last",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = false,
                ["RecoilAngle"] = 90,
            },
            ["a-first"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "a-first",
                ["Name"] = "A First",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = false,
                ["RecoilAngle"] = 90,
            },
            ["m-middle"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "m-middle",
                ["Name"] = "M Middle",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = false,
                ["RecoilAngle"] = 90,
            },
        };

        File.WriteAllText(Path.Combine(basePath, "input", "weapons", "order_test.json"), input.ToJsonString());

        var generator = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath);
        var result = generator.Generate(Path.Combine(basePath, "output"));

        var outputFile = Path.Combine(result.OutputPath, "weapons", "order_test.json");
        var root = JsonNode.Parse(File.ReadAllText(outputFile))!.AsObject();
        var keys = root.Select(pair => pair.Key).ToArray();

        Assert.Equal(["z-last", "a-first", "m-middle"], keys);
    }

    [Fact]
    public void Generate_WithExplicitSeed_ProducesRepeatableOutput()
    {
        var input = new JsonObject
        {
            ["seeded-item"] = new JsonObject
            {
                ["$type"] = "RealismMod.Gun, RealismMod",
                ["ItemID"] = "seeded-item",
                ["Name"] = "Seeded Test Weapon",
                ["WeapType"] = "pistol",
                ["HasShoulderContact"] = false,
                ["RecoilAngle"] = 90,
            },
        };

        File.WriteAllText(Path.Combine(basePath, "input", "weapons", "seed_test.json"), input.ToJsonString());

        var generatorA = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var resultA = generatorA.Generate(Path.Combine(basePath, "output_a"));

        var generatorB = new global::RealismPatchGenerator.Core.RealismPatchGenerator(basePath, 123456789u);
        var resultB = generatorB.Generate(Path.Combine(basePath, "output_b"));

        var fileA = Path.Combine(resultA.OutputPath, "weapons", "seed_test.json");
        var fileB = Path.Combine(resultB.OutputPath, "weapons", "seed_test.json");

        Assert.Equal(File.ReadAllText(fileA), File.ReadAllText(fileB));
    }

    private void WriteItemExceptions(string itemId, JsonObject overrides)
    {
        var document = new ItemExceptionDocument();
        document.Items[itemId] = new ItemExceptionEntry
        {
            ItemId = itemId,
            Enabled = true,
            Name = "Unit Test Pistol",
            SourceFile = "weapons/test.json",
            Notes = "test",
            Overrides = overrides,
        };

        ItemExceptionStore.Save(basePath, document);
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }
}