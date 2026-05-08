using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class GearRuleEngine
{
    public static void ApplyGearSanityCheck(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        ApplyGearSanityCheck(new PatchRuleContext(generator, rules, patch, itemInfo));
    }

    public static void ApplyGearSanityCheck(PatchRuleContext context)
    {
        var generator = context.Generator;
        var rules = context.Rules;
        var patch = context.Patch;
        var itemInfo = context.ItemInfo;

        RealismPatchGenerator.ApplyFieldClamps(patch, rules.Gear.GearClampRules);

        var gearProfile = context.GetGearProfile();
        if (string.IsNullOrWhiteSpace(gearProfile) || !rules.Gear.GearProfileRanges.TryGetValue(gearProfile, out var ranges))
        {
            ApplyGearPriceRule(generator, rules, patch, itemInfo, gearProfile);
            RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
            return;
        }

        generator.ApplyNumericRanges(patch, ranges, ensureFields: true);
        RealismPatchGenerator.ApplyFieldClamps(patch, rules.Gear.GearClampRules);
        ApplyGearPriceRule(generator, rules, patch, itemInfo, gearProfile);
        RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
    }

    public static void ApplyGearPriceRule(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo, string? gearProfile)
    {
        var priceRange = ResolveGearPriceRange(rules, gearProfile);
        var priceScore = CalculateGearPriceScore(generator, patch, itemInfo, gearProfile);
        var resolvedPrice = priceRange.Min + ((priceRange.Max - priceRange.Min) * priceScore);
        patch["Price"] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(resolvedPrice, priceRange.Min, priceRange.Max), true, priceRange.Min, priceRange.Max);
    }

    public static NumericRange ResolveGearPriceRange(RuleSet rules, string? gearProfile)
    {
        if (!string.IsNullOrWhiteSpace(gearProfile) && rules.Gear.GearPriceRanges.TryGetValue(gearProfile, out var range))
        {
            return range;
        }

        return new NumericRange(4000, 18000, true);
    }

    public static double CalculateGearPriceScore(RealismPatchGenerator generator, JsonObject patch, ItemInfo itemInfo, string? gearProfile)
    {
        var armorScore = CalculateArmorProtectionScore(generator, patch, itemInfo);
        var carryScore = CalculateCarryCapacityScore(itemInfo);
        var utilityScore = CalculateGearUtilityScore(patch, itemInfo);
        var mobilityBurdenScore = CalculateMobilityBurdenScore(patch);

        var score = 0.32;
        score += armorScore * 0.38;
        score += carryScore * 0.22;
        score += utilityScore * 0.18;

        if (gearProfile is not null && (gearProfile.Contains("armor", StringComparison.OrdinalIgnoreCase) || gearProfile.Contains("helmet", StringComparison.OrdinalIgnoreCase)))
        {
            score += mobilityBurdenScore * 0.10;
        }
        else if (gearProfile is not null && (gearProfile.Contains("backpack", StringComparison.OrdinalIgnoreCase) || gearProfile.Contains("rig", StringComparison.OrdinalIgnoreCase) || gearProfile.Contains("panel", StringComparison.OrdinalIgnoreCase) || gearProfile.Contains("belt", StringComparison.OrdinalIgnoreCase)))
        {
            score += carryScore * 0.10;
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Price"], out var currentPrice))
        {
            score += RealismPatchGenerator.Normalize(currentPrice, 500, 150000) * 0.06;
        }

        return RealismPatchGenerator.Clamp(score, 0.08, 0.95);
    }

    public static double CalculateArmorProtectionScore(RealismPatchGenerator generator, JsonObject patch, ItemInfo itemInfo)
    {
        var armorText = PatchAnalysisContextFactory.Create(generator, patch, itemInfo).GearArmorClassText;
        var armorClassScore = GetArmorClassScore(armorText);
        var spallScore = RealismPatchGenerator.TryGetNumericValue(patch["SpallReduction"], out var spallReduction)
            ? RealismPatchGenerator.Normalize(spallReduction, 0.0, 1.0)
            : 0.0;
        var canSpallBonus = RealismPatchGenerator.ToOptionalBool(patch["CanSpall"]) == true ? 0.05 : 0.0;
        return RealismPatchGenerator.Clamp((armorClassScore * 0.75) + (spallScore * 0.20) + canSpallBonus, 0.0, 1.0);
    }

    public static double CalculateArmorProtectionScore(PatchRuleContext context)
    {
        var armorClassScore = GetArmorClassScore(context.AnalysisContext.GearArmorClassText);
        var spallScore = RealismPatchGenerator.TryGetNumericValue(context.Patch["SpallReduction"], out var spallReduction)
            ? RealismPatchGenerator.Normalize(spallReduction, 0.0, 1.0)
            : 0.0;
        var canSpallBonus = RealismPatchGenerator.ToOptionalBool(context.Patch["CanSpall"]) == true ? 0.05 : 0.0;
        return RealismPatchGenerator.Clamp((armorClassScore * 0.75) + (spallScore * 0.20) + canSpallBonus, 0.0, 1.0);
    }

    public static double CalculateCarryCapacityScore(ItemInfo itemInfo)
    {
        var cells = GetGridCellCapacity(itemInfo.Properties);
        if (cells <= 0)
        {
            cells = GetGridCellCapacity(itemInfo.SourceProperties);
        }

        var slots = GetSlotCount(itemInfo.Properties);
        if (slots <= 0)
        {
            slots = GetSlotCount(itemInfo.SourceProperties);
        }

        return RealismPatchGenerator.Clamp((cells / 42.0) + (slots / 10.0), 0.0, 1.0);
    }

    public static double CalculateGearUtilityScore(JsonObject patch, ItemInfo itemInfo)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["Comfort"], out var comfort))
        {
            contributions.Add(RealismPatchGenerator.Normalize(comfort, 0.6, 1.4));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["ReloadSpeedMulti"], out var reloadSpeed))
        {
            contributions.Add(RealismPatchGenerator.Normalize(reloadSpeed, 0.85, 1.25));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["GasProtection"], out var gasProtection))
        {
            contributions.Add(RealismPatchGenerator.Normalize(gasProtection, 0.0, 1.0));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["RadProtection"], out var radProtection))
        {
            contributions.Add(RealismPatchGenerator.Normalize(radProtection, 0.0, 1.0));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["dB"], out var dbValue))
        {
            contributions.Add(RealismPatchGenerator.Normalize(dbValue, 15, 30));
        }

        var weightSource = RealismPatchGenerator.TryGetNumericValue(patch["Weight"], out var patchWeight)
            ? patchWeight
            : RealismPatchGenerator.TryGetNumericValue(itemInfo.Properties["Weight"], out var sourceWeight)
                ? sourceWeight
                : 0.0;
        if (weightSource > 0)
        {
            contributions.Add(RealismPatchGenerator.Normalize(weightSource, 0.1, 12.0) * 0.35);
        }

        if (contributions.Count == 0)
        {
            return 0.0;
        }

        return RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateMobilityBurdenScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["speedPenaltyPercent"], out var speedPenalty))
        {
            contributions.Add(RealismPatchGenerator.Normalize(Math.Abs(Math.Min(speedPenalty, 0.0)), 0.0, 20.0));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["weaponErgonomicPenalty"], out var ergoPenalty))
        {
            contributions.Add(RealismPatchGenerator.Normalize(Math.Abs(Math.Min(ergoPenalty, 0.0)), 0.0, 20.0));
        }

        if (contributions.Count == 0)
        {
            return 0.0;
        }

        return RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double GetArmorClassScore(string armorText)
    {
        if (string.IsNullOrWhiteSpace(armorText))
        {
            return 0.0;
        }

        if (ContainsAnyKeyword(armorText, ["gost 6a", "nij iv", "rf3", "xsapi", "esapi", "pm 10", "pm 8"]))
        {
            return 1.0;
        }

        if (ContainsAnyKeyword(armorText, ["gost 6", "gost 5a", "nij iii+", "rf2", "rev. j", "rev. g", "pm 5"]))
        {
            return 0.85;
        }

        if (ContainsAnyKeyword(armorText, ["gost 5", "gost 4", "nij iii", "rf1", "mk4a"]))
        {
            return 0.70;
        }

        if (ContainsAnyKeyword(armorText, ["gost 3a", "gost 3", "nij ii", "nij iia", "iiia", "3a", "pm 3", "pm 2"]))
        {
            return 0.45;
        }

        if (ContainsAnyKeyword(armorText, ["gost 2a", "gost 2", "ballistic", "ansi", "z87", "v50", "anti-shatter"]))
        {
            return 0.25;
        }

        return 0.10;
    }

    public static int GetGridCellCapacity(JsonObject properties)
    {
        if (properties["Grids"] is not JsonArray grids)
        {
            return 0;
        }

        var totalCells = 0;
        foreach (var grid in grids.OfType<JsonObject>())
        {
            var props = grid["_props"] as JsonObject ?? grid["props"] as JsonObject ?? grid;
            if (!RealismPatchGenerator.TryGetNumericValue(props["cellsH"], out var cellsH) || !RealismPatchGenerator.TryGetNumericValue(props["cellsV"], out var cellsV))
            {
                continue;
            }

            totalCells += Math.Max(0, (int)Math.Round(cellsH)) * Math.Max(0, (int)Math.Round(cellsV));
        }

        return totalCells;
    }

    public static int GetSlotCount(JsonObject properties)
    {
        if (properties["Slots"] is not JsonArray slots)
        {
            return 0;
        }

        return slots.Count;
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