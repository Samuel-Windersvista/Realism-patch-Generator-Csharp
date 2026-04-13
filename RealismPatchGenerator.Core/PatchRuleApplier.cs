using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class PatchRuleApplier
{
    public static void ApplyRealismSanityCheck(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        EnsureRequiredFields(patch, itemInfo);
        ApplyPreRuleHeuristics(patch);
        var ruleContext = new PatchRuleContext(generator, rules, patch, itemInfo);

        var itemType = patch["$type"]?.GetValue<string?>() ?? string.Empty;
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            WeaponRuleEngine.ApplyWeaponSanityCheck(ruleContext);
            return;
        }

        if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AttachmentRuleEngine.ApplyAttachmentSanityCheck(ruleContext);
            return;
        }

        if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            GearRuleEngine.ApplyGearSanityCheck(ruleContext);
            return;
        }

        if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AmmoRuleEngine.ApplyAmmoProfileRanges(ruleContext);
            RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
        }
    }

    private static void EnsureRequiredFields(JsonObject patch, ItemInfo itemInfo)
    {
        var itemType = patch["$type"]?.GetValue<string?>() ?? string.Empty;
        var itemId = patch["ItemID"]?.GetValue<string?>() ?? itemInfo.ItemId ?? "unknown";
        var itemName = itemInfo.Name ?? string.Empty;

        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            patch["$type"] ??= "RealismMod.Gun, RealismMod";
            if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["Name"])))
            {
                patch["Name"] = !string.IsNullOrWhiteSpace(itemName) ? itemName : $"weapon_{itemId}";
            }

            patch["Weight"] ??= 1.5;
            patch["LoyaltyLevel"] ??= 1;
        }
        else if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            patch["$type"] ??= "RealismMod.WeaponMod, RealismMod";
            if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["Name"])))
            {
                patch["Name"] = !string.IsNullOrWhiteSpace(itemName) ? itemName : $"mod_{itemId}";
            }

            patch["Weight"] ??= 0.1;
            patch["LoyaltyLevel"] ??= 1;
            patch["ModType"] ??= string.Empty;
        }
        else if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            patch["$type"] ??= "RealismMod.Ammo, RealismMod";
            if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["Name"])))
            {
                patch["Name"] = !string.IsNullOrWhiteSpace(itemName) ? itemName : $"ammo_{itemId}";
            }

            patch["LoyaltyLevel"] ??= 1;
            patch["BasePriceModifier"] ??= 1;
        }
        else if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            patch["$type"] ??= "RealismMod.Gear, RealismMod";
            if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["Name"])))
            {
                patch["Name"] = !string.IsNullOrWhiteSpace(itemName) ? itemName : $"gear_{itemId}";
            }

            patch["LoyaltyLevel"] ??= 1;
        }
    }

    private static void ApplyPreRuleHeuristics(JsonObject patch)
    {
        var itemName = RealismPatchGenerator.GetLowerText(patch["Name"]);
        ApplyMaterialHeuristics(patch, itemName);
        ApplySizeHeuristics(patch, itemName);
        ApplyBarrelVelocityHeuristic(patch, itemName);
    }

    private static void ApplyMaterialHeuristics(JsonObject patch, string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["titanium", "ti-", "carbon"]))
        {
            TransformNumericField(patch, "Weight", value => Math.Round(value * 0.8, 3), false);
            TransformNumericField(patch, "CoolFactor", value => Math.Round(value * 1.15, 2), false);
            TransformNumericField(patch, "Ergonomics", value => Math.Round(value * 1.05, 1), false);
            return;
        }

        if (itemName.Contains("steel", StringComparison.OrdinalIgnoreCase))
        {
            TransformNumericField(patch, "Weight", value => Math.Round(value * 1.25, 3), false);
            TransformNumericField(patch, "DurabilityBurnModificator", value => Math.Round(value * 0.9, 2), false);
        }
    }

    private static void ApplySizeHeuristics(JsonObject patch, string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["compact", "mini", "short", "k-", "kurz"]))
        {
            TransformNumericField(patch, "Weight", value => Math.Round(value * 0.75, 3), false);
            TransformNumericField(patch, "Loudness", value => value < 0 ? Math.Round(value * 0.7, 1) : value, false);
            TransformNumericField(patch, "VerticalRecoil", value => value < 0 ? Math.Round(value * 0.7, 2) : value, false);
            return;
        }

        if (ContainsAnyKeyword(itemName, ["long", "extended", "heavy", "full"]))
        {
            TransformNumericField(patch, "Weight", value => Math.Round(value * 1.3, 3), false);
            TransformNumericField(patch, "Accuracy", value => Math.Round(value * 1.1 + 1, 1), false);
        }
    }

    private static void ApplyBarrelVelocityHeuristic(JsonObject patch, string itemName)
    {
        var barrelLengthMm = RealismPatchGenerator.ExtractBarrelLengthMm(itemName);
        if (barrelLengthMm is null || !itemName.Contains("barrel", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var inferredVelocity = (barrelLengthMm.Value - 370) / 25.4 * 1.5;
        if (patch["Velocity"] is null || (RealismPatchGenerator.TryGetNumericValue(patch["Velocity"], out var currentVelocity) && currentVelocity == 0))
        {
            patch["Velocity"] = Math.Round(RealismPatchGenerator.Clamp(inferredVelocity, -18, 18), 2);
        }
    }

    private static void TransformNumericField(JsonObject patch, string key, Func<double, double> transform, bool? preferInt = null)
    {
        if (patch[key] is null || !RealismPatchGenerator.TryGetNumericValue(patch[key], out var value))
        {
            return;
        }

        patch[key] = RealismPatchGenerator.CreateNumericNode(transform(value), preferInt ?? RealismPatchGenerator.IsIntegerNode(patch[key]));
    }

    private static bool ContainsAnyKeyword(string text, IEnumerable<string> keywords)
    {
        foreach (var keyword in keywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}