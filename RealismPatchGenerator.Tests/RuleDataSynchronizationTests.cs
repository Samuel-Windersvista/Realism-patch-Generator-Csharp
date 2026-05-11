using System.Text.Json.Nodes;
using System.Reflection;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class RuleDataSynchronizationTests : IDisposable
{
    private readonly string basePath;
    private readonly string repoRoot;

    public RuleDataSynchronizationTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-rule-sync-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
        repoRoot = FindRepositoryRoot();
    }

    [Fact]
    public void DefaultRuleData_StaysInSyncWithRealismItemRules()
    {
        RuleWorkspace.EnsureInitialized(basePath);

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

    [Fact]
    public void MultiItemFile_GenerationPreservesPerFileOutputAndItemOrder()
    {
            var workspaceRoot = CreateGeneratorWorkspace("file-processing-context-order");
            var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var inputPath = Path.Combine(inputRoot, "file-processing-context-order.json");
            File.WriteAllText(inputPath, """
            {
                "test_context_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_context_weapon",
                    "parentId": "5447b5e04bdc2d62278b4567",
                    "Name": "Test Context Rifle",
                    "WeapType": "rifle",
                    "LoyaltyLevel": 2,
                    "Price": 42000,
                    "Ergonomics": 62,
                    "VerticalRecoil": 82,
                    "HorizontalRecoil": 164,
                    "Dispersion": 7,
                    "Convergence": 14,
                    "RecoilIntensity": 0.18,
                    "AutoROF": 650,
                    "SemiROF": 320,
                    "Weight": 3.7
                },
                "test_context_ammo": {
                    "$type": "RealismMod.Ammo, RealismMod",
                    "ItemID": "test_context_ammo",
                    "Name": "Test Context Ammo",
                    "Caliber": "Caliber9x19PARA",
                    "Weight": 0.012,
                    "Damage": 52,
                    "PenetrationPower": 18,
                    "InitialSpeed": 410,
                    "ArmorDamage": 18
                }
            }
            """);

            var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
            var result = generator.Generate(Path.Combine(workspaceRoot, "generated-output"));

            var patchPath = Path.Combine(result.OutputPath, "user_templates", "file-processing-context-order_realism_patch.json");
            Assert.True(File.Exists(patchPath), $"缺少输出文件: {patchPath}");

            var root = JsonNode.Parse(File.ReadAllText(patchPath))!.AsObject();
            Assert.Equal(["test_context_weapon", "test_context_ammo"], root.Select(pair => pair.Key).ToArray());
    }

    [Fact]
    public void DisabledItem_IsSkippedWithoutBreakingSiblingOutputInSameFile()
    {
            var workspaceRoot = CreateGeneratorWorkspace("file-processing-context-disabled-item");
            var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var inputPath = Path.Combine(inputRoot, "file-processing-context-disabled-item.json");
            File.WriteAllText(inputPath, """
            {
                "test_disabled_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_disabled_weapon",
                    "enable": false,
                    "Name": "Disabled Weapon"
                },
                "test_enabled_gear": {
                    "$type": "RealismMod.Gear, RealismMod",
                    "ItemID": "test_enabled_gear",
                    "Name": "Enabled Chest Rig",
                    "LoyaltyLevel": 1,
                    "Price": 20000,
                    "ReloadSpeedMulti": 1.0,
                    "Comfort": 0.9,
                    "speedPenaltyPercent": -2.5,
                    "weaponErgonomicPenalty": 0,
                    "Grids": [
                        {
                            "_props": {
                                "cellsH": 4,
                                "cellsV": 4
                            }
                        }
                    ],
                    "Slots": []
                }
            }
            """);

            var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
            var result = generator.Generate(Path.Combine(workspaceRoot, "generated-output"));

            var patchPath = Path.Combine(result.OutputPath, "user_templates", "file-processing-context-disabled-item_realism_patch.json");
            Assert.True(File.Exists(patchPath), $"缺少输出文件: {patchPath}");

            var root = JsonNode.Parse(File.ReadAllText(patchPath))!.AsObject();
            Assert.DoesNotContain("test_disabled_weapon", root.Select(pair => pair.Key));
            Assert.Contains("test_enabled_gear", root.Select(pair => pair.Key));
    }

    [Fact]
    public void SampleRangeValue_UsesProvidedRandomInstance()
    {
            var workspaceRoot = CreateGeneratorWorkspace("rng-context-sample-range");
            var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
            var assembly = typeof(RealismPatchGenerator.Core.RealismPatchGenerator).Assembly;

            var compatibleRandomType = assembly.GetType("RealismPatchGenerator.Core.CompatibleRandom", throwOnError: true)!;
            var randomCtor = compatibleRandomType.GetConstructor(new[] { typeof(uint) })!;
            var sampleRangeValue = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                .GetMethod(
                    "SampleRangeValue",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null,
                    types: [typeof(JsonNode), typeof(double), typeof(double), typeof(bool), compatibleRandomType],
                    modifiers: null);

            Assert.NotNull(sampleRangeValue);

            var firstRandom = randomCtor.Invoke([123u]);
            var secondRandom = randomCtor.Invoke([456u]);

            var firstResult = (JsonNode?)sampleRangeValue!.Invoke(generator, [JsonValue.Create(50.0)!, 10.0, 90.0, false, firstRandom]);
            var secondResult = (JsonNode?)sampleRangeValue.Invoke(generator, [JsonValue.Create(50.0)!, 10.0, 90.0, false, secondRandom]);

            Assert.NotNull(firstResult);
            Assert.NotNull(secondResult);
            Assert.NotEqual(firstResult!.GetValue<double>(), secondResult!.GetValue<double>());
    }

        [Fact]
        public void SameSeed_GenerateTwice_ProducesIdenticalRangeDrivenOutput()
        {
                var workspaceRoot = CreateGeneratorWorkspace("rng-context-determinism");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "rng-context-determinism.json");
                File.WriteAllText(inputPath, """
                {
                    "test_rng_ammo": {
                        "$type": "RealismMod.Ammo, RealismMod",
                        "ItemID": "test_rng_ammo",
                        "Name": "Test RNG Ammo",
                        "Caliber": "Caliber9x19PARA",
                        "Weight": 0.012,
                        "Damage": 52,
                        "PenetrationPower": 18,
                        "InitialSpeed": 410,
                        "ArmorDamage": 18
                    },
                    "test_rng_weapon": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_rng_weapon",
                        "parentId": "5447b5f14bdc2d61278b4567",
                        "Name": "Test RNG Weapon",
                        "WeapType": "rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var firstOutput = GenerateOutputText(workspaceRoot, seed: 12345, outputDirectoryName: "generated-output-a", outputFileName: "rng-context-determinism_realism_patch.json");
                var secondOutput = GenerateOutputText(workspaceRoot, seed: 12345, outputDirectoryName: "generated-output-b", outputFileName: "rng-context-determinism_realism_patch.json");

                Assert.Equal(firstOutput, secondOutput);
        }

        [Fact]
        public void DifferentSeeds_StandardWeaponGeneration_ProducesDifferentOutput()
        {
                var workspaceRoot = CreateGeneratorWorkspace("rng-context-different-seeds");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "rng-context-different-seeds.json");
                File.WriteAllText(inputPath, """
                {
                    "test_diffseed_weapon": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_diffseed_weapon",
                        "parentId": "5447b5f14bdc2d61278b4567",
                        "Name": "Test DiffSeed Weapon",
                        "WeapType": "rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var firstOutput = GenerateOutputText(workspaceRoot, seed: 12345, outputDirectoryName: "generated-output-a", outputFileName: "rng-context-different-seeds_realism_patch.json");
                var secondOutput = GenerateOutputText(workspaceRoot, seed: 54321, outputDirectoryName: "generated-output-b", outputFileName: "rng-context-different-seeds_realism_patch.json");

                Assert.NotEqual(firstOutput, secondOutput);
        }

    [Fact]
    public void StoredPatches_FromMultipleTypes_RemainResolvableByItemId()
    {
            var workspaceRoot = CreateGeneratorWorkspace("patch-store-multi-type-lookup");
            var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var inputPath = Path.Combine(inputRoot, "patch-store-multi-type-lookup.json");
            File.WriteAllText(inputPath, """
            {
                "test_patchstore_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_patchstore_weapon",
                    "parentId": "5447b5e04bdc2d62278b4567",
                    "Name": "PatchStore Weapon",
                    "WeapType": "rifle",
                    "LoyaltyLevel": 2,
                    "Price": 42000,
                    "Ergonomics": 62,
                    "VerticalRecoil": 82,
                    "HorizontalRecoil": 164,
                    "Dispersion": 7,
                    "Convergence": 14,
                    "RecoilIntensity": 0.18,
                    "AutoROF": 650,
                    "SemiROF": 320,
                    "Weight": 3.7
                },
                "test_patchstore_ammo": {
                    "$type": "RealismMod.Ammo, RealismMod",
                    "ItemID": "test_patchstore_ammo",
                    "Name": "PatchStore Ammo",
                    "Caliber": "Caliber9x19PARA",
                    "Weight": 0.012,
                    "Damage": 52,
                    "PenetrationPower": 18,
                    "InitialSpeed": 410,
                    "ArmorDamage": 18
                },
                "test_patchstore_gear": {
                    "$type": "RealismMod.Gear, RealismMod",
                    "ItemID": "test_patchstore_gear",
                    "Name": "PatchStore Rig",
                    "LoyaltyLevel": 1,
                    "Price": 20000,
                    "ReloadSpeedMulti": 1.0,
                    "Comfort": 0.9,
                    "speedPenaltyPercent": -2.5,
                    "weaponErgonomicPenalty": 0,
                    "Grids": [
                        {
                            "_props": {
                                "cellsH": 4,
                                "cellsV": 4
                            }
                        }
                    ],
                    "Slots": []
                }
            }
            """);

            var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
            generator.Generate(Path.Combine(workspaceRoot, "generated-output"));

            var tryGetStoredPatchById = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                .GetMethod("TryGetStoredPatchById", BindingFlags.Instance | BindingFlags.NonPublic)!;

            var weaponArgs = new object?[] { "test_patchstore_weapon", null };
            var ammoArgs = new object?[] { "test_patchstore_ammo", null };
            var gearArgs = new object?[] { "test_patchstore_gear", null };

            Assert.True((bool)tryGetStoredPatchById.Invoke(generator, weaponArgs)!);
            Assert.True((bool)tryGetStoredPatchById.Invoke(generator, ammoArgs)!);
            Assert.True((bool)tryGetStoredPatchById.Invoke(generator, gearArgs)!);

            var weaponPatch = Assert.IsType<JsonObject>(weaponArgs[1]);
            var ammoPatch = Assert.IsType<JsonObject>(ammoArgs[1]);
            var gearPatch = Assert.IsType<JsonObject>(gearArgs[1]);

            Assert.Equal("RealismMod.Gun, RealismMod", weaponPatch["$type"]?.GetValue<string>());
            Assert.Equal("RealismMod.Ammo, RealismMod", ammoPatch["$type"]?.GetValue<string>());
            Assert.Equal("RealismMod.Gear, RealismMod", gearPatch["$type"]?.GetValue<string>());
    }

    [Fact]
    public void CrossFileCloneResolution_RemainsValidWhenOutputFlushIsDeferredPerFile()
    {
            var workspaceRoot = CreateGeneratorWorkspace("output-merge-cross-file-clone");
            var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var baseInputPath = Path.Combine(inputRoot, "a-base-weapon.json");
            File.WriteAllText(baseInputPath, """
            {
                "test_base_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_base_weapon",
                    "parentId": "5447b5e04bdc2d62278b4567",
                    "Name": "Test Base Weapon",
                    "WeapType": "rifle",
                    "LoyaltyLevel": 2,
                    "Price": 42000,
                    "Ergonomics": 62,
                    "VerticalRecoil": 82,
                    "HorizontalRecoil": 164,
                    "Dispersion": 7,
                    "Convergence": 14,
                    "RecoilIntensity": 0.18,
                    "AutoROF": 650,
                    "SemiROF": 320,
                    "Weight": 3.7
                }
            }
            """);

            var cloneInputPath = Path.Combine(inputRoot, "b-clone-weapon.json");
            File.WriteAllText(cloneInputPath, """
            {
                "test_clone_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_clone_weapon",
                    "TemplateID": "test_base_weapon",
                    "parentId": "5447b5e04bdc2d62278b4567",
                    "Name": "Test Clone Weapon",
                    "LoyaltyLevel": 2,
                    "Price": 43000,
                    "Ergonomics": 58,
                    "VerticalRecoil": 88,
                    "HorizontalRecoil": 170,
                    "Dispersion": 8,
                    "Convergence": 15,
                    "RecoilIntensity": 0.20,
                    "AutoROF": 620,
                    "SemiROF": 300,
                    "Weight": 3.9
                }
            }
            """);

            var basePatch = GenerateSinglePatch(workspaceRoot, "a-base-weapon_realism_patch.json", "test_base_weapon");
            var clonePatch = GenerateSinglePatch(workspaceRoot, "b-clone-weapon_realism_patch.json", "test_clone_weapon");

            Assert.Equal("RealismMod.Gun, RealismMod", basePatch["$type"]?.GetValue<string>());
            Assert.Equal("RealismMod.Gun, RealismMod", clonePatch["$type"]?.GetValue<string>());
            Assert.Equal("test_clone_weapon", clonePatch["ItemID"]?.GetValue<string>());
    }

    [Fact]
    public void MultiItemFile_ItemOrderRemainsStableAfterPerFileBatchFlush()
    {
            var workspaceRoot = CreateGeneratorWorkspace("output-merge-item-order");
            var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var inputPath = Path.Combine(inputRoot, "output-merge-item-order.json");
            File.WriteAllText(inputPath, """
            {
                "test_merge_weapon": {
                    "$type": "RealismMod.Gun, RealismMod",
                    "ItemID": "test_merge_weapon",
                    "parentId": "5447b5e04bdc2d62278b4567",
                    "Name": "Test Merge Weapon",
                    "WeapType": "rifle",
                    "LoyaltyLevel": 2,
                    "Price": 42000,
                    "Ergonomics": 62,
                    "VerticalRecoil": 82,
                    "HorizontalRecoil": 164,
                    "Dispersion": 7,
                    "Convergence": 14,
                    "RecoilIntensity": 0.18,
                    "AutoROF": 650,
                    "SemiROF": 320,
                    "Weight": 3.7
                },
                "test_merge_ammo": {
                    "$type": "RealismMod.Ammo, RealismMod",
                    "ItemID": "test_merge_ammo",
                    "Name": "Test Merge Ammo",
                    "Caliber": "Caliber9x19PARA",
                    "Weight": 0.012,
                    "Damage": 52,
                    "PenetrationPower": 18,
                    "InitialSpeed": 410,
                    "ArmorDamage": 18
                },
                "test_merge_gear": {
                    "$type": "RealismMod.Gear, RealismMod",
                    "ItemID": "test_merge_gear",
                    "Name": "Test Merge Rig",
                    "LoyaltyLevel": 1,
                    "Price": 20000,
                    "ReloadSpeedMulti": 1.0,
                    "Comfort": 0.9,
                    "speedPenaltyPercent": -2.5,
                    "weaponErgonomicPenalty": 0,
                    "Grids": [
                        {
                            "_props": {
                                "cellsH": 4,
                                "cellsV": 4
                            }
                        }
                    ],
                    "Slots": []
                }
            }
            """);

            var outputText = GenerateOutputText(workspaceRoot, seed: 12345, outputDirectoryName: "generated-output", outputFileName: "output-merge-item-order_realism_patch.json");
            var root = JsonNode.Parse(outputText)!.AsObject();

            Assert.Equal(["test_merge_weapon", "test_merge_ammo", "test_merge_gear"], root.Select(pair => pair.Key).ToArray());
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }

        [Fact]
        public void GearPrice_IsRecalculatedWithinProfileRange()
        {
                var workspaceRoot = CreateGeneratorWorkspace("gear-price");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var gearInputPath = Path.Combine(inputRoot, "price-test-gear.json");
                File.WriteAllText(gearInputPath, """
                {
                    "test_gear_large_backpack": {
                        "$type": "RealismMod.Gear, RealismMod",
                        "ItemID": "test_gear_large_backpack",
                        "Name": "Test Large Backpack",
                        "LoyaltyLevel": 1,
                        "Price": 999999,
                        "ReloadSpeedMulti": 1.0,
                        "Comfort": 0.82,
                        "speedPenaltyPercent": -4.4,
                        "weaponErgonomicPenalty": 0,
                        "Grids": [
                            {
                                "_props": {
                                    "cellsH": 7,
                                    "cellsV": 6
                                }
                            }
                        ],
                        "Slots": [
                            {},
                            {}
                        ]
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "price-test-gear_realism_patch.json", "test_gear_large_backpack");
                var price = patch["Price"]!.GetValue<int>();

                Assert.InRange(price, 18000, 26000);
                Assert.NotEqual(999999, price);
        }

        [Fact]
        public void WeaponPrice_IsRecalculatedWithinProfileRange()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-price");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "price-test-weapon.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_smg": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_smg",
                        "parentId": "5447b5e04bdc2d62278b4567",
                        "Name": "Test SMG 9x19",
                        "WeapType": "smg",
                        "LoyaltyLevel": 2,
                        "Price": 999999,
                        "Ergonomics": 96,
                        "VerticalRecoil": 48,
                        "HorizontalRecoil": 78,
                        "Dispersion": 6,
                        "Convergence": 16,
                        "RecoilIntensity": 0.1,
                        "AutoROF": 850,
                        "SemiROF": 360,
                        "Weight": 2.6
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "price-test-weapon_realism_patch.json", "test_weapon_smg");
                var price = patch["Price"]!.GetValue<int>();

                Assert.InRange(price, 22000, 65000);
                Assert.NotEqual(999999, price);
        }

        [Fact]
        public void WeaponName_SubstringOnlyKeyword_DoesNotOverrideRealTokenProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_token_profile",
                        "Name": "Test Pistolero Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-token-profile_realism_patch.json", "test_weapon_token_profile");

                Assert.Equal("rifle", patch["WeapType"]?.GetValue<string>());
        }

        [Fact]
        public void WeaponName_SubstringOnlyStockKeyword_DoesNotForceStocklessProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-stock-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-stock-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_stock_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_stock_token_profile",
                        "Name": "Test Stocklesser Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-stock-token-profile_realism_patch.json", "test_weapon_stock_token_profile");

                Assert.NotNull(patch["BaseReloadSpeedMulti"]);
        }

        [Fact]
        public void WeaponName_SubstringOnlyCaliberKeyword_DoesNotForcePistolCaliberProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-caliber-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-caliber-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_caliber_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_caliber_token_profile",
                        "Name": "Test Pistolero Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-caliber-token-profile_realism_patch.json", "test_weapon_caliber_token_profile");

                Assert.Null(patch["Velocity"]);
        }

        [Fact]
        public void WeaponName_SubstringOnlyShotgunKeyword_DoesNotOverrideAssaultProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-shotgun-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-shotgun-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_shotgun_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_shotgun_token_profile",
                        "Name": "Test Shotgunner Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-shotgun-token-profile_realism_patch.json", "test_weapon_shotgun_token_profile");

                Assert.Equal("rifle", patch["WeapType"]?.GetValue<string>());
        }

        [Fact]
        public void WeaponName_SubstringOnlyLauncherKeyword_DoesNotOverrideProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-launcher-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-launcher-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_launcher_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_launcher_token_profile",
                        "Name": "Test Relauncher Tool",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-launcher-token-profile_realism_patch.json", "test_weapon_launcher_token_profile");

                Assert.Null(patch["WeapType"]);
        }

        [Fact]
        public void WeaponName_SubstringOnlySniperKeyword_DoesNotOverrideAssaultProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-sniper-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-sniper-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_sniper_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_sniper_token_profile",
                        "Name": "Test Gunsniper Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-sniper-token-profile_realism_patch.json", "test_weapon_sniper_token_profile");

                Assert.Equal("rifle", patch["WeapType"]?.GetValue<string>());
        }

        [Fact]
        public void WeaponName_SubstringOnlySmgKeyword_DoesNotOverrideAssaultProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("weapon-smg-token-profile");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var weaponInputPath = Path.Combine(inputRoot, "weapon-smg-token-profile.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_smg_token_profile": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_smg_token_profile",
                        "Name": "Test Cosmg Rifle",
                        "LoyaltyLevel": 2,
                        "Price": 42000,
                        "Ergonomics": 62,
                        "VerticalRecoil": 82,
                        "HorizontalRecoil": 164,
                        "Dispersion": 7,
                        "Convergence": 14,
                        "RecoilIntensity": 0.18,
                        "AutoROF": 650,
                        "SemiROF": 320,
                        "Weight": 3.7
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "weapon-smg-token-profile_realism_patch.json", "test_weapon_smg_token_profile");

                Assert.Equal("rifle", patch["WeapType"]?.GetValue<string>());
        }

        [Fact]
        public void AttachmentPrice_IsRecalculatedWithinProfileRange()
        {
                var workspaceRoot = CreateGeneratorWorkspace("attachment-price");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var modInputPath = Path.Combine(inputRoot, "price-test-mod.json");
                File.WriteAllText(modInputPath, """
                {
                    "test_mod_suppressor": {
                        "$type": "RealismMod.WeaponMod, RealismMod",
                        "ItemID": "test_mod_suppressor",
                        "parentId": "550aa4cd4bdc2dd8348b456c",
                        "Name": "Test 5.56 suppressor",
                        "ModType": "suppressor",
                        "LoyaltyLevel": 2,
                        "Price": 999999,
                        "Weight": 0.62,
                        "Ergonomics": -10,
                        "VerticalRecoil": -13,
                        "HorizontalRecoil": -9,
                        "Loudness": -32,
                        "Flash": -55,
                        "Accuracy": 2,
                        "AimSpeed": -9
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "price-test-mod_realism_patch.json", "test_mod_suppressor");
                var price = patch["Price"]!.GetValue<int>();

                Assert.InRange(price, 18000, 65000);
                Assert.NotEqual(999999, price);
        }

        [Fact]
        public void StandardWeaponTemplate_WithTemplateIdClone_IsStillRecognizedAndExported()
        {
                var workspaceRoot = CreateGeneratorWorkspace("standard-templateid-weapon");
                var weaponsRoot = Path.Combine(workspaceRoot, "input", "weapons");
                Directory.CreateDirectory(weaponsRoot);

                var weaponInputPath = Path.Combine(weaponsRoot, "AssaultRifleTemplates.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_weapon_assault_base": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_assault_base",
                        "Name": "Test Assault Base",
                        "WeapType": "bullpup",
                        "LoyaltyLevel": 3,
                        "Ergonomics": 85,
                        "VerticalRecoil": 65,
                        "HorizontalRecoil": 160,
                        "Dispersion": 6,
                        "Convergence": 13.5,
                        "RecoilIntensity": 0.19,
                        "AutoROF": 710,
                        "SemiROF": 340,
                        "Weight": 1.51
                    },
                    "test_weapon_assault_clone": {
                        "$type": "RealismMod.Gun, RealismMod",
                        "ItemID": "test_weapon_assault_clone",
                        "Name": "Test Assault Clone",
                        "TemplateID": "test_weapon_assault_base"
                    }
                }
                """);

                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
                var result = generator.Generate(Path.Combine(workspaceRoot, "generated-output"));
                var patchPath = Path.Combine(result.OutputPath, "weapons", "AssaultRifleTemplates.json");

                Assert.True(File.Exists(patchPath), $"缺少输出文件: {patchPath}");

                var root = JsonNode.Parse(File.ReadAllText(patchPath))!.AsObject();
                Assert.NotNull(root["test_weapon_assault_base"]);
                Assert.NotNull(root["test_weapon_assault_clone"]);
                Assert.Equal(2, root.Count);

                var clone = root["test_weapon_assault_clone"]!.AsObject();
                Assert.Equal("RealismMod.Gun, RealismMod", clone["$type"]?.GetValue<string>());
                Assert.Equal("test_weapon_assault_clone", clone["ItemID"]?.GetValue<string>());
                Assert.Equal("Test Assault Clone", clone["Name"]?.GetValue<string>());
                Assert.Equal("bullpup", clone["WeapType"]?.GetValue<string>());
                Assert.Equal(710, clone["AutoROF"]?.GetValue<int>());
                Assert.Equal(340, clone["SemiROF"]?.GetValue<int>());
                Assert.Null(clone["TemplateID"]);
        }

        [Fact]
        public void WttArmory_templates_WithTemplateClone_ExportsCompleteWeaponPatch()
        {
                var workspaceRoot = CreateGeneratorWorkspace("wtt-template-weapon");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var weaponInputPath = Path.Combine(inputRoot, "WTT - Armory_WeaponSample.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_wtt_weapon": {
                        "itemTplToClone": "63171672192e68c5460cebc5",
                        "parentId": "WEAPONS_ASSAULTRIFLES",
                        "handbookParentId": "WEAPONS_ASSAULTRIFLES",
                        "overrideProperties": {
                            "Ergonomics": 91,
                            "SingleFireRate": 550,
                            "ConflictingItems": ["test_conflict_item"],
                            "Weight": 1.62
                        },
                        "locales": {
                            "en": {
                                "name": "Test WTT Weapon",
                                "shortName": "WTT",
                                "description": "WTT weapon sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "WTT - Armory_WeaponSample_realism_patch.json", "test_wtt_weapon");

                Assert.Equal("RealismMod.Gun, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test WTT Weapon", patch["Name"]?.GetValue<string>());
                Assert.Equal("bullpup", patch["WeapType"]?.GetValue<string>());
                Assert.InRange(patch["Ergonomics"]?.GetValue<int>() ?? -1, 79, 97);
                Assert.Equal(550, patch["SingleFireRate"]?.GetValue<int>());
                Assert.Equal(1, patch["ConflictingItems"]?.AsArray().Count);
                Assert.Null(patch["itemTplToClone"]);
        }

        [Fact]
        public void TryResolveTemplateCloneByIdOrAlias_CachesResolvedAliasResult()
        {
                var workspaceRoot = CreateGeneratorWorkspace("template-alias-cache-hit");
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);

                var ensureAuditContextLoaded = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("EnsureAuditContextLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ensureAuditContextLoaded.Invoke(generator, null);

                var metadataCacheField = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetField("templateMetadataCache", BindingFlags.Instance | BindingFlags.NonPublic);
                var resolveMethod = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("TryResolveTemplateCloneByIdOrAlias", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Assert.NotNull(metadataCacheField);

                var metadataCache = metadataCacheField!.GetValue(generator)!;
                var cacheField = metadataCache.GetType()
                    .GetField("templateAliasCache", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(cacheField);

                var args = new object?[] { "weapon_izhmash_ak101_556x45", null, null };
                var first = (bool)resolveMethod.Invoke(generator, args)!;
                var second = (bool)resolveMethod.Invoke(generator, args)!;
                var cache = (System.Collections.IDictionary)cacheField!.GetValue(metadataCache)!;

                Assert.True(first);
                Assert.True(second);
                Assert.True(cache.Contains("weapon_izhmash_ak101_556x45"));
        }

        [Fact]
        public void TemplateRepository_Reload_ClearsResolvedAliasCacheAndRebuildsAliasIndex()
        {
                var workspaceRoot = CreateGeneratorWorkspace("template-repo-reload-cache");
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);

                var repositoryField = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetField("templateRepository", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(repositoryField);

                var repository = repositoryField!.GetValue(generator)!;
                var reloadMethod = repository.GetType()
                    .GetMethod("Reload", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.NotNull(reloadMethod);

                var ensureAuditContextLoaded = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("EnsureAuditContextLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ensureAuditContextLoaded.Invoke(generator, null);

                var resolveMethod = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("TryResolveTemplateCloneByIdOrAlias", BindingFlags.Instance | BindingFlags.NonPublic)!;

                var firstArgs = new object?[] { "weapon_izhmash_ak101_556x45", null, null };
                Assert.True((bool)resolveMethod.Invoke(generator, firstArgs)!);
                var resolvedIdBefore = (string)firstArgs[1]!;
                Assert.False(string.IsNullOrWhiteSpace(resolvedIdBefore));

                reloadMethod.Invoke(repository, null);

                var secondArgs = new object?[] { "weapon_izhmash_ak101_556x45", null, null };
                Assert.True((bool)resolveMethod.Invoke(generator, secondArgs)!);
                var resolvedIdAfter = (string)secondArgs[1]!;

                Assert.Equal(resolvedIdBefore, resolvedIdAfter);
                Assert.IsType<JsonObject>(secondArgs[2]);
        }

        [Fact]
        public void TryResolveTemplateCloneByIdOrAlias_CachedAliasAndDirectIdReturnSameTemplateInstance()
        {
                var workspaceRoot = CreateGeneratorWorkspace("template-alias-instance-same");
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);

                var ensureAuditContextLoaded = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("EnsureAuditContextLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ensureAuditContextLoaded.Invoke(generator, null);

                var resolveMethod = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("TryResolveTemplateCloneByIdOrAlias", BindingFlags.Instance | BindingFlags.NonPublic)!;

                var aliasArgs = new object?[] { "weapon_izhmash_ak101_556x45", null, null };
                Assert.True((bool)resolveMethod.Invoke(generator, aliasArgs)!);
                var resolvedId = (string)aliasArgs[1]!;
                Assert.False(string.IsNullOrWhiteSpace(resolvedId));
                var aliasTemplate = Assert.IsType<JsonObject>(aliasArgs[2]);

                var directArgs = new object?[] { resolvedId, null, null };
                Assert.True((bool)resolveMethod.Invoke(generator, directArgs)!);
                var directTemplate = Assert.IsType<JsonObject>(directArgs[2]);

                Assert.Same(directTemplate, aliasTemplate);
        }

        [Fact]
        public void IsWeapon_CachesParentIdResult()
        {
                var workspaceRoot = CreateGeneratorWorkspace("template-metadata-cache-weapon");
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);

                var ensureAuditContextLoaded = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("EnsureAuditContextLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ensureAuditContextLoaded.Invoke(generator, null);

                var metadataCacheField = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetField("templateMetadataCache", BindingFlags.Instance | BindingFlags.NonPublic);
                var isWeaponMethod = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("IsWeapon", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Assert.NotNull(metadataCacheField);

                var metadataCache = metadataCacheField!.GetValue(generator)!;
                var cacheField = metadataCache.GetType()
                    .GetField("isWeaponCache", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(cacheField);

                var first = (bool)isWeaponMethod.Invoke(generator, new object?[] { "5447b5fc4bdc2d87278b4567" })!;
                var second = (bool)isWeaponMethod.Invoke(generator, new object?[] { "5447b5fc4bdc2d87278b4567" })!;

                var cache = (System.Collections.IDictionary)cacheField!.GetValue(metadataCache)!;

                Assert.True(first);
                Assert.True(second);
                Assert.True(cache.Contains("5447b5fc4bdc2d87278b4567"));
                Assert.True((bool)cache["5447b5fc4bdc2d87278b4567"]!);
        }

        [Fact]
        public void IsWeapon_CachesMissingParentIdAsFalse()
        {
                var workspaceRoot = CreateGeneratorWorkspace("template-metadata-cache-miss");
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);

                var ensureAuditContextLoaded = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("EnsureAuditContextLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!;
                ensureAuditContextLoaded.Invoke(generator, null);

                var metadataCacheField = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetField("templateMetadataCache", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var isWeaponMethod = typeof(RealismPatchGenerator.Core.RealismPatchGenerator)
                    .GetMethod("IsWeapon", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var metadataCache = metadataCacheField.GetValue(generator)!;
                var cacheField = metadataCache.GetType()
                    .GetField("isWeaponCache", BindingFlags.Instance | BindingFlags.NonPublic)!;

                var result = (bool)isWeaponMethod.Invoke(generator, new object?[] { "deadbeef0000000000000000" })!;
                var cache = (System.Collections.IDictionary)cacheField.GetValue(metadataCache)!;

                Assert.False(result);
                Assert.True(cache.Contains("deadbeef0000000000000000"));
                Assert.False((bool)cache["deadbeef0000000000000000"]!);
        }

        [Fact]
        public void ProfileInferenceService_DoesNotDependOn_RealismPatchGenerator_NameInferenceHelpers()
        {
                var helperNames = new[]
                {
                    "InferMagazineProfile",
                    "ExtractMagCapacity",
                    "InferBarrelProfileFromName",
                    "ExtractBarrelLengthMm",
                    "IsHandguardLikeName",
                    "InferHandguardProfileFromName",
                    "InferSuppressorProfileFromName",
                    "InferSightProfileFromName",
                    "InferModStockProfile",
                    "ContainsAnyKeyword"
                };

                var generatorType = typeof(RealismPatchGenerator.Core.RealismPatchGenerator);
                var profileInferenceText = File.ReadAllText(Path.Combine(repoRoot, "RealismPatchGenerator.Core", "ProfileInferenceService.cs"));

                foreach (var helperName in helperNames)
                {
                    Assert.DoesNotContain($"RealismPatchGenerator.{helperName}", profileInferenceText, StringComparison.Ordinal);
                    Assert.Null(generatorType.GetMethod(helperName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                }
        }

        [Fact]
        public void ItemInfoFactory_DoesNotDependOn_RealismPatchGenerator_TextHelpers()
        {
                var helperNames = new[]
                {
                    "ExtractLocalizedName",
                    "FirstNonEmpty",
                    "SelectBestDisplayName",
                    "ExtractEffectiveInputFields",
                    "ExtractProperties",
                    "GetLegacyItemNode",
                    "ResolveEffectiveModType"
                };

                var generatorType = typeof(RealismPatchGenerator.Core.RealismPatchGenerator);
                var itemInfoFactoryText = File.ReadAllText(Path.Combine(repoRoot, "RealismPatchGenerator.Core", "ItemInfoFactory.cs"));

                foreach (var helperName in helperNames)
                {
                    Assert.DoesNotContain($"RealismPatchGenerator.{helperName}", itemInfoFactoryText, StringComparison.Ordinal);
                    Assert.Null(generatorType.GetMethod(helperName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                }
        }

        [Fact]
        public void ItemInfoFactory_DoesNotDependOn_RealismPatchGenerator_FieldPermissionHelpers()
        {
                var helperNames = new[]
                {
                    "CreateAllowedPatchFieldSet",
                    "AddRuleAllowedFieldsToSet",
                    "AddRuleAllowedFields",
                    "AddRequiredAllowedFields",
                    "AddRequiredAllowedFieldsToSet",
                    "AddRangeFieldNames",
                    "AddFieldNames",
                    "CreateAllowedFieldMap",
                    "TryAddCanonicalField"
                };

                var generatorType = typeof(RealismPatchGenerator.Core.RealismPatchGenerator);
                var itemInfoFactoryText = File.ReadAllText(Path.Combine(repoRoot, "RealismPatchGenerator.Core", "ItemInfoFactory.cs"));

                Assert.DoesNotContain("generator.CreateAllowedPatchFieldSet", itemInfoFactoryText, StringComparison.Ordinal);

                foreach (var helperName in helperNames)
                {
                    Assert.Null(generatorType.GetMethod(helperName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
                }
        }

        [Fact]
            public void WttArmory_templates_MagazineClone_ExportsMagazineRealismFieldsOnly()
        {
                var workspaceRoot = CreateGeneratorWorkspace("wtt-template-magazine");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var magazineInputPath = Path.Combine(inputRoot, "WTT - Armory_Attachment_Magazines.json");
                File.WriteAllText(magazineInputPath, """
                {
                    "test_wtt_magazine": {
                        "itemTplToClone": "5fc3e466187fea44d52eda90",
                        "parentId": "5448bc234bdc2d3c308b4569",
                        "handbookParentId": "5b5f754a86f774094242f19b",
                        "overrideProperties": {
                            "Cartridges": [
                                {
                                    "_id": "test_cartridges_slot",
                                    "_max_count": 50,
                                    "_name": "cartridges",
                                    "_parent": "test_wtt_magazine",
                                    "_props": {
                                        "filters": [
                                            {
                                                "Filter": [
                                                    "5e81f423763d9f754677bf2e"
                                                ]
                                            }
                                        ]
                                    },
                                    "_proto": "5748538b2459770af276a261"
                                }
                            ],
                            "Weight": 0.23,
                            "CheckTimeModifier": 8,
                            "LoadUnloadModifier": 5,
                            "ConflictingItems": ["test_conflict_magazine_item"]
                        },
                        "locales": {
                            "en": {
                                "name": "Test WTT Magazine",
                                "shortName": "WTT MAG",
                                "description": "WTT magazine sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "WTT - Armory_Attachment_Magazines_realism_patch.json", "test_wtt_magazine");

                Assert.Equal("RealismMod.WeaponMod, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test WTT Magazine", patch["Name"]?.GetValue<string>());
                Assert.Equal("magazine", patch["ModType"]?.GetValue<string>());
                Assert.NotNull(patch["LoadUnloadModifier"]);
                Assert.NotNull(patch["CheckTimeModifier"]);
                Assert.Equal(1, patch["ConflictingItems"]?.AsArray().Count);
                Assert.Null(patch["Cartridges"]);
                Assert.Null(patch["itemTplToClone"]);
        }

        [Fact]
        public void WttArmory_templates_WithMissingClone_FallsBackFromParentId()
        {
                var workspaceRoot = CreateGeneratorWorkspace("wtt-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var weaponInputPath = Path.Combine(inputRoot, "WTT - Armory_WeaponFallback.json");
                File.WriteAllText(weaponInputPath, """
                {
                    "test_wtt_fallback_weapon": {
                        "itemTplToClone": "missing_wtt_template_reference_1234567890",
                        "parentId": "WEAPONS_ASSAULTRIFLES",
                        "overrideProperties": {
                            "SingleFireRate": 620,
                            "Weight": 3.1,
                            "Ergonomics": 74
                        },
                        "locales": {
                            "en": {
                                "name": "Fallback WTT Weapon",
                                "shortName": "FWTT",
                                "description": "WTT fallback sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "WTT - Armory_WeaponFallback_realism_patch.json", "test_wtt_fallback_weapon");

                Assert.Equal("RealismMod.Gun, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Fallback WTT Weapon", patch["Name"]?.GetValue<string>());
                Assert.Equal(620, patch["SingleFireRate"]?.GetValue<int>());
                Assert.False(string.IsNullOrWhiteSpace(patch["WeapType"]?.GetValue<string>()));
                Assert.NotNull(patch["Weight"]);
        }

        [Fact]
        public void WttArmory_templates_ScopeClone_DoesNotLeakNonStandardInputFields()
        {
                var workspaceRoot = CreateGeneratorWorkspace("wtt-template-scope-boundary");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

            var scopeInputPath = Path.Combine(inputRoot, "WTT - Armory_Attachment_Scopes.json");
                File.WriteAllText(scopeInputPath, """
                {
                    "test_wtt_scope": {
                        "itemTplToClone": "57ac965c24597706be5f975c",
                        "overrideProperties": {
                            "AimSpeed": -5,
                            "AimStability": 11,
                            "ConflictingItems": ["scope_conflict_a"],
                            "AimSensitivity": 0.85,
                            "CalibrationDistances": [50, 100, 200],
                            "ModesCount": 2,
                            "IsAdjustableOptic": true,
                            "sightModType": "hybrid",
                            "Slots": [{ "_name": "ignored_slot" }]
                        },
                        "locales": {
                            "en": {
                                "name": "Test WTT Scope",
                                "shortName": "WTTS",
                                "description": "WTT scope boundary sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "WTT - Armory_Attachment_Scopes_realism_patch.json", "test_wtt_scope");

                Assert.Equal("RealismMod.WeaponMod, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test WTT Scope", patch["Name"]?.GetValue<string>());
                Assert.Equal("sight", patch["ModType"]?.GetValue<string>());
                Assert.NotNull(patch["AimSpeed"]);
                Assert.NotNull(patch["AimStability"]);
                Assert.Equal(1, patch["ConflictingItems"]?.AsArray().Count);
                Assert.Null(patch["AimSensitivity"]);
                Assert.Null(patch["CalibrationDistances"]);
                Assert.Null(patch["ModesCount"]);
                Assert.Null(patch["IsAdjustableOptic"]);
                Assert.Null(patch["sightModType"]);
                Assert.Null(patch["Slots"]);
        }

        [Fact]
        public void Epic_templates_WithMissingClone_PrefersHandbookParentForFallback()
        {
                var workspaceRoot = CreateGeneratorWorkspace("epic-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "EpicRangeTime-Weapons_MCX_Parts.json");
                File.WriteAllText(inputPath, """
                {
                    "test_epic_receiver": {
                        "itemTplToClone": "missing_epic_receiver_clone",
                        "parentId": "MOUNT",
                        "handbookParentId": "MOD_RECEIVER",
                        "overrideProperties": {
                            "Weight": 0.31,
                            "Ergonomics": 5,
                            "ChamberSpeed": 8
                        },
                        "locales": {
                            "en": {
                                "name": "Test Epic Receiver",
                                "shortName": "TER",
                                "description": "Epic receiver fallback sample"
                            }
                        }
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "EpicRangeTime-Weapons_MCX_Parts_realism_patch.json", "test_epic_receiver");

                Assert.Equal("RealismMod.WeaponMod, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("receiver", patch["ModType"]?.GetValue<string>());
                Assert.Equal("Test Epic Receiver", patch["Name"]?.GetValue<string>());
                Assert.NotNull(patch["ChamberSpeed"]);
        }

        [Fact]
        public void MixedTemplate_WeaponClone_FillsEmptyWeapTypeFromProfile()
        {
                var workspaceRoot = CreateGeneratorWorkspace("mixed-template-weapon-weaptype");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var mixedInputPath = Path.Combine(inputRoot, "mixed-template-weapon-weaptype.json");
                File.WriteAllText(mixedInputPath, """
                {
                    "test_mixed_weapon": {
                        "clone": "5fbcc1d9016cce60e8341ab3",
                        "isweapon": true,
                        "item": {
                            "_id": "test_mixed_weapon",
                            "_name": "test_mixed_weapon",
                            "_parent": "5447b5f14bdc2d61278b4567",
                            "_props": {
                                "Name": "Test Mixed Assault Rifle",
                                "Weight": 0.4,
                                "Ergonomics": 48,
                                "bFirerate": 800,
                                "BaseMalfunctionChance": 0.17
                            }
                        }
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "mixed-template-weapon-weaptype_realism_patch.json", "test_mixed_weapon");

                Assert.Equal("RealismMod.Gun, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("rifle", patch["WeapType"]?.GetValue<string>());
        }

        [Fact]
        public void Requisitions_templates_WithTemplateClone_ExportsPatch()
        {
                var workspaceRoot = CreateGeneratorWorkspace("requisitions-template-attachment");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "Echoes.of.Tarkov.-.Requisitions_Recievers.json");
                File.WriteAllText(inputPath, """
                {
                    "test_requisitions_receiver": {
                        "itemTplToClone": "missing_requisitions_receiver_clone",
                        "parentId": "55818b224bdc2dde698b456f",
                        "handbookParentId": "5b5f755f86f77447ec5d770e",
                        "overrideProperties": {
                            "Weight": 0.31,
                            "Ergonomics": 5,
                            "ChamberSpeed": 8
                        },
                        "locales": {
                            "en": {
                                "name": "Test Requisitions Receiver",
                                "shortName": "TRR",
                                "description": "Requisitions receiver sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "Echoes.of.Tarkov.-.Requisitions_Recievers_realism_patch.json", "test_requisitions_receiver");

                Assert.Equal("RealismMod.WeaponMod, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("mount", patch["ModType"]?.GetValue<string>());
                Assert.Equal("Test Requisitions Receiver", patch["Name"]?.GetValue<string>());
                Assert.NotNull(patch["ChamberSpeed"]);
        }

        [Fact]
        public void ConsortiumOfThings_templates_WithMissingClone_UsesWeaponFallback()
        {
                var workspaceRoot = CreateGeneratorWorkspace("consortium-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "ConsortiumOfThings_WeaponGlock22.json");
                File.WriteAllText(inputPath, """
                {
                    "test_consortium_glock22": {
                        "itemTplToClone": "missing_consortium_glock22_clone",
                        "parentId": "HANDGUN",
                        "handbookParentId": "WEAPONS_PISTOLS",
                        "overrideProperties": {
                            "Weight": 0.67,
                            "SingleFireRate": 550
                        },
                        "locales": {
                            "en": {
                                "name": "Test Consortium Glock 22",
                                "shortName": "TCG22",
                                "description": "Consortium fallback sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "ConsortiumOfThings_WeaponGlock22_realism_patch.json", "test_consortium_glock22");

                Assert.Equal("RealismMod.Gun, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("pistol", patch["WeapType"]?.GetValue<string>());
                Assert.Equal("Test Consortium Glock 22", patch["Name"]?.GetValue<string>());
                Assert.Null(patch["itemTplToClone"]);
        }

        [Fact]
        public void EcoAttachment_templates_WithMissingClone_UsesParentFallback()
        {
                var workspaceRoot = CreateGeneratorWorkspace("eco-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "Eco-Attachment Emporium_Clutch_CH.json");
                File.WriteAllText(inputPath, """
                {
                    "test_eco_charging_handle": {
                        "itemTplToClone": "missing_eco_charging_handle_clone",
                        "parentId": "55818a6f4bdc2db9688b456b",
                        "handbookParentId": "CHARGING_HANDLE",
                        "overrideProperties": {
                            "Ergonomics": 3,
                            "Weight": 0.113
                        },
                        "locales": {
                            "en": {
                                "name": "Test Eco Charging Handle",
                                "shortName": "TECH",
                                "description": "Eco charging handle fallback sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "Eco-Attachment Emporium_Clutch_CH_realism_patch.json", "test_eco_charging_handle");

                Assert.Equal("RealismMod.WeaponMod, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test Eco Charging Handle", patch["Name"]?.GetValue<string>());
                Assert.NotNull(patch["ChamberSpeed"]);
                Assert.Null(patch["itemTplToClone"]);
        }

        [Fact]
        public void Artem_templates_WithMissingClone_UsesGearFallback()
        {
                var workspaceRoot = CreateGeneratorWorkspace("artem-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "[4]新商人-Artem_Helmets.json");
                File.WriteAllText(inputPath, """
                {
                    "test_artem_helmet": {
                        "itemTplToClone": "missing_artem_helmet_clone",
                        "parentId": "5a341c4086f77401f2541505",
                        "handbookParentId": "5b47574386f77428ca22b330",
                        "overrideProperties": {
                            "Weight": 0.96,
                            "ArmorClass": "NIJ III"
                        },
                        "locales": {
                            "en": {
                                "name": "Test Artem Helmet",
                                "shortName": "TAH",
                                "description": "Artem helmet fallback sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "[4]新商人-Artem_Helmets_realism_patch.json", "test_artem_helmet");

                Assert.Equal("RealismMod.Gear, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test Artem Helmet", patch["Name"]?.GetValue<string>());
                Assert.NotNull(patch["ArmorClass"]);
                Assert.Null(patch["itemTplToClone"]);
        }

        [Fact]
        public void SptBattlepass_templates_WithMissingClone_UsesGearFallback()
        {
                var workspaceRoot = CreateGeneratorWorkspace("spt-battlepass-template-fallback");
                var inputRoot = Path.Combine(workspaceRoot, "input", "user_templates");

                var inputPath = Path.Combine(inputRoot, "[2]新物品-竞技场赛季奖励-SPT Battlepass.json");
                File.WriteAllText(inputPath, """
                {
                    "test_battlepass_facecover": {
                        "itemTplToClone": "missing_battlepass_facecover_clone",
                        "parentId": "5a341c4686f77469e155819e",
                        "handbookParentId": "5b47574386f77428ca22b32f",
                        "overrideProperties": {
                            "Weight": 1.23,
                            "ArmorClass": "NIJ III"
                        },
                        "locales": {
                            "en": {
                                "name": "Test Battlepass Facecover",
                                "shortName": "TBF",
                                "description": "Battlepass fallback sample"
                            }
                        },
                        "clearClonedProps": false
                    }
                }
                """);

                var patch = GenerateSinglePatch(workspaceRoot, "[2]新物品-竞技场赛季奖励-SPT Battlepass_realism_patch.json", "test_battlepass_facecover");

                Assert.Equal("RealismMod.Gear, RealismMod", patch["$type"]?.GetValue<string>());
                Assert.Equal("Test Battlepass Facecover", patch["Name"]?.GetValue<string>());
                Assert.NotNull(patch["ArmorClass"]);
                Assert.Null(patch["itemTplToClone"]);
        }


        private string CreateGeneratorWorkspace(string scenarioName)
        {
                var workspaceRoot = Path.Combine(basePath, scenarioName);
                Directory.CreateDirectory(Path.Combine(workspaceRoot, "input", "user_templates"));
                RuleWorkspace.EnsureInitialized(workspaceRoot);

                var sourceTemplates = RuleWorkspace.GetTemplatesDirectory(repoRoot);
                var targetTemplates = RuleWorkspace.GetTemplatesDirectory(workspaceRoot);
                CopyDirectory(sourceTemplates, targetTemplates);

                return workspaceRoot;
        }

        private JsonObject GenerateSinglePatch(string workspaceRoot, string outputFileName, string itemId)
        {
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed: 12345);
                var result = generator.Generate(Path.Combine(workspaceRoot, "generated-output"));

                var patchPath = Path.Combine(result.OutputPath, "user_templates", outputFileName);
                Assert.True(File.Exists(patchPath), $"缺少输出文件: {patchPath}");

                var root = JsonNode.Parse(File.ReadAllText(patchPath))!.AsObject();
                return root[itemId]!.AsObject();
        }

        private static string GenerateOutputText(string workspaceRoot, uint seed, string outputDirectoryName, string outputFileName)
        {
                var generator = new RealismPatchGenerator.Core.RealismPatchGenerator(workspaceRoot, seed);
                var result = generator.Generate(Path.Combine(workspaceRoot, outputDirectoryName));

                var patchPath = Path.Combine(result.OutputPath, "user_templates", outputFileName);
                Assert.True(File.Exists(patchPath), $"缺少输出文件: {patchPath}");

                return File.ReadAllText(patchPath);
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
                Directory.CreateDirectory(targetDirectory);

                foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                        var relativePath = Path.GetRelativePath(sourceDirectory, directory);
                        Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
                }

                foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                        var relativePath = Path.GetRelativePath(sourceDirectory, file);
                        var targetPath = Path.Combine(targetDirectory, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                        File.Copy(file, targetPath, overwrite: true);
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
