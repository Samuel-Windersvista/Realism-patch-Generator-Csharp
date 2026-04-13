using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class AttachmentRuleEngine
{
    public static void ApplyAttachmentSanityCheck(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        ApplyAttachmentSanityCheck(new PatchRuleContext(generator, rules, patch, itemInfo));
    }

    public static void ApplyAttachmentSanityCheck(PatchRuleContext context)
    {
        var rules = context.Rules;
        var patch = context.Patch;
        var itemInfo = context.ItemInfo;

        RealismPatchGenerator.ApplyFieldClamps(patch, rules.Attachment.ModClampRules);

        if (string.IsNullOrWhiteSpace(RealismPatchGenerator.GetText(patch["ModType"])))
        {
            var resolvedModType = RealismPatchGenerator.ResolveEffectiveModType(itemInfo.SourceProperties, patch, itemInfo.TemplateFile);
            if (!string.IsNullOrWhiteSpace(resolvedModType))
            {
                patch["ModType"] = resolvedModType;
                context.InvalidateAnalysis();
            }
        }

        if ((patch["ModType"]?.GetValue<string?>() ?? string.Empty).Equals("barrel_2slot", StringComparison.OrdinalIgnoreCase))
        {
            if (RealismPatchGenerator.TryGetNumericValue(patch["ModShotDispersion"], out var modShotDispersion))
            {
                patch["ModShotDispersion"] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(modShotDispersion, 0, 0), RealismPatchGenerator.IsIntegerNode(patch["ModShotDispersion"]));
            }
            else
            {
                patch["ModShotDispersion"] = 0;
            }
        }

        if ((patch["ModType"]?.GetValue<string?>() ?? string.Empty).Equals("bipod", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var fieldName in new[] { "AutoROF", "SemiROF", "ModMalfunctionChance", "ReloadSpeed", "FixSpeed" })
            {
                if (RealismPatchGenerator.TryGetNumericValue(patch[fieldName], out var numericValue))
                {
                    patch[fieldName] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(numericValue, 0, 0), RealismPatchGenerator.IsIntegerNode(patch[fieldName]));
                }
                else
                {
                    patch[fieldName] = 0;
                }
            }
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Velocity"], out var velocity))
        {
            var maxVelocity = RealismPatchGenerator.GetLowerText(patch["Name"]).Contains("barrel", StringComparison.OrdinalIgnoreCase) ? 15.0 : 5.0;
            patch["Velocity"] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(velocity, -maxVelocity, maxVelocity), RealismPatchGenerator.IsIntegerNode(patch["Velocity"]));
        }

        var modProfile = context.GetModProfile();
        if (string.IsNullOrWhiteSpace(modProfile) || !rules.Attachment.ModProfileRanges.TryGetValue(modProfile, out var ranges))
        {
            RemoveAttachmentFieldsByProfile(patch, modProfile);
            ApplyAttachmentPriceRule(rules, patch, itemInfo, modProfile);
            RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
            return;
        }

        context.Generator.ApplyNumericRanges(patch, ranges, ensureFields: true);
        ApplyAttachmentPreservedSourceFields(patch, itemInfo, modProfile, ranges);
        RemoveAttachmentFieldsByProfile(patch, modProfile);
        RealismPatchGenerator.ApplyFieldClamps(patch, rules.Attachment.ModClampRules);
        ApplyAttachmentPriceRule(rules, patch, itemInfo, modProfile);
        if (modProfile.StartsWith("muzzle_suppressor", StringComparison.OrdinalIgnoreCase))
        {
            patch["CanCycleSubs"] = true;
        }

        RealismPatchGenerator.ApplyGlobalSafetyClamps(patch);
    }

    public static void ApplyAttachmentPriceRule(RuleSet rules, JsonObject patch, ItemInfo itemInfo, string? modProfile)
    {
        var priceRange = ResolveAttachmentPriceRange(rules, modProfile);
        var priceScore = CalculateAttachmentPriceScore(patch, itemInfo, modProfile);
        var resolvedPrice = priceRange.Min + ((priceRange.Max - priceRange.Min) * priceScore);
        patch["Price"] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(resolvedPrice, priceRange.Min, priceRange.Max), true, priceRange.Min, priceRange.Max);
    }

    public static NumericRange ResolveAttachmentPriceRange(RuleSet rules, string? modProfile)
    {
        if (!string.IsNullOrWhiteSpace(modProfile))
        {
            if (rules.Attachment.ModPriceRanges.TryGetValue(modProfile, out var exactRange))
            {
                return exactRange;
            }

            var fallbackKeys = new[]
            {
                modProfile.Split('_')[0],
                modProfile.StartsWith("muzzle_", StringComparison.OrdinalIgnoreCase) ? "muzzle_adapter" : null,
                modProfile.StartsWith("magazine_", StringComparison.OrdinalIgnoreCase) ? "magazine" : null,
                modProfile.StartsWith("stock_", StringComparison.OrdinalIgnoreCase) ? "stock" : null,
                modProfile.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase) ? "handguard_medium" : null,
                modProfile.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase) ? "barrel_medium" : null,
            };

            foreach (var fallbackKey in fallbackKeys)
            {
                if (!string.IsNullOrWhiteSpace(fallbackKey) && rules.Attachment.ModPriceRanges.TryGetValue(fallbackKey, out var fallbackRange))
                {
                    return fallbackRange;
                }
            }
        }

        return new NumericRange(1500, 12000, true);
    }

    public static double CalculateAttachmentPriceScore(JsonObject patch, ItemInfo itemInfo, string? modProfile)
    {
        var recoilScore = CalculateAttachmentRecoilBenefitScore(patch);
        var handlingScore = CalculateAttachmentHandlingScore(patch);
        var utilityScore = CalculateAttachmentUtilityScore(patch, itemInfo, modProfile);
        var profilePremium = CalculateAttachmentProfilePremium(modProfile);

        var score = 0.18;
        score += recoilScore * 0.24;
        score += handlingScore * 0.20;
        score += utilityScore * 0.22;
        score += profilePremium * 0.16;

        if (RealismPatchGenerator.TryGetNumericValue(patch["Price"], out var currentPrice))
        {
            score += RealismPatchGenerator.Normalize(currentPrice, 300, 120000) * 0.08;
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Weight"], out var weight))
        {
            score += RealismPatchGenerator.Normalize(weight, 0.02, 2.0) * 0.06;
        }

        return RealismPatchGenerator.Clamp(score, 0.05, 0.97);
    }

    public static double CalculateAttachmentRecoilBenefitScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["VerticalRecoil"], out var verticalRecoil))
        {
            contributions.Add(RealismPatchGenerator.Normalize(-verticalRecoil, 0, 20));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["HorizontalRecoil"], out var horizontalRecoil))
        {
            contributions.Add(RealismPatchGenerator.Normalize(-horizontalRecoil, 0, 18));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["CameraRecoil"], out var cameraRecoil))
        {
            contributions.Add(RealismPatchGenerator.Normalize(-cameraRecoil, 0, 20));
        }

        return contributions.Count == 0 ? 0.12 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateAttachmentHandlingScore(JsonObject patch)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["Ergonomics"], out var ergonomics))
        {
            contributions.Add(RealismPatchGenerator.Normalize(ergonomics, -15, 18));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["AimSpeed"], out var aimSpeed))
        {
            contributions.Add(RealismPatchGenerator.Normalize(aimSpeed, -20, 12));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Handling"], out var handling))
        {
            contributions.Add(RealismPatchGenerator.Normalize(handling, -12, 20));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["AimStability"], out var aimStability))
        {
            contributions.Add(RealismPatchGenerator.Normalize(aimStability, 0, 20));
        }

        return contributions.Count == 0 ? 0.18 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateAttachmentUtilityScore(JsonObject patch, ItemInfo itemInfo, string? modProfile)
    {
        var contributions = new List<double>();

        if (RealismPatchGenerator.TryGetNumericValue(patch["Accuracy"], out var accuracy))
        {
            contributions.Add(RealismPatchGenerator.Normalize(accuracy, -15, 15));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Loudness"], out var loudness) && modProfile?.StartsWith("muzzle_", StringComparison.OrdinalIgnoreCase) == true)
        {
            contributions.Add(RealismPatchGenerator.Normalize(-loudness, 0, 40));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["Flash"], out var flash) && modProfile?.StartsWith("muzzle_", StringComparison.OrdinalIgnoreCase) == true)
        {
            contributions.Add(RealismPatchGenerator.Normalize(-flash, 0, 80));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["ReloadSpeed"], out var reloadSpeed))
        {
            contributions.Add(RealismPatchGenerator.Normalize(reloadSpeed, -20, 12));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["LoadUnloadModifier"], out var loadUnload))
        {
            contributions.Add(RealismPatchGenerator.Normalize(loadUnload, 0, 25));
        }

        if (RealismPatchGenerator.TryGetNumericValue(patch["ChamberSpeed"], out var chamberSpeed))
        {
            contributions.Add(RealismPatchGenerator.Normalize(chamberSpeed, -5, 40));
        }

        if (modProfile?.StartsWith("magazine", StringComparison.OrdinalIgnoreCase) == true)
        {
            var capacity = RealismPatchGenerator.ExtractMagCapacity(itemInfo, RealismPatchGenerator.GetLowerText(patch["Name"]));
            if (capacity is not null)
            {
                contributions.Add(RealismPatchGenerator.Normalize(capacity.Value, 5, 95));
            }
        }

        if (modProfile?.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase) == true)
        {
            var length = RealismPatchGenerator.ExtractBarrelLengthMm(RealismPatchGenerator.GetLowerText(patch["Name"]));
            if (length is not null)
            {
                contributions.Add(RealismPatchGenerator.Normalize(length.Value, 100, 650));
            }
        }

        return contributions.Count == 0 ? 0.15 : RealismPatchGenerator.Clamp(contributions.Average(), 0.0, 1.0);
    }

    public static double CalculateAttachmentProfilePremium(string? modProfile)
    {
        if (string.IsNullOrWhiteSpace(modProfile))
        {
            return 0.12;
        }

        return modProfile.ToLowerInvariant() switch
        {
            "ubgl" => 1.0,
            "barrel_integral_suppressed" => 0.88,
            "scope_magnified" => 0.78,
            "muzzle_suppressor" => 0.76,
            "muzzle_suppressor_compact" => 0.68,
            "receiver" => 0.56,
            "barrel_long" => 0.55,
            "barrel_medium" => 0.48,
            "barrel_short" => 0.42,
            "stock_ads_support" => 0.46,
            "stock_fixed" => 0.42,
            "stock_folding" => 0.38,
            "scope_red_dot" => 0.4,
            "magazine_drum" => 0.52,
            "magazine_extended" => 0.38,
            "magazine_standard" => 0.24,
            "magazine_compact" => 0.18,
            "foregrip" => 0.32,
            "flashlight_laser" => 0.3,
            "mount" => 0.18,
            "gasblock" => 0.14,
            "pistol_grip" => 0.22,
            "bipod" => 0.34,
            _ when modProfile.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase) => 0.3,
            _ when modProfile.StartsWith("muzzle_", StringComparison.OrdinalIgnoreCase) => 0.26,
            _ when modProfile.StartsWith("stock_", StringComparison.OrdinalIgnoreCase) => 0.24,
            _ => 0.16,
        };
    }

    private static void RemoveAttachmentFieldsByProfile(JsonObject patch, string? modProfile)
    {
        if (string.IsNullOrWhiteSpace(modProfile))
        {
            return;
        }

        if (modProfile.StartsWith("handguard", StringComparison.OrdinalIgnoreCase))
        {
            patch.Remove("ChamberSpeed");
        }
    }

    private static void ApplyAttachmentPreservedSourceFields(JsonObject patch, ItemInfo itemInfo, string modProfile, IReadOnlyDictionary<string, NumericRange> ranges)
    {
        if (string.Equals(modProfile, "gasblock", StringComparison.OrdinalIgnoreCase))
        {
            PreserveSourceFieldWithinRange(patch, itemInfo.SourceProperties, "Loudness", ranges);
            PreserveSourceFieldWithinRange(patch, itemInfo.SourceProperties, "Velocity", ranges);
            return;
        }

        if (string.Equals(modProfile, "iron_sight", StringComparison.OrdinalIgnoreCase))
        {
            PreserveSourceFieldWithinRange(patch, itemInfo.SourceProperties, "Accuracy", ranges);
            return;
        }

        if (!modProfile.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PreserveSourceFieldWithinRange(patch, itemInfo.SourceProperties, "Accuracy", ranges);
        PreserveSourceFieldWithinRange(patch, itemInfo.SourceProperties, "Dispersion", ranges);
    }

    private static void PreserveSourceFieldWithinRange(JsonObject patch, JsonObject sourceProperties, string fieldName, IReadOnlyDictionary<string, NumericRange> ranges)
    {
        if (!ranges.TryGetValue(fieldName, out var range)
            || sourceProperties[fieldName] is null
            || !RealismPatchGenerator.TryGetNumericValue(sourceProperties[fieldName], out var sourceValue))
        {
            return;
        }

        patch[fieldName] = RealismPatchGenerator.CreateNumericNode(RealismPatchGenerator.Clamp(sourceValue, range.Min, range.Max), range.PreferInt);
    }
}