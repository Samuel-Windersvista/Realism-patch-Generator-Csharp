using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class AmmoRuleEngine
{
    public static void ApplyAmmoProfileRanges(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        ApplyAmmoProfileRanges(new PatchRuleContext(generator, rules, patch, itemInfo));
    }

    public static void ApplyAmmoProfileRanges(PatchRuleContext context)
    {
        var generator = context.Generator;
        var rules = context.Rules;
        var patch = context.Patch;

        var ammoProfile = context.GetAmmoProfile();
        if (!rules.Ammo.AmmoProfileRanges.TryGetValue(ammoProfile, out var baseRanges))
        {
            return;
        }

        var penetrationTier = context.GetAmmoPenetrationTier();
        var specialProfile = context.GetAmmoSpecialProfile();
        var penetrationMods = rules.Ammo.AmmoPenetrationModifiers.GetValueOrDefault(penetrationTier);
        var specialMods = !string.IsNullOrWhiteSpace(specialProfile) ? rules.Ammo.AmmoSpecialModifiers.GetValueOrDefault(specialProfile) : null;
        var malfunctionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MalfMisfireChance", "MisfireChance", "MalfFeedChance" };

        foreach (var pair in baseRanges)
        {
            var min = pair.Value.Min;
            var max = pair.Value.Max;
            if (penetrationMods is not null && penetrationMods.TryGetValue(pair.Key, out var tierRange))
            {
                min += tierRange.Min;
                max += tierRange.Max;
            }

            if (specialMods is not null && specialMods.TryGetValue(pair.Key, out var specialRange))
            {
                min += specialRange.Min;
                max += specialRange.Max;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            if (malfunctionKeys.Contains(pair.Key))
            {
                min = RealismPatchGenerator.Clamp(min, 0.001, 0.015);
                max = RealismPatchGenerator.Clamp(max, 0.001, 0.015);
                if (min > max)
                {
                    (min, max) = (max, min);
                }
            }

            if (string.Equals(pair.Key, "ArmorDamage", StringComparison.OrdinalIgnoreCase))
            {
                min = RealismPatchGenerator.Clamp(min, 1.0, 1.2);
                max = RealismPatchGenerator.Clamp(max, 1.0, 1.2);
                if (min > max)
                {
                    (min, max) = (max, min);
                }
            }

            if (patch[pair.Key] is null)
            {
                patch[pair.Key] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.GetRangeSeedValue(min, max, pair.Value.PreferInt), pair.Value.PreferInt);
            }

            patch[pair.Key] = generator.SampleRangeValue(patch[pair.Key], min, max, pair.Value.PreferInt);
        }
    }

    public static string InferAmmoPenetrationTier(PatchRuleContext context)
        => context.GetAmmoPenetrationTier();

    public static string InferAmmoPenetrationTier(RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        var penetration = ExtractPenetrationValue(patch, itemInfo);
        if (penetration is null)
        {
            return "pen_lvl_5";
        }

        foreach (var pair in rules.Ammo.AmmoPenetrationTiers)
        {
            if (penetration >= pair.Value.Min && penetration <= pair.Value.Max)
            {
                return pair.Key;
            }
        }

        return penetration > 130 ? "pen_lvl_11" : "pen_lvl_1";
    }

    public static double? ExtractPenetrationValue(JsonObject patch, ItemInfo itemInfo)
    {
        if (RealismPatchGenerator.TryGetNumericValue(patch["PenetrationPower"], out var patchValue))
        {
            return patchValue;
        }

        foreach (var key in new[] { "PenetrationPower", "Penetration", "penPower" })
        {
            if (RealismPatchGenerator.TryGetNumericValue(itemInfo.Properties[key], out var propertyValue))
            {
                return propertyValue;
            }
        }

        return null;
    }
}