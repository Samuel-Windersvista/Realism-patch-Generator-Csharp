using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RealismPatchGenerator.Core;

internal static class PatchTextInferenceHelpers
{
    private static readonly Regex AlphaNumericTokenRegex = new("[a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MagCapacityNameRegex = new(@"\b(\d{1,3})(?:\s*|-)?(?:round|rnd|rds)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MagCapacityCnRegex = new(@"(?<!\d)(\d{1,3})\s*发", RegexOptions.Compiled);
    private static readonly Regex MagCapacityRuRegex = new(@"(?<![\d.])(\d{1,3})\s*патрон", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BarrelLengthRegex = new(@"(\d+(?:\.\d+)?)\s*(mm|inch|in|"")", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // === Name / text helpers ===

    internal static string? ExtractLocalizedName(JsonNode? localeNode)
    {
        if (localeNode is not JsonObject localeObject)
        {
            return null;
        }

        foreach (var lang in new[] { "en", "ch", "zh", "ru" })
        {
            if (localeObject[lang] is JsonObject localized)
            {
                var name = localized["name"]?.GetValue<string?>() ?? localized["Name"]?.GetValue<string?>();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }

        var direct = localeObject["name"]?.GetValue<string?>() ?? localeObject["Name"]?.GetValue<string?>();
        return string.IsNullOrWhiteSpace(direct) ? null : direct;
    }

    internal static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    internal static string? SelectBestDisplayName(string? localizedName, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !LooksLikeInternalItemName(value))
            {
                return value;
            }
        }

        if (!string.IsNullOrWhiteSpace(localizedName))
        {
            return localizedName;
        }

        return FirstNonEmpty(values);
    }

    private static bool LooksLikeInternalItemName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 6)
        {
            return false;
        }

        if (trimmed.StartsWith("mod_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ammo_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("stock_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("pistolgrip_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("reciever_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("receiver_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("sight_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("mag_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.Length == 24 && trimmed.All(Uri.IsHexDigit))
        {
            return true;
        }

        var hasSeparator = trimmed.Contains('_') || trimmed.Contains('/');
        var allAsciiTokenChars = trimmed.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.' or '"');
        var hasLowercase = trimmed.Any(char.IsLower);
        var hasWhitespace = trimmed.Any(char.IsWhiteSpace);

        return hasSeparator && allAsciiTokenChars && hasLowercase && !hasWhitespace;
    }

    internal static HashSet<string> ExtractAlphaNumericTokens(string value)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AlphaNumericTokenRegex.Matches(value))
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                tokens.Add(match.Value);
            }
        }

        return tokens;
    }

    internal static bool ContainsAnyKeyword(string text, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    // === Name-driven profile inference helpers ===

    internal static string InferMagazineProfile(int? capacity, string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["drum", "casket", "quad", "coupled", "twin", "beta", "helical", "snail"]))
        {
            return "magazine_drum";
        }

        if (ContainsAnyKeyword(itemName, ["extended", "extend", "加长", "扩容"]))
        {
            return "magazine_extended";
        }

        if (ContainsAnyKeyword(itemName, ["compact", "short", "stubby", "短弹匣", "短匣"]))
        {
            return "magazine_compact";
        }

        if (capacity is null)
        {
            return "magazine_standard";
        }

        if (capacity <= 20)
        {
            return "magazine_compact";
        }

        if (capacity <= 40)
        {
            return "magazine_standard";
        }

        if (capacity <= 60)
        {
            return "magazine_extended";
        }

        return "magazine_drum";
    }

