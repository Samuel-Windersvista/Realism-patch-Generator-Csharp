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
        Directory.CreateDirectory(Path.Combine(basePath, "现实主义物品模板"));
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
        Directory.CreateDirectory(Path.Combine(basePath, "现实主义物品模板", "gear"));

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
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "现实主义物品模板", "gear", "armorMasksTemplates.json"),
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