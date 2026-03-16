using System.Collections.ObjectModel;

namespace RealismPatchGenerator.Core;

internal static class GearRuleData
{
    public static readonly IReadOnlyDictionary<string, NumericRange> GearClampRules =
        new ReadOnlyDictionary<string, NumericRange>(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReloadSpeedMulti"] = new(0.85, 1.25),
            ["Comfort"] = new(0.6, 1.4),
            ["speedPenaltyPercent"] = new(-40, 10),
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> GearProfileRanges =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["armor_vest_light"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.15, 0.55),
                ["ReloadSpeedMulti"] = new(1.0, 1.06),
                ["Comfort"] = new(0.9, 1.08),
                ["speedPenaltyPercent"] = new(-4.5, 0.0),
                ["weaponErgonomicPenalty"] = new(-5.5, 0.0),
            }),
            ["armor_vest_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.55, 0.92),
                ["ReloadSpeedMulti"] = new(0.97, 1.03),
                ["Comfort"] = new(1.0, 1.14),
                ["speedPenaltyPercent"] = new(-8.0, -0.8),
                ["weaponErgonomicPenalty"] = new(-10.0, -1.0),
            }),
            ["armor_chest_rig_light"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.22, 0.55),
                ["ReloadSpeedMulti"] = new(0.95, 1.16),
                ["Comfort"] = new(0.76, 1.12),
                ["speedPenaltyPercent"] = new(-4.5, 0.0),
                ["weaponErgonomicPenalty"] = new(-4.0, 0.0),
            }),
            ["armor_chest_rig_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.55, 0.9),
                ["ReloadSpeedMulti"] = new(0.89, 1.08),
                ["Comfort"] = new(0.72, 1.1),
                ["speedPenaltyPercent"] = new(-6.5, -0.2),
                ["weaponErgonomicPenalty"] = new(-6.5, -0.5),
            }),
            ["chest_rig_light"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(1.0, 1.0),
                ["ReloadSpeedMulti"] = new(0.98, 1.17),
                ["Comfort"] = new(0.76, 1.18),
                ["speedPenaltyPercent"] = new(-0.4, 0.0),
            }),
            ["chest_rig_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(1.0, 1.0),
                ["ReloadSpeedMulti"] = new(0.86, 1.08),
                ["Comfort"] = new(0.7, 1.1),
                ["speedPenaltyPercent"] = new(-1.0, -0.2),
            }),
            ["helmet_light"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(1.0, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["Comfort"] = new(0.82, 1.06),
            }),
            ["helmet_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(1.0, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["Comfort"] = new(0.95, 1.16),
            }),
            ["armor_component_accessory"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.45, 0.85),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_component_faceshield"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.75, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_mask_decorative"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(1.0, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_mask_ballistic"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.7, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_plate_hard"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.18, 0.85),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_plate_helmet"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.55, 1.0),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["armor_plate_soft"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.1, 0.45),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["backpack_compact"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["Comfort"] = new(0.9, 1.18),
                ["speedPenaltyPercent"] = new(-2.8, -0.6),
            }),
            ["backpack_full"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["Comfort"] = new(0.74, 0.96),
                ["speedPenaltyPercent"] = new(-4.8, -2.0),
            }),
            ["back_panel"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReloadSpeedMulti"] = new(0.97, 1.02),
                ["Comfort"] = new(0.9, 1.0),
                ["speedPenaltyPercent"] = new(-0.65, -0.15),
                ["weaponErgonomicPenalty"] = new(0.0, 0.0),
            }),
            ["belt_harness"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReloadSpeedMulti"] = new(1.0, 1.1),
                ["Comfort"] = new(1.0, 1.12),
                ["speedPenaltyPercent"] = new(-0.55, 0.0),
                ["weaponErgonomicPenalty"] = new(0.0, 0.0),
            }),
            ["headset"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["dB"] = new(19, 26, true),
            }),
            ["cosmetic_headwear"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["speedPenaltyPercent"] = new(0.0, 0.0),
                ["weaponErgonomicPenalty"] = new(0.0, 0.0),
            }),
            ["protective_eyewear_standard"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.35, 0.62),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["protective_eyewear_ballistic"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["SpallReduction"] = new(0.62, 0.9),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
            }),
            ["cosmetic_gasmask"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["GasProtection"] = new(0.82, 0.90),
                ["RadProtection"] = new(0.75, 0.82),
                ["ReloadSpeedMulti"] = new(1.0, 1.0),
                ["speedPenaltyPercent"] = new(-10.0, 0.0),
                ["weaponErgonomicPenalty"] = new(-24.0, 0.0),
            }),
        });

    private static IReadOnlyDictionary<string, NumericRange> CreateRanges(Dictionary<string, NumericRange> ranges)
    {
        return new ReadOnlyDictionary<string, NumericRange>(ranges);
    }

    public static GearRules CreateDefaultRules()
    {
        return new GearRules
        {
            GearClampRules = GearClampRules,
            GearProfileRanges = GearProfileRanges,
        };
    }
}