    internal static int? ExtractMagCapacity(ItemInfo itemInfo, string itemName)
    {
        foreach (var key in new[] { "Capacity", "capacity", "MaxCount", "max_count", "CartridgeMaxCount", "cartridgeMaxCount" })
        {
            if (TryReadCapacityValue(itemInfo.Properties[key], out var capacity))
            {
                return capacity;
            }
        }

        if (itemInfo.Properties["Cartridges"] is JsonArray cartridges)
        {
            foreach (var slot in cartridges.OfType<JsonObject>())
            {
                if (TryReadCapacityValue(slot["_max_count"], out var capacity))
                {
                    return capacity;
                }
            }
        }

        foreach (var regex in new[] { MagCapacityNameRegex, MagCapacityCnRegex, MagCapacityRuRegex })
        {
            var match = regex.Match(itemName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed is >= 1 and <= 200)
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryReadCapacityValue(JsonNode? node, out int capacity)
    {
        capacity = 0;
        if (node is null)
        {
            return false;
        }

        if (RealismPatchGenerator.TryGetNumericValue(node, out var numericValue))
        {
            var parsed = (int)Math.Round(numericValue);
            if (parsed is >= 1 and <= 200)
            {
                capacity = parsed;
                return true;
            }
        }

        var textValue = RealismPatchGenerator.GetText(node);
        if (int.TryParse(textValue, out var parsedInt) && parsedInt is >= 1 and <= 200)
        {
            capacity = parsedInt;
            return true;
        }

        return false;
    }

    internal static string InferBarrelProfileFromName(string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["integral barrel-suppressor", "integral suppressor", "integrally suppressed", "barrel-suppressor", "一体消音枪管", "整体消音枪管"]))
        {
            return "barrel_integral_suppressed";
        }

        var barrelLength = ExtractBarrelLengthMm(itemName);
        if (ContainsAnyKeyword(itemName, ["shortened", "short", "sbr", "kurz"]))
        {
            return "barrel_short";
        }

        if (barrelLength is not null && barrelLength <= 330 && ContainsAnyKeyword(itemName, ["carbine", "smg", "pdw", "shotgun", "12ga", "762x51", "556x45", "545x39", "762x39"]))
        {
            return "barrel_short";
        }

        if (ContainsAnyKeyword(itemName, ["extended", "long", "rifle length", "full length"]))
        {
            return "barrel_long";
        }

        return "barrel_medium";
    }

    internal static double? ExtractBarrelLengthMm(string itemName)
    {
        var matches = BarrelLengthRegex.Matches(itemName);
        if (matches.Count == 0)
        {
            return null;
        }

        var match = matches[^1];
        if (!double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups[2].Value.ToLowerInvariant();
        return unit is "inch" or "in" or "\"" ? value * 25.4 : value;
    }

    internal static bool IsHandguardLikeName(string itemName)
    {
        return itemName.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase)
            || ContainsAnyKeyword(itemName, ["护木", "forend", "handguard", "front-end assembly", "front end assembly", "цевье"]);
    }

