using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed class RuleSet
{
    public required WeaponRules Weapon { get; init; }
    public required AttachmentRules Attachment { get; init; }
    public required AmmoRules Ammo { get; init; }
    public required GearRules Gear { get; init; }
}

internal sealed class WeaponRules
{
    public required IReadOnlyDictionary<string, IReadOnlySet<string>> WeaponParentGroups { get; init; }
    public required IReadOnlyDictionary<string, NumericRange> GunClampRules { get; init; }
    public required IReadOnlyDictionary<string, NumericRange> GunPriceRanges { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponProfileRanges { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponCaliberRuleModifiers { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponStockRuleModifiers { get; init; }
    public required IReadOnlyDictionary<string, string> TemplateFileToWeaponProfile { get; init; }
    public required IReadOnlyList<KeywordProfile> CaliberProfileKeywords { get; init; }
}

internal sealed class AttachmentRules
{
    public required IReadOnlyDictionary<string, NumericRange> ModClampRules { get; init; }
    public required IReadOnlyDictionary<string, NumericRange> ModPriceRanges { get; init; }
    public required IReadOnlyDictionary<string, string> ModParentBaseProfiles { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> ModProfileRanges { get; init; }
}

internal sealed class AmmoRules
{
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoProfileRanges { get; init; }
    public required IReadOnlyList<KeywordProfile> AmmoProfileKeywords { get; init; }
    public required IReadOnlyList<KeywordProfile> AmmoSpecialKeywords { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoSpecialModifiers { get; init; }
    public required IReadOnlyDictionary<string, NumericRange> AmmoPenetrationTiers { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoPenetrationModifiers { get; init; }
}

internal sealed class GearRules
{
    public required IReadOnlyDictionary<string, NumericRange> GearClampRules { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> GearProfileRanges { get; init; }
    public required IReadOnlyDictionary<string, NumericRange> GearPriceRanges { get; init; }
}

internal readonly record struct KeywordProfile(string Profile, IReadOnlyList<string> Keywords);

internal static class RuleSetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static RuleSet Load(string basePath, Action<string> log)
    {
        var rulesDirectory = RuleWorkspace.GetRulesDirectory(basePath);
        Directory.CreateDirectory(rulesDirectory);

        var weaponPath = Path.Combine(rulesDirectory, "weapon_rules.json");
        var attachmentPath = Path.Combine(rulesDirectory, "attachment_rules.json");
        var ammoPath = Path.Combine(rulesDirectory, "ammo_rules.json");
        var gearPath = Path.Combine(rulesDirectory, "gear_rules.json");

        EnsureDefaultFile(weaponPath, BuildWeaponRulesJson(WeaponRuleData.CreateDefaultRules()), "武器", log);
        EnsureDefaultFile(attachmentPath, BuildAttachmentRulesJson(AttachmentRuleData.CreateDefaultRules()), "配件", log);
        EnsureDefaultFile(ammoPath, BuildAmmoRulesJson(AmmoRuleData.CreateDefaultRules()), "子弹", log);
        EnsureDefaultFile(gearPath, BuildGearRulesJson(GearRuleData.CreateDefaultRules()), "装备", log);

        return new RuleSet
        {
            Weapon = LoadWeaponRules(weaponPath, log) ?? WeaponRuleData.CreateDefaultRules(),
            Attachment = LoadAttachmentRules(attachmentPath, log) ?? AttachmentRuleData.CreateDefaultRules(),
            Ammo = LoadAmmoRules(ammoPath, log) ?? AmmoRuleData.CreateDefaultRules(),
            Gear = LoadGearRules(gearPath, log) ?? GearRuleData.CreateDefaultRules(),
        };
    }

    public static bool TryNormalizeRuleFile(string ruleFileName, string rawText, out string normalizedJson, out string errorMessage)
    {
        normalizedJson = string.Empty;
        errorMessage = string.Empty;

        try
        {
            var root = JsonNode.Parse(rawText)?.AsObject()
                ?? throw new InvalidOperationException("规则文件内容必须是 JSON 对象。");

            JsonObject normalized = ruleFileName switch
            {
                "weapon_rules.json" => BuildWeaponRulesJson(new WeaponRules
                {
                    WeaponParentGroups = ParseStringSetMap(root["weaponParentGroups"], "weaponParentGroups"),
                    GunClampRules = ParseRangeMap(root["gunClampRules"], "gunClampRules"),
                    GunPriceRanges = ParseRangeMap(root["gunPriceRanges"], "gunPriceRanges"),
                    WeaponProfileRanges = ParseNestedRangeMap(root["weaponProfileRanges"], "weaponProfileRanges"),
                    WeaponCaliberRuleModifiers = ParseNestedRangeMap(root["weaponCaliberRuleModifiers"], "weaponCaliberRuleModifiers"),
                    WeaponStockRuleModifiers = ParseNestedRangeMap(root["weaponStockRuleModifiers"], "weaponStockRuleModifiers"),
                    TemplateFileToWeaponProfile = ParseStringMap(root["templateFileToWeaponProfile"], "templateFileToWeaponProfile"),
                    CaliberProfileKeywords = ParseKeywordProfiles(root["caliberProfileKeywords"], "caliberProfileKeywords"),
                }),
                "attachment_rules.json" => BuildAttachmentRulesJson(new AttachmentRules
                {
                    ModClampRules = ParseRangeMap(root["modClampRules"], "modClampRules"),
                    ModPriceRanges = ParseRangeMap(root["modPriceRanges"], "modPriceRanges"),
                    ModParentBaseProfiles = ParseStringMap(root["modParentBaseProfiles"], "modParentBaseProfiles"),
                    ModProfileRanges = ParseNestedRangeMap(root["modProfileRanges"], "modProfileRanges"),
                }),
                "ammo_rules.json" => BuildAmmoRulesJson(new AmmoRules
                {
                    AmmoProfileRanges = ParseNestedRangeMap(root["ammoProfileRanges"], "ammoProfileRanges"),
                    AmmoProfileKeywords = ParseKeywordProfiles(root["ammoProfileKeywords"], "ammoProfileKeywords"),
                    AmmoSpecialKeywords = ParseKeywordProfiles(root["ammoSpecialKeywords"], "ammoSpecialKeywords"),
                    AmmoSpecialModifiers = ParseNestedRangeMap(root["ammoSpecialModifiers"], "ammoSpecialModifiers"),
                    AmmoPenetrationTiers = ParseRangeMap(root["ammoPenetrationTiers"], "ammoPenetrationTiers"),
                    AmmoPenetrationModifiers = ParseNestedRangeMap(root["ammoPenetrationModifiers"], "ammoPenetrationModifiers"),
                }),
                "gear_rules.json" => BuildGearRulesJson(new GearRules
                {
                    GearClampRules = ParseRangeMap(root["gearClampRules"], "gearClampRules"),
                    GearProfileRanges = ParseNestedRangeMap(root["gearProfileRanges"], "gearProfileRanges"),
                    GearPriceRanges = ParseRangeMap(root["gearPriceRanges"], "gearPriceRanges"),
                }),
                _ => throw new InvalidOperationException($"不支持的规则文件: {ruleFileName}"),
            };

            normalizedJson = normalized.ToJsonString(JsonOptions);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static void EnsureDefaultFile(string path, JsonObject content, string label, Action<string> log)
    {
        if (File.Exists(path))
        {
            return;
        }

        File.WriteAllText(path, content.ToJsonString(JsonOptions));
        log($"已初始化外置{label}规则: {Path.GetFileName(path)}");
    }

    private static WeaponRules? LoadWeaponRules(string path, Action<string> log)
    {
        try
        {
            var root = LoadJsonObject(path);
            return new WeaponRules
            {
                WeaponParentGroups = ParseStringSetMap(root["weaponParentGroups"], "weaponParentGroups"),
                GunClampRules = ParseRangeMap(root["gunClampRules"], "gunClampRules"),
                GunPriceRanges = ParseRangeMap(root["gunPriceRanges"], "gunPriceRanges"),
                WeaponProfileRanges = ParseNestedRangeMap(root["weaponProfileRanges"], "weaponProfileRanges"),
                WeaponCaliberRuleModifiers = ParseNestedRangeMap(root["weaponCaliberRuleModifiers"], "weaponCaliberRuleModifiers"),
                WeaponStockRuleModifiers = ParseNestedRangeMap(root["weaponStockRuleModifiers"], "weaponStockRuleModifiers"),
                TemplateFileToWeaponProfile = ParseStringMap(root["templateFileToWeaponProfile"], "templateFileToWeaponProfile"),
                CaliberProfileKeywords = ParseKeywordProfiles(root["caliberProfileKeywords"], "caliberProfileKeywords"),
            };
        }
        catch (Exception ex)
        {
            log($"加载武器外置规则失败，回退到内置规则: {ex.Message}");
            return null;
        }
    }

    private static AttachmentRules? LoadAttachmentRules(string path, Action<string> log)
    {
        try
        {
            var root = LoadJsonObject(path);
            return new AttachmentRules
            {
                ModClampRules = ParseRangeMap(root["modClampRules"], "modClampRules"),
                ModPriceRanges = ParseRangeMap(root["modPriceRanges"], "modPriceRanges"),
                ModParentBaseProfiles = ParseStringMap(root["modParentBaseProfiles"], "modParentBaseProfiles"),
                ModProfileRanges = ParseNestedRangeMap(root["modProfileRanges"], "modProfileRanges"),
            };
        }
        catch (Exception ex)
        {
            log($"加载配件外置规则失败，回退到内置规则: {ex.Message}");
            return null;
        }
    }

    private static AmmoRules? LoadAmmoRules(string path, Action<string> log)
    {
        try
        {
            var root = LoadJsonObject(path);
            return new AmmoRules
            {
                AmmoProfileRanges = ParseNestedRangeMap(root["ammoProfileRanges"], "ammoProfileRanges"),
                AmmoProfileKeywords = ParseKeywordProfiles(root["ammoProfileKeywords"], "ammoProfileKeywords"),
                AmmoSpecialKeywords = ParseKeywordProfiles(root["ammoSpecialKeywords"], "ammoSpecialKeywords"),
                AmmoSpecialModifiers = ParseNestedRangeMap(root["ammoSpecialModifiers"], "ammoSpecialModifiers"),
                AmmoPenetrationTiers = ParseRangeMap(root["ammoPenetrationTiers"], "ammoPenetrationTiers"),
                AmmoPenetrationModifiers = ParseNestedRangeMap(root["ammoPenetrationModifiers"], "ammoPenetrationModifiers"),
            };
        }
        catch (Exception ex)
        {
            log($"加载子弹外置规则失败，回退到内置规则: {ex.Message}");
            return null;
        }
    }

    private static GearRules? LoadGearRules(string path, Action<string> log)
    {
        try
        {
            var root = LoadJsonObject(path);
            return new GearRules
            {
                GearClampRules = ParseRangeMap(root["gearClampRules"], "gearClampRules"),
                GearProfileRanges = ParseNestedRangeMap(root["gearProfileRanges"], "gearProfileRanges"),
                GearPriceRanges = ParseRangeMap(root["gearPriceRanges"], "gearPriceRanges"),
            };
        }
        catch (Exception ex)
        {
            log($"加载装备外置规则失败，回退到内置规则: {ex.Message}");
            return null;
        }
    }

    private static JsonObject LoadJsonObject(string path)
    {
        return JsonNode.Parse(File.ReadAllText(path))?.AsObject()
            ?? throw new InvalidOperationException($"规则文件不是有效 JSON 对象: {path}");
    }

    private static IReadOnlyDictionary<string, string> ParseStringMap(JsonNode? node, string sectionName)
    {
        var obj = node?.AsObject() ?? throw new InvalidOperationException($"缺少对象节点: {sectionName}");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in obj)
        {
            result[pair.Key] = pair.Value?.GetValue<string>()
                ?? throw new InvalidOperationException($"{sectionName}.{pair.Key} 必须是字符串");
        }

        return new ReadOnlyDictionary<string, string>(result);
    }

    private static IReadOnlyDictionary<string, IReadOnlySet<string>> ParseStringSetMap(JsonNode? node, string sectionName)
    {
        var obj = node?.AsObject() ?? throw new InvalidOperationException($"缺少对象节点: {sectionName}");
        var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in obj)
        {
            var array = pair.Value?.AsArray() ?? throw new InvalidOperationException($"{sectionName}.{pair.Key} 必须是数组");
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in array)
            {
                var value = entry?.GetValue<string>() ?? throw new InvalidOperationException($"{sectionName}.{pair.Key} 数组项必须是字符串");
                values.Add(value);
            }

            result[pair.Key] = values;
        }

        return new ReadOnlyDictionary<string, IReadOnlySet<string>>(result);
    }

    private static IReadOnlyList<KeywordProfile> ParseKeywordProfiles(JsonNode? node, string sectionName)
    {
        var array = node?.AsArray() ?? throw new InvalidOperationException($"缺少数组节点: {sectionName}");
        var result = new List<KeywordProfile>(array.Count);
        foreach (var entry in array)
        {
            var obj = entry?.AsObject() ?? throw new InvalidOperationException($"{sectionName} 项必须是对象");
            var profile = obj["profile"]?.GetValue<string>() ?? throw new InvalidOperationException($"{sectionName} 项缺少 profile");
            var keywordsArray = obj["keywords"]?.AsArray() ?? throw new InvalidOperationException($"{sectionName}.{profile}.keywords 必须是数组");
            var keywords = keywordsArray
                .Select(keyword => keyword?.GetValue<string>() ?? throw new InvalidOperationException($"{sectionName}.{profile}.keywords 项必须是字符串"))
                .ToArray();
            result.Add(new KeywordProfile(profile, keywords));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, NumericRange> ParseRangeMap(JsonNode? node, string sectionName)
    {
        var obj = node?.AsObject() ?? throw new InvalidOperationException($"缺少对象节点: {sectionName}");
        var result = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in obj)
        {
            result[pair.Key] = ParseNumericRange(pair.Value, $"{sectionName}.{pair.Key}");
        }

        return new ReadOnlyDictionary<string, NumericRange>(result);
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> ParseNestedRangeMap(JsonNode? node, string sectionName)
    {
        var obj = node?.AsObject() ?? throw new InvalidOperationException($"缺少对象节点: {sectionName}");
        var result = new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in obj)
        {
            result[pair.Key] = ParseRangeMap(pair.Value, $"{sectionName}.{pair.Key}");
        }

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(result);
    }

    private static NumericRange ParseNumericRange(JsonNode? node, string sectionName)
    {
        var obj = node?.AsObject() ?? throw new InvalidOperationException($"缺少对象节点: {sectionName}");
        var min = obj["min"]?.GetValue<double?>() ?? throw new InvalidOperationException($"{sectionName}.min 必须是数字");
        var max = obj["max"]?.GetValue<double?>() ?? throw new InvalidOperationException($"{sectionName}.max 必须是数字");
        var preferInt = obj["preferInt"]?.GetValue<bool?>() ?? false;
        return new NumericRange(min, max, preferInt);
    }

    private static JsonObject BuildWeaponRulesJson(WeaponRules rules)
    {
        return new JsonObject
        {
            ["weaponParentGroups"] = BuildStringSetMap(rules.WeaponParentGroups),
            ["gunClampRules"] = BuildRangeMap(rules.GunClampRules),
            ["gunPriceRanges"] = BuildRangeMap(rules.GunPriceRanges),
            ["weaponProfileRanges"] = BuildNestedRangeMap(rules.WeaponProfileRanges),
            ["weaponCaliberRuleModifiers"] = BuildNestedRangeMap(rules.WeaponCaliberRuleModifiers),
            ["weaponStockRuleModifiers"] = BuildNestedRangeMap(rules.WeaponStockRuleModifiers),
            ["templateFileToWeaponProfile"] = BuildStringMap(rules.TemplateFileToWeaponProfile),
            ["caliberProfileKeywords"] = BuildKeywordProfiles(rules.CaliberProfileKeywords),
        };
    }

    private static JsonObject BuildAttachmentRulesJson(AttachmentRules rules)
    {
        return new JsonObject
        {
            ["modClampRules"] = BuildRangeMap(rules.ModClampRules),
            ["modPriceRanges"] = BuildRangeMap(rules.ModPriceRanges),
            ["modParentBaseProfiles"] = BuildStringMap(rules.ModParentBaseProfiles),
            ["modProfileRanges"] = BuildNestedRangeMap(rules.ModProfileRanges),
        };
    }

    private static JsonObject BuildAmmoRulesJson(AmmoRules rules)
    {
        return new JsonObject
        {
            ["ammoProfileRanges"] = BuildNestedRangeMap(rules.AmmoProfileRanges),
            ["ammoProfileKeywords"] = BuildKeywordProfiles(rules.AmmoProfileKeywords),
            ["ammoSpecialKeywords"] = BuildKeywordProfiles(rules.AmmoSpecialKeywords),
            ["ammoSpecialModifiers"] = BuildNestedRangeMap(rules.AmmoSpecialModifiers),
            ["ammoPenetrationTiers"] = BuildRangeMap(rules.AmmoPenetrationTiers),
            ["ammoPenetrationModifiers"] = BuildNestedRangeMap(rules.AmmoPenetrationModifiers),
        };
    }

    private static JsonObject BuildGearRulesJson(GearRules rules)
    {
        return new JsonObject
        {
            ["gearClampRules"] = BuildRangeMap(rules.GearClampRules),
            ["gearProfileRanges"] = BuildNestedRangeMap(rules.GearProfileRanges),
            ["gearPriceRanges"] = BuildRangeMap(rules.GearPriceRanges),
        };
    }

    private static JsonObject BuildStringMap(IReadOnlyDictionary<string, string> values)
    {
        var obj = new JsonObject();
        foreach (var pair in values)
        {
            obj[pair.Key] = pair.Value;
        }

        return obj;
    }

    private static JsonObject BuildStringSetMap(IReadOnlyDictionary<string, IReadOnlySet<string>> values)
    {
        var obj = new JsonObject();
        foreach (var pair in values)
        {
            var array = new JsonArray();
            foreach (var entry in pair.Value)
            {
                array.Add(entry);
            }

            obj[pair.Key] = array;
        }

        return obj;
    }

    private static JsonArray BuildKeywordProfiles(IReadOnlyList<KeywordProfile> profiles)
    {
        var array = new JsonArray();
        foreach (var profile in profiles)
        {
            var keywords = new JsonArray();
            foreach (var keyword in profile.Keywords)
            {
                keywords.Add(keyword);
            }

            array.Add(new JsonObject
            {
                ["profile"] = profile.Profile,
                ["keywords"] = keywords,
            });
        }

        return array;
    }

    private static JsonObject BuildRangeMap(IReadOnlyDictionary<string, NumericRange> ranges)
    {
        var obj = new JsonObject();
        foreach (var pair in ranges)
        {
            obj[pair.Key] = BuildNumericRange(pair.Value);
        }

        return obj;
    }

    private static JsonObject BuildNestedRangeMap(IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> values)
    {
        var obj = new JsonObject();
        foreach (var pair in values)
        {
            obj[pair.Key] = BuildRangeMap(pair.Value);
        }

        return obj;
    }

    private static JsonObject BuildNumericRange(NumericRange range)
    {
        return new JsonObject
        {
            ["min"] = range.Min,
            ["max"] = range.Max,
            ["preferInt"] = range.PreferInt,
        };
    }
}