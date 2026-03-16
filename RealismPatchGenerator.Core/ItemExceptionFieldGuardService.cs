using System.Globalization;
using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

public enum ItemExceptionFieldCategory
{
    Unknown = 0,
    Weapon = 1,
    Attachment = 2,
    Gear = 3,
    Ammo = 4,
}

public sealed class ItemExceptionFieldGuidance
{
    public required string FieldName { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public bool PreferInt { get; init; }
    public string Reason { get; init; } = string.Empty;

    public string FormatRange()
    {
        if (Min is null || Max is null)
        {
            return string.Empty;
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            PreferInt ? "{0:0} ~ {1:0}" : "{0:0.##} ~ {1:0.##}",
            Min.Value,
            Max.Value);
    }
}

public sealed class ItemExceptionFieldNormalizationResult
{
    public required JsonNode Value { get; init; }
    public bool WasAdjusted { get; init; }
    public string Message { get; init; } = string.Empty;
}

public static class ItemExceptionFieldGuardService
{
    private static readonly GuardData DefaultGuardData = BuildGuardData(new RuleSet
    {
        Weapon = WeaponRuleData.CreateDefaultRules(),
        Attachment = AttachmentRuleData.CreateDefaultRules(),
        Ammo = AmmoRuleData.CreateDefaultRules(),
        Gear = GearRuleData.CreateDefaultRules(),
    }, null);
    private static readonly Dictionary<string, GuardData> GuardDataCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock GuardDataCacheLock = new();

    public static IReadOnlyList<string> GetKnownFieldNames()
    {
        return DefaultGuardData.KnownFieldNames;
    }

    public static IReadOnlyList<string> GetKnownFieldNames(ItemExceptionFieldCategory category)
    {
        return GetKnownFieldNames(null, category);
    }

    public static IReadOnlyList<string> GetKnownFieldNames(string? basePath, ItemExceptionFieldCategory category)
    {
        var guardData = GetGuardData(basePath);
        return guardData.CategoryFieldNames.TryGetValue(category, out var fieldNames) ? fieldNames : [];
    }