    internal static string InferHandguardProfileFromName(string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["short", "carbine", "pdw", "compact"]))
        {
            return "handguard_short";
        }

        if (ContainsAnyKeyword(itemName, ["long", "extended", "rifle length", "full length"]))
        {
            return "handguard_long";
        }

        return "handguard_medium";
    }

    internal static string InferSuppressorProfileFromName(string itemName)
    {
        return ContainsAnyKeyword(itemName, ["mini", "mini2", "compact", "short", "45s", "rbs", "k-can", "mini monster"])
            ? "muzzle_suppressor_compact"
            : "muzzle_suppressor";
    }

    internal static string? InferSightProfileFromName(string itemName)
    {
        var normalized = itemName.Replace(',', '.');
        if (ContainsAnyKeyword(normalized, ["sight_front", "sight_rear", "front sight", "rear sight", "sight post", "front post", "iron", "mbus", "flip", "backup", "drum rear sight", "tritium rear sight", "tritium front sight"]))
        {
            return "iron_sight";
        }

        if (ContainsAnyKeyword(normalized, ["red dot", "reddot", "reflex", "holo", "holographic", "rds", "eotech", "xps", "exps", "aimpoint", "micro", "t1", "t2", "pk06", "okp", "kobra", "romeo", "holosun", "delta point", "deltapoint", "rmr", "srs", "uh-1", "1p87", "comp_m4", "comp m4", "compm4", "aimpooint", "boss_xe", "boss xe"]))
        {
            return "scope_red_dot";
        }

        if (Regex.IsMatch(normalized, @"(?:^|[^0-9])1(?:[.]0+)?x(?:[^0-9]|$)", RegexOptions.IgnoreCase))
        {
            return "scope_red_dot";
        }

        if (ContainsAnyKeyword(normalized, ["acog", "prism", "specter", "hamr", "valday", "lpvo", "vudu", "razor", "march", "bravo4", "ta01", "ta11", "ps320", "hensoldt"]))
        {
            return "scope_magnified";
        }

        if (Regex.IsMatch(normalized, @"(?:^|[^0-9])(2|3|4|5|6|7|8|9|10|11|12)(?:[.]\d+)?x(?:[^0-9]|$)", RegexOptions.IgnoreCase)
            || Regex.IsMatch(normalized, @"(?:^|[^0-9])1(?:[.]\d+)?[-/](2|3|4|5|6|7|8|9|10|11|12)(?:[.]\d+)?(?:x)?(?:[^0-9]|$)", RegexOptions.IgnoreCase))
        {
            return "scope_magnified";
        }

        return null;
    }

    internal static string InferModStockProfile(string itemName, JsonObject patch, ItemInfo itemInfo)
    {
        if (ContainsAnyKeyword(itemName, ["buttpad", "recoil pad", "butt pad", "shoulder pad", "托腮", "后托垫", "枪托垫", "缓冲垫"]))
        {
            return "stock_buttpad";
        }

        var stockAllowAds = RealismPatchGenerator.ToOptionalBool(itemInfo.Properties["StockAllowADS"]) ?? RealismPatchGenerator.ToOptionalBool(patch["StockAllowADS"]);
        var hasShoulder = RealismPatchGenerator.ToOptionalBool(itemInfo.Properties["HasShoulderContact"]) ?? RealismPatchGenerator.ToOptionalBool(patch["HasShoulderContact"]);
        if (stockAllowAds == true)
        {
            return "stock_ads_support";
        }

        if (hasShoulder == false || ContainsAnyKeyword(itemName, ["fold", "folding", "collapsed", "retracted", "telescop", "wire", "pdw", "skeleton", "折叠", "伸缩", "收缩", "骨架", "折叠托", "伸缩托", "枪托"]))
        {
            return "stock_folding";
        }

        return "stock_fixed";
    }

    // === Pure extraction helpers for ItemInfoFactory ===

    internal static JsonObject ExtractProperties(JsonObject source, HashSet<string> ignoredKeys)
    {
        var result = new JsonObject();
        foreach (var pair in source)
        {
            if (!ignoredKeys.Contains(pair.Key) && pair.Value is not null)
            {
                result[pair.Key] = pair.Value.DeepClone();
            }
        }

        return result;
    }

    internal static JsonObject ExtractEffectiveInputFields(JsonObject itemData, JsonObject? cloneTemplate)
    {
        JsonObject fields;
        if (itemData["items"] is JsonObject itemsObject && itemsObject["_props"] is JsonObject multiProps)
        {
            fields = (JsonObject)multiProps.DeepClone();
        }
        else if (itemData["item"] is JsonObject itemObject && itemObject["_props"] is JsonObject singleProps)
        {
            fields = (JsonObject)singleProps.DeepClone();
        }
        else if (TryGetRaidOverhaulOverrideProperties(itemData, out var overrideProperties))
        {
            fields = (JsonObject)overrideProperties.DeepClone();
        }
        else
        {
            fields = new JsonObject();
            foreach (var pair in itemData)
            {
                if (!ItemJsonSchema.RealismStandardTemplateIgnoredKeys.Contains(pair.Key) && pair.Value is not null)
                {
                    fields[pair.Key] = pair.Value.DeepClone();
                }
            }
        }

        if (cloneTemplate is null)
        {
            return fields;
        }

        var allowedFields = GetAllowedLegacySourceFields(cloneTemplate);
        var filteredFields = new JsonObject();
        foreach (var pair in fields)
        {
            if ((cloneTemplate.ContainsKey(pair.Key)
                    || allowedFields.Contains(pair.Key)
                    || ShouldPreserveLegacyFilteredField(pair.Key))
                && pair.Value is not null)
            {
                filteredFields[pair.Key] = pair.Value.DeepClone();
            }
        }

        return filteredFields;
    }

    private static bool TryGetRaidOverhaulOverrideProperties(JsonObject itemData, out JsonObject overrideProperties)
    {
        if (itemData["OverrideProperties"] is JsonObject uppercaseOverrideProperties)
        {
            overrideProperties = uppercaseOverrideProperties;
            return true;
        }

        if (itemData["overrideProperties"] is JsonObject lowercaseOverrideProperties)
        {
            overrideProperties = lowercaseOverrideProperties;
            return true;
        }

        overrideProperties = new JsonObject();
        return false;
    }

    private static HashSet<string> GetAllowedLegacySourceFields(JsonObject cloneTemplate)
    {
        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAllowedLegacySourceFields(allowedFields, cloneTemplate);

        var itemType = cloneTemplate["$type"]?.GetValue<string?>() ?? string.Empty;
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultWeaponTemplate());
            return allowedFields;
        }

        if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultModTemplate());

            var modType = cloneTemplate["ModType"]?.GetValue<string?>();
            if (!string.IsNullOrWhiteSpace(modType)
                && StaticData.ModTypeSpecificAttributes.TryGetValue(modType, out var modTypeAttributes))
            {
                AddAllowedLegacySourceFields(allowedFields, modTypeAttributes);
            }

            return allowedFields;
        }

        if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultAmmoTemplate());
            return allowedFields;
        }

        if (itemType.Contains("RealismMod.Consumable", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultConsumableTemplate());
        }

        return allowedFields;
    }

    private static void AddAllowedLegacySourceFields(HashSet<string> allowedFields, JsonObject fieldSource)
    {
        foreach (var pair in fieldSource)
        {
            allowedFields.Add(pair.Key);
        }
    }

    private static bool ShouldPreserveLegacyFilteredField(string fieldName)
    {
        return string.Equals(fieldName, "ConflictingItems", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fieldName, "SingleFireRate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fieldName, "ChamberSpeed", StringComparison.OrdinalIgnoreCase);
    }

    internal static JsonObject? GetLegacyItemNode(JsonObject itemData)
    {
        return itemData["item"] as JsonObject ?? itemData["items"] as JsonObject;
    }

    internal static string? ResolveEffectiveModType(JsonObject properties, JsonObject referenceData, string? templateFile)
    {
        var sourceModType = properties["ModType"]?.GetValue<string?>();
        if (!string.IsNullOrWhiteSpace(sourceModType))
        {
            return sourceModType;
        }

        var referenceModType = referenceData["ModType"]?.GetValue<string?>();
        if (!string.IsNullOrWhiteSpace(referenceModType))
        {
            return referenceModType;
        }

        var templateName = Path.GetFileName(templateFile ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(templateName)
            && StaticData.TemplateFileToModType.TryGetValue(templateName, out var templateModType)
            && !string.IsNullOrWhiteSpace(templateModType))
        {
            return templateModType;
        }

        if (properties["Cartridges"] is not null
            || referenceData["Cartridges"] is not null
            || ((properties["LoadUnloadModifier"] is not null || referenceData["LoadUnloadModifier"] is not null)
                && (properties["CheckTimeModifier"] is not null || referenceData["CheckTimeModifier"] is not null)))
        {
            return "magazine";
        }

        return null;
    }
}
