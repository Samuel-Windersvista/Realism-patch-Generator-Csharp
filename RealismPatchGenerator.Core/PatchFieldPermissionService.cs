using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed class PatchFieldPermissionService
{
    private readonly RuleSet rules;
    private readonly TemplateRepository templateRepository;

    public PatchFieldPermissionService(RuleSet rules, TemplateRepository templateRepository)
    {
        this.rules = rules;
        this.templateRepository = templateRepository;
    }

    public HashSet<string> CreateAllowedPatchFieldSet(JsonObject templateSource, string? itemType, string? modType)
    {
        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in templateSource)
        {
            allowedFields.Add(pair.Key);
        }

        AddRuleAllowedFieldsToSet(allowedFields, itemType, modType);
        AddRequiredAllowedFieldsToSet(allowedFields, itemType ?? string.Empty);
        return allowedFields;
    }

    public Dictionary<string, string> GetAllowedOutputFieldMap(ItemInfo itemInfo, JsonObject patch)
    {
        var allowedFields = CreateAllowedFieldMap();

        foreach (var fieldName in itemInfo.AllowedPatchFields)
        {
            TryAddCanonicalField(allowedFields, fieldName);
        }

        if (!string.IsNullOrWhiteSpace(itemInfo.TemplateFile))
        {
            var templateFileName = Path.GetFileName(itemInfo.TemplateFile);
            if (!templateRepository.TryGetAllowedFieldMap(templateFileName, out var cachedTemplateFields))
            {
                if (templateRepository.TryGetTemplateData(itemInfo.TemplateFile, out var templateData))
                {
                    cachedTemplateFields = CreateAllowedFieldMap();
                    foreach (var template in templateData.Values)
                    {
                        foreach (var pair in template)
                        {
                            TryAddCanonicalField(cachedTemplateFields, pair.Key);
                        }
                    }

                    templateRepository.StoreAllowedFieldMap(templateFileName, cachedTemplateFields);
                }
                else
                {
                    cachedTemplateFields = CreateAllowedFieldMap();
                }
            }

            foreach (var pair in cachedTemplateFields)
            {
                allowedFields[pair.Key] = pair.Value;
            }
        }

        var itemType = patch["$type"]?.GetValue<string?>() ?? itemInfo.ItemType ?? string.Empty;
        var modType = patch["ModType"]?.GetValue<string?>() ?? itemInfo.SourceProperties["ModType"]?.GetValue<string?>();
        AddRuleAllowedFields(allowedFields, itemType, modType);
        AddRequiredAllowedFields(allowedFields, itemType);
        return allowedFields;
    }

    public void PruneDisallowedOutputFields(JsonObject patch, ItemInfo itemInfo)
    {
        var allowedFields = GetAllowedOutputFieldMap(itemInfo, patch);
        var keys = patch.Select(pair => pair.Key).ToArray();
        foreach (var key in keys)
        {
            if (!allowedFields.TryGetValue(key, out var canonicalKey))
            {
                patch.Remove(key);
                continue;
            }

            if (string.Equals(key, canonicalKey, StringComparison.Ordinal))
            {
                continue;
            }

            if (patch[canonicalKey] is null && patch[key] is not null)
            {
                patch[canonicalKey] = patch[key]!.DeepClone();
            }

            patch.Remove(key);
        }
    }

    private void AddRuleAllowedFieldsToSet(HashSet<string> allowedFields, string? itemType, string? modType)
    {
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddRuleAllowedFields(fieldMap, itemType, modType);
        foreach (var fieldName in fieldMap.Values)
        {
            allowedFields.Add(fieldName);
        }
    }

    private void AddRuleAllowedFields(IDictionary<string, string> allowedFields, string? itemType, string? modType)
    {
        var resolvedItemType = itemType ?? string.Empty;
        if (resolvedItemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Weapon.GunClampRules.Keys);
            AddFieldNames(allowedFields, ["HasShoulderContact", "WeapType", "RecoilAngle", "CameraRecoil", "Price", "SingleFireRate"]);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponProfileRanges.Values);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponCaliberRuleModifiers.Values);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponStockRuleModifiers.Values);
            return;
        }

        if (resolvedItemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Attachment.ModClampRules.Keys);
            AddFieldNames(allowedFields, ["CameraRecoil", "AimStability", "Flash", "HeatFactor", "CoolFactor", "AimSpeed", "Handling", "ReloadSpeed", "LoadUnloadModifier", "CheckTimeModifier", "ModMalfunctionChance", "DurabilityBurnModificator", "Price"]);
            AddRangeFieldNames(allowedFields, rules.Attachment.ModProfileRanges.Values);

            if (!string.IsNullOrWhiteSpace(modType)
                && StaticData.ModTypeSpecificAttributes.TryGetValue(modType, out var modTypeAttributes))
            {
                AddFieldNames(allowedFields, modTypeAttributes.Select(pair => pair.Key));
            }

            return;
        }

        if (resolvedItemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Gear.GearClampRules.Keys);
            AddFieldNames(allowedFields, ["SpallReduction", "ReloadSpeedMulti", "Comfort", "speedPenaltyPercent", "weaponErgonomicPenalty", "GasProtection", "RadProtection", "dB", "Price"]);
            AddRangeFieldNames(allowedFields, rules.Gear.GearProfileRanges.Values);
            return;
        }

        if (resolvedItemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["InitialSpeed", "BulletMassGram", "Damage", "PenetrationPower", "ammoRec", "ammoAccr", "ArmorDamage", "HeatFactor", "HeavyBleedingDelta", "LightBleedingDelta", "DurabilityBurnModificator", "BallisticCoeficient", "MalfMisfireChance", "MisfireChance", "MalfFeedChance"]);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoProfileRanges.Values);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoSpecialModifiers.Values);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoPenetrationModifiers.Values);
        }
    }

    private static void AddRequiredAllowedFields(IDictionary<string, string> allowedFields, string itemType)
    {
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["Weight", "LoyaltyLevel", "Price"]);
            return;
        }

        if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["Weight", "LoyaltyLevel", "ModType", "Price"]);
            return;
        }

        if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["LoyaltyLevel", "BasePriceModifier"]);
            return;
        }

        if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["LoyaltyLevel", "Price"]);
        }
    }

    private static void AddRequiredAllowedFieldsToSet(HashSet<string> allowedFields, string itemType)
    {
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddRequiredAllowedFields(fieldMap, itemType);
        foreach (var fieldName in fieldMap.Values)
        {
            allowedFields.Add(fieldName);
        }
    }

    private static void AddRangeFieldNames(IDictionary<string, string> allowedFields, IEnumerable<IReadOnlyDictionary<string, NumericRange>> rangeMaps)
    {
        foreach (var rangeMap in rangeMaps)
        {
            AddFieldNames(allowedFields, rangeMap.Keys);
        }
    }

    private static void AddFieldNames(IDictionary<string, string> allowedFields, IEnumerable<string> fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            TryAddCanonicalField(allowedFields, fieldName);
        }
    }

    private static Dictionary<string, string> CreateAllowedFieldMap()
    {
        var allowedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 最终输出字段边界只允许 Realism 标准补丁字段；源 mod 输入字段可参与识别和推断，但不能直接泄漏到输出。
        TryAddCanonicalField(allowedFields, "$type");
        TryAddCanonicalField(allowedFields, "ItemID");
        TryAddCanonicalField(allowedFields, "Name");
        TryAddCanonicalField(allowedFields, "ConflictingItems");
        return allowedFields;
    }

    private static void TryAddCanonicalField(IDictionary<string, string> allowedFields, string fieldName)
    {
        if (!allowedFields.ContainsKey(fieldName))
        {
            allowedFields[fieldName] = fieldName;
        }
    }
}