    public static ItemExceptionFieldCategory DetectCategory(string? sourceFile, JsonObject? fields = null)
    {
        var normalized = (sourceFile ?? string.Empty).Replace('\\', '/');
        if (normalized.Contains("/weapons/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("weapons/", StringComparison.OrdinalIgnoreCase))
        {
            return ItemExceptionFieldCategory.Weapon;
        }

        if (normalized.Contains("/attatchments/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("attatchments/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/attachments/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase))
        {
            return ItemExceptionFieldCategory.Attachment;
        }

        if (normalized.Contains("/gear/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("gear/", StringComparison.OrdinalIgnoreCase))
        {
            return ItemExceptionFieldCategory.Gear;
        }

        if (normalized.Contains("/ammo/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("ammo/", StringComparison.OrdinalIgnoreCase))
        {
            return ItemExceptionFieldCategory.Ammo;
        }

        if (fields is null)
        {
            return ItemExceptionFieldCategory.Unknown;
        }

        if (ContainsAny(fields, "InitialSpeed", "PenetrationPower", "BulletMassGram", "ArmorDamage", "HeavyBleedingDelta", "LightBleedingDelta"))
        {
            return ItemExceptionFieldCategory.Ammo;
        }

        if (ContainsAny(fields, "HasShoulderContact", "WeapType", "VerticalRecoil", "HorizontalRecoil", "Convergence", "VisualMulti", "BaseTorque", "ShotgunDispersion"))
        {
            return ItemExceptionFieldCategory.Weapon;
        }

        if (ContainsAny(fields, "SpallReduction", "Comfort", "speedPenaltyPercent", "GasProtection", "RadProtection", "weaponErgonomicPenalty", "dB"))
        {
            return ItemExceptionFieldCategory.Gear;
        }

        if (ContainsAny(fields, "Loudness", "Flash", "AimSpeed", "AimStability", "Handling", "ModMalfunctionChance", "ReloadSpeed", "LoadUnloadModifier"))
        {
            return ItemExceptionFieldCategory.Attachment;
        }

        return ItemExceptionFieldCategory.Unknown;
    }

    public static ItemExceptionFieldGuidance GetGuidance(string fieldName)
    {
        return GetGuidance(null, fieldName);
    }

    public static ItemExceptionFieldGuidance GetGuidance(string? basePath, string fieldName)
    {
        var trimmed = fieldName.Trim();
        var guardData = GetGuardData(basePath);
        if (guardData.KnownRanges.TryGetValue(trimmed, out var exact))
        {
            return new ItemExceptionFieldGuidance
            {
                FieldName = trimmed,
                Min = exact.Min,
                Max = exact.Max,
                PreferInt = exact.PreferInt,
                Reason = "rules",
            };
        }

        return trimmed switch
        {
            _ when trimmed.Contains("LoyaltyLevel", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, 1, 5, true, "heuristic"),
            _ when trimmed.Contains("Ergonomics", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, -50, 100, true, "heuristic"),
            _ when trimmed.Contains("Recoil", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, -2000, 2000, false, "heuristic"),
            _ when trimmed.Contains("Weight", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, 0, 50, false, "heuristic"),
            _ when trimmed.Contains("Multi", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Factor", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, 0.01, 10, false, "heuristic"),
            _ when trimmed.Contains("Speed", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Chance", StringComparison.OrdinalIgnoreCase) => CreateHeuristic(trimmed, -100, 100, false, "heuristic"),
            _ => new ItemExceptionFieldGuidance { FieldName = trimmed, Reason = "none" },
        };
    }

    public static ItemExceptionFieldNormalizationResult NormalizeValue(string fieldName, JsonNode value)
    {
        return NormalizeValue(null, fieldName, value);
    }

    public static ItemExceptionFieldNormalizationResult NormalizeValue(string? basePath, string fieldName, JsonNode value)
    {
        var guidance = GetGuidance(basePath, fieldName);
        if (guidance.Min is null || guidance.Max is null || !TryGetNumericValue(value, out var numericValue))
        {
            return new ItemExceptionFieldNormalizationResult { Value = value, WasAdjusted = false };
        }

        var clamped = Math.Clamp(numericValue, guidance.Min.Value, guidance.Max.Value);
        if (guidance.PreferInt)
        {
            clamped = Math.Round(clamped, MidpointRounding.AwayFromZero);
        }
        else
        {
            clamped = Math.Round(clamped, 4, MidpointRounding.AwayFromZero);
        }

        var adjusted = Math.Abs(clamped - numericValue) > 0.0000001d;
        return new ItemExceptionFieldNormalizationResult
        {
            Value = guidance.PreferInt ? JsonValue.Create((int)clamped)! : JsonValue.Create(clamped)!,
            WasAdjusted = adjusted,
            Message = adjusted ? guidance.FormatRange() : string.Empty,
        };
    }

    public static JsonNode GetSuggestedValue(string fieldName)
    {
        return GetSuggestedValue(null, fieldName);
    }

    public static JsonNode GetSuggestedValue(string? basePath, string fieldName)
    {
        var trimmed = fieldName.Trim();
        if (string.Equals(trimmed, "HasShoulderContact", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(false)!;
        }

        if (string.Equals(trimmed, "WeapType", StringComparison.OrdinalIgnoreCase))
        {
            return JsonValue.Create(string.Empty)!;
        }

        var guidance = GetGuidance(basePath, trimmed);
        if (guidance.Min is null || guidance.Max is null)
        {
            var guardData = GetGuardData(basePath);
            if (guardData.TemplateSuggestedValues.TryGetValue(trimmed, out var templateValue))
            {
                return templateValue.DeepClone()!;
            }

            return JsonValue.Create((string?)null)!;
        }

        var suggested = GetSuggestedNumericValue(trimmed, guidance);
        if (guidance.PreferInt)
        {
            return JsonValue.Create((int)Math.Round(suggested, MidpointRounding.AwayFromZero))!;
        }

        return JsonValue.Create(Math.Round(suggested, 4, MidpointRounding.AwayFromZero))!;
    }

    private static GuardData GetGuardData(string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return DefaultGuardData;
        }

        var normalizedBasePath = Path.GetFullPath(basePath);
        lock (GuardDataCacheLock)
        {
            if (GuardDataCache.TryGetValue(normalizedBasePath, out var cached))
            {
                return cached;
            }

            var rules = RuleSetLoader.Load(normalizedBasePath, _ => { });
            var guardData = BuildGuardData(rules, normalizedBasePath);
            GuardDataCache[normalizedBasePath] = guardData;
            return guardData;
        }
    }

    private static ItemExceptionFieldGuidance CreateHeuristic(string fieldName, double min, double max, bool preferInt, string reason)
    {
        return new ItemExceptionFieldGuidance
        {
            FieldName = fieldName,
            Min = min,
            Max = max,
            PreferInt = preferInt,
            Reason = reason,
        };
    }

    private static double GetSuggestedNumericValue(string fieldName, ItemExceptionFieldGuidance guidance)
    {
        var min = guidance.Min!.Value;
        var max = guidance.Max!.Value;

        if (fieldName.Contains("LoyaltyLevel", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Clamp(1, min, max);
        }

        if ((fieldName.Contains("Multi", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("Factor", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("Coef", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("Reduction", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("Protection", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("Comfort", StringComparison.OrdinalIgnoreCase))
            && min <= 1 && max >= 1)
        {
            return 1;
        }

        if (min <= 0 && max >= 0)
        {
            return 0;
        }

        return (min + max) / 2d;
    }

    private static GuardData BuildGuardData(RuleSet rules, string? basePath)
    {
        var knownRanges = BuildKnownRanges(rules);
        var categoryFieldNames = BuildCategoryFieldNames(rules);
        var templateSuggestedValues = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase);
        MergeTemplateFields(basePath, categoryFieldNames, templateSuggestedValues);

        var knownFieldNames = new HashSet<string>(knownRanges.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var categoryFields in categoryFieldNames.Values)
        {
            foreach (var fieldName in categoryFields)
            {
                knownFieldNames.Add(fieldName);
            }
        }

        return new GuardData(
            knownRanges,
            knownFieldNames.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray(),
            categoryFieldNames.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()),
            templateSuggestedValues);
    }

    private static IReadOnlyDictionary<string, NumericRange> BuildKnownRanges(RuleSet rules)
    {
        var ranges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);

        AddRangeMap(ranges, rules.Weapon.GunClampRules);
        AddNestedRangeMap(ranges, rules.Weapon.WeaponProfileRanges.Values);
        AddNestedRangeMap(ranges, rules.Weapon.WeaponCaliberRuleModifiers.Values);
        AddNestedRangeMap(ranges, rules.Weapon.WeaponStockRuleModifiers.Values);
        AddRangeMap(ranges, rules.Attachment.ModClampRules);
        AddNestedRangeMap(ranges, rules.Attachment.ModProfileRanges.Values);
        AddRangeMap(ranges, rules.Gear.GearClampRules);
        AddNestedRangeMap(ranges, rules.Gear.GearProfileRanges.Values);
        AddNestedRangeMap(ranges, rules.Ammo.AmmoProfileRanges.Values);
        AddNestedRangeMap(ranges, rules.Ammo.AmmoSpecialModifiers.Values);
        AddNestedRangeMap(ranges, rules.Ammo.AmmoPenetrationModifiers.Values);

        return ranges;
    }

    private static Dictionary<ItemExceptionFieldCategory, HashSet<string>> BuildCategoryFieldNames(RuleSet rules)
    {
        return new Dictionary<ItemExceptionFieldCategory, HashSet<string>>
        {
            [ItemExceptionFieldCategory.Weapon] = BuildFieldSet(
                rules.Weapon.GunClampRules.Keys,
                ["HasShoulderContact", "WeapType", "RecoilAngle", "CameraRecoil"],
                rules.Weapon.WeaponProfileRanges.Values,
                rules.Weapon.WeaponCaliberRuleModifiers.Values,
                rules.Weapon.WeaponStockRuleModifiers.Values),
            [ItemExceptionFieldCategory.Attachment] = BuildFieldSet(
                rules.Attachment.ModClampRules.Keys,
                ["CameraRecoil", "AimStability", "Flash", "HeatFactor", "CoolFactor", "AimSpeed", "Handling", "ReloadSpeed", "LoadUnloadModifier", "CheckTimeModifier", "ModMalfunctionChance", "DurabilityBurnModificator"],
                rules.Attachment.ModProfileRanges.Values),
            [ItemExceptionFieldCategory.Gear] = BuildFieldSet(
                rules.Gear.GearClampRules.Keys,
                ["SpallReduction", "ReloadSpeedMulti", "Comfort", "speedPenaltyPercent", "weaponErgonomicPenalty", "GasProtection", "RadProtection", "dB"],
                rules.Gear.GearProfileRanges.Values),
            [ItemExceptionFieldCategory.Ammo] = BuildFieldSet(
                [],
                ["InitialSpeed", "BulletMassGram", "Damage", "PenetrationPower", "ammoRec", "ammoAccr", "ArmorDamage", "HeatFactor", "HeavyBleedingDelta", "LightBleedingDelta", "DurabilityBurnModificator", "BallisticCoeficient", "MalfMisfireChance", "MisfireChance", "MalfFeedChance"],
                rules.Ammo.AmmoProfileRanges.Values,
                rules.Ammo.AmmoSpecialModifiers.Values,
                rules.Ammo.AmmoPenetrationModifiers.Values),
        };
    }

    private static HashSet<string> BuildFieldSet(
        IEnumerable<string> baseFields,
        IEnumerable<string> extraFields,
        params IEnumerable<IReadOnlyDictionary<string, NumericRange>>[] rangeSourceGroups)
    {
        var names = new HashSet<string>(baseFields, StringComparer.OrdinalIgnoreCase);
        foreach (var rangeSources in rangeSourceGroups)
        {
            foreach (var source in rangeSources)
            {
                foreach (var fieldName in source.Keys)
                {
                    names.Add(fieldName);
                }
            }
        }

        foreach (var fieldName in extraFields)
        {
            names.Add(fieldName);
        }

        return names;
    }

    private static void MergeTemplateFields(
        string? basePath,
        IDictionary<ItemExceptionFieldCategory, HashSet<string>> categoryFieldNames,
        IDictionary<string, JsonNode> templateSuggestedValues)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return;
        }

        var templateRoot = Path.Combine(basePath, "现实主义物品模板");
        if (!Directory.Exists(templateRoot))
        {
            return;
        }

        foreach (var (category, filePath) in EnumerateTemplateFiles(templateRoot))
        {
            try
            {
                var root = JsonNode.Parse(File.ReadAllText(filePath))?.AsObject();
                if (root is null)
                {
                    continue;
                }

                foreach (var item in root)
                {
                    if (item.Value is not JsonObject itemObject)
                    {
                        continue;
                    }

                    foreach (var pair in itemObject)
                    {
                        if (pair.Value is null)
                        {
                            continue;
                        }

                        categoryFieldNames[category].Add(pair.Key);
                        templateSuggestedValues.TryAdd(pair.Key, pair.Value.DeepClone()!);
                    }
                }
            }
            catch
            {
                // Ignore malformed template files and keep current known field set.
            }
        }
    }

    private static IEnumerable<(ItemExceptionFieldCategory Category, string FilePath)> EnumerateTemplateFiles(string templateRoot)
    {
        var mappings = new[]
        {
            (ItemExceptionFieldCategory.Weapon, Path.Combine(templateRoot, "weapons")),
            (ItemExceptionFieldCategory.Attachment, Path.Combine(templateRoot, "attatchments")),
            (ItemExceptionFieldCategory.Gear, Path.Combine(templateRoot, "gear")),
            (ItemExceptionFieldCategory.Ammo, Path.Combine(templateRoot, "ammo")),
        };

        foreach (var (category, directory) in mappings)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                yield return (category, filePath);
            }
        }

        var topLevelAmmoTemplate = Path.Combine(templateRoot, "Ammo_templates.json");
        if (File.Exists(topLevelAmmoTemplate))
        {
            yield return (ItemExceptionFieldCategory.Ammo, topLevelAmmoTemplate);
        }
    }

    private static void AddRangeMap(IDictionary<string, NumericRange> target, IReadOnlyDictionary<string, NumericRange> source)
    {
        foreach (var pair in source)
        {
            AddRange(target, pair.Key, pair.Value);
        }
    }

    private static void AddNestedRangeMap(IDictionary<string, NumericRange> target, IEnumerable<IReadOnlyDictionary<string, NumericRange>> sources)
    {
        foreach (var source in sources)
        {
            AddRangeMap(target, source);
        }
    }

    private static void AddRange(IDictionary<string, NumericRange> target, string fieldName, NumericRange range)
    {
        if (target.TryGetValue(fieldName, out var existing))
        {
            target[fieldName] = new NumericRange(
                Math.Min(existing.Min, range.Min),
                Math.Max(existing.Max, range.Max),
                existing.PreferInt || range.PreferInt);
            return;
        }

        target[fieldName] = range;
    }

    private static bool ContainsAny(JsonObject fields, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (fields.ContainsKey(fieldName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNumericValue(JsonNode? value, out double numericValue)
    {
        numericValue = 0;
        if (value is not JsonValue jsonValue)
        {
            return false;
        }

        if (jsonValue.TryGetValue<int>(out var intValue))
        {
            numericValue = intValue;
            return true;
        }

        if (jsonValue.TryGetValue<long>(out var longValue))
        {
            numericValue = longValue;
            return true;
        }

        if (jsonValue.TryGetValue<float>(out var floatValue))
        {
            numericValue = floatValue;
            return true;
        }

        if (jsonValue.TryGetValue<double>(out var doubleValue))
        {
            numericValue = doubleValue;
            return true;
        }

        if (jsonValue.TryGetValue<decimal>(out var decimalValue))
        {
            numericValue = (double)decimalValue;
            return true;
        }

        return false;
    }

    private sealed record GuardData(
        IReadOnlyDictionary<string, NumericRange> KnownRanges,
        IReadOnlyList<string> KnownFieldNames,
        IReadOnlyDictionary<ItemExceptionFieldCategory, IReadOnlyList<string>> CategoryFieldNames,
        IReadOnlyDictionary<string, JsonNode> TemplateSuggestedValues);
}