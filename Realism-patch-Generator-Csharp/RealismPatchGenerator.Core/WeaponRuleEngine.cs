using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class WeaponRuleEngine
{
    public static void ApplyWeaponSanityCheck(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        ApplyWeaponSanityCheck(new PatchRuleContext(generator, rules, patch, itemInfo));
    }

    public static void ApplyWeaponSanityCheck(PatchRuleContext context)
    {
        var generator = context.Generator;
        var rules = context.Rules;
        var patch = context.Patch;
        var itemInfo = context.ItemInfo;

        RealismPatchGenerator.ApplyFieldClamps(patch, rules.Weapon.GunClampRules);
        if (RealismPatchGenerator.TryGetNumericValue(patch["RecoilAngle"], out var recoilAngle) && (recoilAngle < 30 || recoilAngle > 150))
        {
            patch["RecoilAngle"] = 90;
        }

        var weaponProfile = context.GetWeaponProfile();
        var preserveExistingValues = true;
        if (!string.IsNullOrWhiteSpace(weaponProfile) && rules.Weapon.WeaponProfileRanges.TryGetValue(weaponProfile, out var ranges))
        {
            if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["WeapType"])))
            {
                patch["WeapType"] = GetDefaultWeaponTypeForProfile(weaponProfile);
                context.InvalidateAnalysis();
            }

            generator.ApplyNumericRanges(patch, ranges, ensureFields: true, preserveExistingValues);
            ApplyWeaponRefinementRanges(context, weaponProfile, preserveExistingValues);
            RealismPatchGenerator.ApplyFieldClamps(patch, rules.Weapon.GunClampRules);
        }

        ApplyWeaponPriceRule(rules, patch, itemInfo, weaponProfile, context.GetWeaponCaliberProfile(), context.GetWeaponStockProfile());

        if (string.Equals(weaponProfile, "pistol", StringComparison.OrdinalIgnoreCase))
        {
            patch["HasShoulderContact"] = false;
        }

        RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
    }

    public static string GetDefaultWeaponTypeForProfile(string profile)
    {
        return profile.ToLowerInvariant() switch
        {
            "pistol" => "pistol",
            "smg" => "smg",
            "sniper" => "sniper",
            "shotgun" => "shotgun",
            "machinegun" => "machinegun",
            "launcher" => "launcher",
            _ => "rifle",
        };
    }

    public static void ApplyWeaponPriceRule(RuleSet rules, JsonObject patch, ItemInfo itemInfo, string? weaponProfile, string? caliberProfile, string stockProfile)
    {
        var priceRange = ResolveWeaponPriceRange(rules, weaponProfile);
        var priceScore = CalculateWeaponPriceScore(patch, itemInfo, weaponProfile, caliberProfile, stockProfile);
        var resolvedPrice = priceRange.Min + ((priceRange.Max - priceRange.Min) * priceScore);
        patch["Price"] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(resolvedPrice, priceRange.Min, priceRange.Max), true, priceRange.Min, priceRange.Max);
    }

    public static void ApplyWeaponRefinementRanges(PatchRuleContext context, string weaponProfile, bool preserveExistingValues)
    {
        var generator = context.Generator;
        var rules = context.Rules;
        var patch = context.Patch;

        if (!rules.Weapon.WeaponProfileRanges.TryGetValue(weaponProfile, out var baseRanges))
        {
            return;
        }

        var caliberProfile = context.GetWeaponCaliberProfile();
        var stockProfile = context.GetWeaponStockProfile();
        var caliberMods = !string.IsNullOrWhiteSpace(caliberProfile) && rules.Weapon.WeaponCaliberRuleModifiers.TryGetValue(caliberProfile, out var resolvedCaliberMods)
            ? resolvedCaliberMods
            : null;
        var stockMods = rules.Weapon.WeaponStockRuleModifiers.TryGetValue(stockProfile, out var resolvedStockMods)
            ? resolvedStockMods
            : null;

        foreach (var pair in baseRanges)
        {
            if (patch[pair.Key] is null)
            {
                patch[pair.Key] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.GetRangeSeedValue(pair.Value.Min, pair.Value.Max, pair.Value.PreferInt), pair.Value.PreferInt);
            }

            var deltaMin = 0.0;
            var deltaMax = 0.0;
            if (caliberMods is not null && caliberMods.TryGetValue(pair.Key, out var caliberRange))
            {
                deltaMin += caliberRange.Min;
                deltaMax += caliberRange.Max;
            }

            if (stockMods is not null && stockMods.TryGetValue(pair.Key, out var stockRange))
            {
                deltaMin += stockRange.Min;
                deltaMax += stockRange.Max;
            }

            if (deltaMin == 0 && deltaMax == 0)
            {
                continue;
            }

            var min = pair.Value.Min + deltaMin;
            var max = pair.Value.Max + deltaMax;
            patch[pair.Key] = preserveExistingValues
                ? RealismPatchGenerator.ClampRangeValue(patch[pair.Key], min, max, pair.Value.PreferInt)
                : generator.SampleRangeValue(patch[pair.Key], min, max, pair.Value.PreferInt);
        }

        var supplementalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (caliberMods is not null)
        {
            foreach (var key in caliberMods.Keys)
            {
                supplementalKeys.Add(key);
            }
        }

        if (stockMods is not null)
        {
            foreach (var key in stockMods.Keys)
            {
                supplementalKeys.Add(key);
            }
        }

        foreach (var key in supplementalKeys)
        {
            if (baseRanges.ContainsKey(key))
            {
                continue;
            }

            var ranges = new List<NumericRange>();
            if (caliberMods is not null && caliberMods.TryGetValue(key, out var caliberRange))
            {
                ranges.Add(caliberRange);
            }

            if (stockMods is not null && stockMods.TryGetValue(key, out var stockRange))
            {
                ranges.Add(stockRange);
            }

            if (ranges.Count == 0)
            {
                continue;
            }

            var min = ranges.Sum(range => range.Min);
            var max = ranges.Sum(range => range.Max);
            var preferInt = ranges.All(range => range.PreferInt);
            if (patch[key] is null)
            {
                patch[key] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.GetRangeSeedValue(min, max, preferInt), preferInt);
            }

            patch[key] = preserveExistingValues
                ? RealismPatchGenerator.ClampRangeValue(patch[key], min, max, preferInt)
                : generator.SampleRangeValue(patch[key], min, max, preferInt);
        }
    }

    public static NumericRange ResolveWeaponPriceRange(RuleSet rules, string? weaponProfile)
    {
        if (!string.IsNullOrWhiteSpace(weaponProfile) && rules.Weapon.GunPriceRanges.TryGetValue(weaponProfile, out var range))
        {
            return range;
        }

        return new NumericRange(15000, 70000, true);
    }

    public static double CalculateWeaponPriceScore(JsonObject patch, ItemInfo itemInfo, string? weaponProfile, string? caliberProfile, string stockProfile)
    {
        var recoilScore = CalculateWeaponRecoilScore(patch);
        var handlingScore = CalculateWeaponHandlingScore(patch);
        var accuracyScore = CalculateWeaponAccuracyScore(patch);
        var rateScore = CalculateWeaponRateScore(patch);
        var caliberScore = CalculateWeaponCaliberPremium(caliberProfile);
        var stockScore = CalculateWeaponStockPremium(stockProfile);
        var weightScore = CalculateWeaponWeightScore(patch, itemInfo);

        var score = 0.22;
        score += recoilScore * 0.24;
        score += handlingScore * 0.17;
        score += accuracyScore * 0.16;
        score += rateScore * 0.08;
        score += caliberScore * 0.13;
        score += stockScore * 0.08;
        score += weightScore * 0.07;

        if (RealismPatchGenerator.TryGetNumericValue(patch["Price"], out var currentPrice))
        {
            score += RealismPatchGenerator.Normalize(currentPrice, 5000, 250000) * 0.07;
        }

        if (string.Equals(weaponProfile, "launcher", StringComparison.OrdinalIgnoreCase))
        {
            score += 0.08;
        }

        return RealismPatchGenerator.Clamp(score, 0.08, 0.96);
    }

    public static double CalculateWeaponRecoilScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["VerticalRecoil"], out var verticalRecoil))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(verticalRecoil, 70, 420));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["HorizontalRecoil"], out var horizontalRecoil))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(horizontalRecoil, 90, 420));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["RecoilIntensity"], out var recoilIntensity))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(recoilIntensity, 0.08, 0.55));
        }

        return contributions.Count == 0 ? 0.4 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateWeaponHandlingScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["Ergonomics"], out var ergonomics))
        {
            contributions.Add(RealismPatchGenerator.Normalize(ergonomics, 45, 100));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["VisualMulti"], out var visualMulti))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(visualMulti, 0.8, 2.8));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["BaseReloadSpeedMulti"], out var reloadSpeed))
        {
            contributions.Add(RealismPatchGenerator.Normalize(reloadSpeed, 0.84, 1.08));
        }

        return contributions.Count == 0 ? 0.35 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateWeaponAccuracyScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["Dispersion"], out var dispersion))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(dispersion, 0.5, 18.0));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Convergence"], out var convergence))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(convergence, 1.0, 25.0));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["CenterOfImpact"], out var centerOfImpact))
        {
            contributions.Add(1.0 - RealismPatchGenerator.Normalize(Math.Abs(centerOfImpact), 0.0, 0.2));
        }

        return contributions.Count == 0 ? 0.35 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateWeaponRateScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["AutoROF"], out var autoRof))
        {
            contributions.Add(RealismPatchGenerator.Normalize(autoRof, 200, 950));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["SemiROF"], out var semiRof))
        {
            contributions.Add(RealismPatchGenerator.Normalize(semiRof, 120, 450));
        }

        return contributions.Count == 0 ? 0.25 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateWeaponCaliberPremium(string? caliberProfile)
    {
        return caliberProfile?.ToLowerInvariant() switch
        {
            "magnum_heavy" => 1.0,
            "full_power_rifle_rimmed" => 0.88,
            "full_power_rifle" => 0.82,
            "subsonic_heavy_9x39" => 0.68,
            "pdw_high_pen_small" => 0.64,
            "intermediate_rifle_762x39" => 0.58,
            "intermediate_rifle_58x42" => 0.55,
            "small_high_velocity" => 0.52,
            "shotgun_shell_23x75" => 0.5,
            "shotgun_shell_12g" => 0.42,
            "shotgun_shell_20g" => 0.34,
            "pistol_caliber" => 0.24,
            _ => 0.35,
        };
    }

    public static double CalculateWeaponStockPremium(string stockProfile)
    {
        return stockProfile.ToLowerInvariant() switch
        {
            "fixed_stock" => 0.55,
            "bullpup" => 0.52,
            "folding_stock_extended" => 0.42,
            "folding_stock_collapsed" => 0.26,
            "stockless" => 0.18,
            _ => 0.35,
        };
    }

    public static double CalculateWeaponWeightScore(JsonObject patch, ItemInfo itemInfo)
    {
        var weight = RealismPatchGenerator.TryGetNumericValue(patch["Weight"], out var patchWeight)
            ? patchWeight
            : RealismPatchGenerator.TryGetNumericValue(itemInfo.Properties["Weight"], out var sourceWeight)
                ? sourceWeight
                : 0.0;
        if (weight <= 0)
        {
            return 0.3;
        }

        return RealismPatchGenerator.Normalize(weight, 0.7, 9.5);
    }
}