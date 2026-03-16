using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class ItemExceptionFieldGuardServiceTests : IDisposable
{
    private readonly string basePath;

    public ItemExceptionFieldGuardServiceTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-field-guard-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
    }

    [Fact]
    public void NormalizeValue_ClampsMergedKnownFieldRange()
    {
        var result = ItemExceptionFieldGuardService.NormalizeValue("Ergonomics", JsonValue.Create(10000)!);

        Assert.True(result.WasAdjusted);
        Assert.Equal(100, result.Value.GetValue<int>());
        Assert.Equal("-15 ~ 100", result.Message);
    }

    [Fact]
    public void NormalizeValue_ClampsMergedRecoilFieldRange()
    {
        var result = ItemExceptionFieldGuardService.NormalizeValue("VerticalRecoil", JsonValue.Create(-1000)!);

        Assert.True(result.WasAdjusted);
        Assert.Equal(-45, result.Value.GetValue<int>());
        Assert.Equal("-45 ~ 700", result.Message);
    }

    [Fact]
    public void NormalizeValue_LeavesNonNumericValueUntouched()
    {
        var value = JsonValue.Create("custom string")!;
        var result = ItemExceptionFieldGuardService.NormalizeValue("Name", value);

        Assert.False(result.WasAdjusted);
        Assert.Equal("custom string", result.Value.GetValue<string>());
    }

    [Fact]
    public void DetectCategory_UsesSourceFileAndFieldHeuristics()
    {
        Assert.Equal(
            ItemExceptionFieldCategory.Attachment,
            ItemExceptionFieldGuardService.DetectCategory("output/user_templates/custom_patch.json", new JsonObject
            {
                ["Loudness"] = 10,
            }));

        Assert.Equal(
            ItemExceptionFieldCategory.Weapon,
            ItemExceptionFieldGuardService.DetectCategory("weapons/test_realism_patch.json", new JsonObject()));
    }

    [Fact]
    public void GetKnownFieldNames_ByCategory_ReturnsRestrictedFields()
    {
        var gearFields = ItemExceptionFieldGuardService.GetKnownFieldNames(ItemExceptionFieldCategory.Gear);
        var weaponFields = ItemExceptionFieldGuardService.GetKnownFieldNames(ItemExceptionFieldCategory.Weapon);

        Assert.Contains("Comfort", gearFields);
        Assert.DoesNotContain("PenetrationPower", gearFields);
        Assert.Contains("HasShoulderContact", weaponFields);
        Assert.DoesNotContain("SpallReduction", weaponFields);
    }

        [Fact]
        public void GetKnownFieldNames_IncludesTemplateOnlyGearFields()
        {
                var templateDirectory = Path.Combine(basePath, "现实主义物品模板", "gear");
                Directory.CreateDirectory(templateDirectory);
                File.WriteAllText(
                        Path.Combine(templateDirectory, "armorMasksTemplates.json"),
                        """
                        {
                            "mask-1": {
                                "$type": "RealismMod.Gear, RealismMod",
                                "ItemID": "mask-1",
                                "Name": "mask",
                                "IsGasMask": true,
                                "MaskToUse": "gp5"
                            }
                        }
                        """);

                var gearFields = ItemExceptionFieldGuardService.GetKnownFieldNames(basePath, ItemExceptionFieldCategory.Gear);
                var suggested = ItemExceptionFieldGuardService.GetSuggestedValue(basePath, "IsGasMask");

                Assert.Contains("IsGasMask", gearFields);
                Assert.Contains("MaskToUse", gearFields);
                Assert.True(suggested.GetValue<bool>());
        }

    [Fact]
    public void GetSuggestedValue_UsesNeutralNumericDefaultWhenAvailable()
    {
        var multiValue = ItemExceptionFieldGuardService.GetSuggestedValue("ReloadSpeedMulti");
        var recoilValue = ItemExceptionFieldGuardService.GetSuggestedValue("VerticalRecoil");

        Assert.Equal(1d, multiValue.GetValue<double>());
        Assert.Equal(0, recoilValue.GetValue<int>());
    }

    [Fact]
    public void GetSuggestedValue_UsesSpecialDefaultsForKnownNonNumericFields()
    {
        var shoulder = ItemExceptionFieldGuardService.GetSuggestedValue("HasShoulderContact");
        var weaponType = ItemExceptionFieldGuardService.GetSuggestedValue("WeapType");

        Assert.False(shoulder.GetValue<bool>());
        Assert.Equal(string.Empty, weaponType.GetValue<string>());
    }

    [Fact]
    public void GetGuidance_UsesLatestExternalRuleRange()
    {
        WriteGearRules(
            """
            {
              "gearClampRules": {
                "ReloadSpeedMulti": { "min": 2.0, "max": 3.0, "preferInt": false }
              },
              "gearProfileRanges": {}
            }
            """);

        var guidance = ItemExceptionFieldGuardService.GetGuidance(basePath, "ReloadSpeedMulti");

        Assert.Equal(2.0, guidance.Min);
        Assert.Equal(3.0, guidance.Max);
        Assert.Equal("2 ~ 3", guidance.FormatRange());
    }

    [Fact]
    public void GetSuggestedValue_UsesLatestExternalRuleRange()
    {
        WriteGearRules(
            """
            {
              "gearClampRules": {
                "ReloadSpeedMulti": { "min": 2.0, "max": 3.0, "preferInt": false }
              },
              "gearProfileRanges": {}
            }
            """);

        var suggested = ItemExceptionFieldGuardService.GetSuggestedValue(basePath, "ReloadSpeedMulti");

        Assert.Equal(2.5, suggested.GetValue<double>(), 3);
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }

    private void WriteGearRules(string json)
    {
        var rulesDirectory = Path.Combine(basePath, "rules");
        Directory.CreateDirectory(rulesDirectory);
        File.WriteAllText(Path.Combine(rulesDirectory, "gear_rules.json"), json);
    }
}