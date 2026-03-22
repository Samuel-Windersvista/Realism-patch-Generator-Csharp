using System.Collections.ObjectModel;

namespace RealismPatchGenerator.Core;

internal static class AttachmentRuleData
{
    public static readonly IReadOnlyDictionary<string, NumericRange> ModClampRules =
        new ReadOnlyDictionary<string, NumericRange>(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
        {
            ["VerticalRecoil"] = new(-35, 35),
            ["HorizontalRecoil"] = new(-35, 35),
            ["Dispersion"] = new(-55, 55),
            ["Loudness"] = new(-45, 50, true),
            ["Accuracy"] = new(-15, 15, true),
            ["LoyaltyLevel"] = new(1, 4, true),
        });

    public static readonly IReadOnlyDictionary<string, string> ModParentBaseProfiles =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["5448bc234bdc2d3c308b4569"] = "magazine",
            ["56ea9461d2720b67698b456f"] = "gasblock",
            ["55818a304bdc2db5418b457d"] = "receiver",
            ["55818a684bdc2ddd698b456d"] = "pistol_grip",
            ["55818af64bdc2d5b648b4570"] = "foregrip",
            ["55818a594bdc2db9688b456a"] = "stock",
            ["55818b224bdc2dde698b456f"] = "mount",
            ["55818ac54bdc2d5b648b456e"] = "iron_sight",
            ["55818ae44bdc2dde698b456c"] = "scope_magnified",
            ["55818ad54bdc2ddc698b4569"] = "scope_red_dot",
            ["55818add4bdc2d5b648b456f"] = "scope_magnified",
            ["55818acf4bdc2dde698b456b"] = "scope_red_dot",
            ["55818a104bdc2db9688b4569"] = "handguard_medium",
            ["555ef6e44bdc2de9068b457e"] = "barrel_medium",
            ["55818b084bdc2d5b648b4571"] = "flashlight_laser",
            ["55818b164bdc2ddc698b456c"] = "flashlight_laser",
            ["55818b014bdc2ddc698b456b"] = "ubgl",
            ["617f1ef5e8b54b0998387734"] = "ubgl",
            ["5448fe124bdc2da5018b4567"] = "flashlight_laser",
            ["550aa4cd4bdc2dd8348b456c"] = "muzzle_suppressor",
            ["550aa4bf4bdc2dd6348b456b"] = "muzzle_flashhider",
            ["550aa4dd4bdc2dc9348b4569"] = "muzzle_brake",
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> ModProfileRanges =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["muzzle_suppressor"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-15, -8, true),
                ["CameraRecoil"] = new(-8, -3, true),
                ["VerticalRecoil"] = new(-15, -8, true),
                ["HorizontalRecoil"] = new(-12, -6, true),
                ["Dispersion"] = new(-5, -1, true),
                ["Accuracy"] = new(-5, 5, true),
                ["Velocity"] = new(0, 3, true),
                ["Loudness"] = new(-40, -20, true),
                ["Flash"] = new(-80, -30, true),
                ["ModMalfunctionChance"] = new(10, 25, true),
                ["DurabilityBurnModificator"] = new(1.2, 1.5),
                ["AimSpeed"] = new(-20, -8, true),
            }),
            ["muzzle_suppressor_compact"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-6, -2, true),
                ["CameraRecoil"] = new(-3, -1, true),
                ["VerticalRecoil"] = new(-8, -3, true),
                ["HorizontalRecoil"] = new(-6, -1, true),
                ["Dispersion"] = new(-1, 1, true),
                ["Accuracy"] = new(-3, 5, true),
                ["Velocity"] = new(0, 2, true),
                ["Loudness"] = new(-20, 10, true),
                ["Flash"] = new(-45, -10, true),
                ["ModMalfunctionChance"] = new(4, 18, true),
                ["DurabilityBurnModificator"] = new(1.0, 1.75),
                ["AimSpeed"] = new(-10, -2, true),
            }),
            ["muzzle_flashhider"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-5, -2, true),
                ["CameraRecoil"] = new(-2, -1, true),
                ["VerticalRecoil"] = new(-5, -2, true),
                ["HorizontalRecoil"] = new(-3, -1, true),
                ["Dispersion"] = new(-2, 2, true),
                ["Loudness"] = new(0, 10, true),
                ["Flash"] = new(-70, -40, true),
                ["AimSpeed"] = new(0, 0, true),
            }),
            ["muzzle_brake"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-6, -3, true),
                ["CameraRecoil"] = new(-9, -3, true),
                ["VerticalRecoil"] = new(-20, -12, true),
                ["HorizontalRecoil"] = new(-18, -10, true),
                ["Dispersion"] = new(-5, -2, true),
                ["Accuracy"] = new(-2, -1, true),
                ["Velocity"] = new(0, 0, true),
                ["Loudness"] = new(10, 20, true),
                ["Flash"] = new(3, 15, true),
                ["AimSpeed"] = new(-2, 3, true),
            }),
            ["muzzle_thread"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-1, 1, true),
                ["CameraRecoil"] = new(0, 0, true),
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["Dispersion"] = new(0, 0, true),
                ["Accuracy"] = new(0, 0, true),
                ["Velocity"] = new(0, 0, true),
                ["Loudness"] = new(0, 0, true),
                ["Flash"] = new(0, 0, true),
                ["ModMalfunctionChance"] = new(0, 0, true),
                ["DurabilityBurnModificator"] = new(1.0, 1.05),
                ["AimSpeed"] = new(0, 0, true),
            }),
            ["muzzle_adapter"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-2, 0, true),
                ["Flash"] = new(-5, 5, true),
                ["DurabilityBurnModificator"] = new(1.0, 1.1),
            }),
            ["booster"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["AutoROF"] = new(1.0, 1.2),
                ["SemiROF"] = new(1.0, 1.2),
                ["ModMalfunctionChance"] = new(-15, -8, true),
            }),
            ["magazine_compact"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(0, 6, true),
                ["ReloadSpeed"] = new(0, 8, true),
                ["LoadUnloadModifier"] = new(5, 15, true),
                ["CheckTimeModifier"] = new(-3, 1, true),
                ["ModMalfunctionChance"] = new(-3, 1, true),
                ["AimSpeed"] = new(0, 4, true),
                ["Handling"] = new(0, 5, true),
            }),
            ["magazine_standard"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-3, 1, true),
                ["ReloadSpeed"] = new(-4, 3, true),
                ["LoadUnloadModifier"] = new(3, 10, true),
                ["CheckTimeModifier"] = new(0, 4, true),
                ["ModMalfunctionChance"] = new(-1.5, 1.5),
                ["AimSpeed"] = new(-3, 1, true),
                ["Handling"] = new(-3, 1, true),
            }),
            ["magazine_extended"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-8, -3, true),
                ["ReloadSpeed"] = new(-10, -1, true),
                ["LoadUnloadModifier"] = new(6, 15, true),
                ["CheckTimeModifier"] = new(2, 8, true),
                ["ModMalfunctionChance"] = new(1, 2, true),
                ["AimSpeed"] = new(-6, -1, true),
                ["Handling"] = new(-6, -2, true),
            }),
            ["magazine_drum"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-15, -8, true),
                ["ReloadSpeed"] = new(-20, -5, true),
                ["LoadUnloadModifier"] = new(12, 25, true),
                ["CheckTimeModifier"] = new(6, 14, true),
                ["ModMalfunctionChance"] = new(3, 5, true),
                ["AimSpeed"] = new(-10, -3, true),
                ["Handling"] = new(-12, -4, true),
            }),
            ["magazine"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-3, 1, true),
                ["ReloadSpeed"] = new(-4, 3, true),
                ["LoadUnloadModifier"] = new(3, 10, true),
                ["CheckTimeModifier"] = new(0, 4, true),
                ["ModMalfunctionChance"] = new(-1.2, 1.2),
                ["AimSpeed"] = new(-3, 1, true),
                ["Handling"] = new(-3, 1, true),
            }),
            ["gasblock"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(0, 2, true),
                ["VerticalRecoil"] = new(-2, 0, true),
                ["HorizontalRecoil"] = new(-1, 0, true),
                ["ModMalfunctionChance"] = new(-2, 5, true),
                ["DurabilityBurnModificator"] = new(0.95, 1.05),
                ["HeatFactor"] = new(0.98, 1.03),
                ["CoolFactor"] = new(0.98, 1.02),
                ["Loudness"] = new(0, 10, true),
                ["Velocity"] = new(0.0, 2.0),
                ["Flash"] = new(-3, 3, true),
            }),
            ["scope_magnified"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["AimSpeed"] = new(-8, -4, true),
                ["Accuracy"] = new(3, 8, true),
                ["AimStability"] = new(5, 10, true),
                ["Ergonomics"] = new(-8, -5, true),
                ["Handling"] = new(-7, -2, true),
            }),
            ["scope_red_dot"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["AimSpeed"] = new(2, 5, true),
                ["Accuracy"] = new(-5, 5),
                ["AimStability"] = new(0, 3, true),
                ["Ergonomics"] = new(-2, 2, true),
            }),
            ["iron_sight"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["AimSpeed"] = new(0, 3, true),
                    ["Accuracy"] = new(-15, 0, true),
                ["Ergonomics"] = new(0, 2, true),
            }),
            ["stock_fixed"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-13, -8, true),
                ["HorizontalRecoil"] = new(-10, -6, true),
                ["CameraRecoil"] = new(-20, -12, true),
                ["Convergence"] = new(12, 20, true),
                ["AimSpeed"] = new(-10, -3, true),
                ["AimStability"] = new(10, 15, true),
                ["Handling"] = new(-10, -5, true),
                ["Ergonomics"] = new(8, 15, true),
            }),
            ["stock_folding"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-10, -5, true),
                ["HorizontalRecoil"] = new(-6, -2, true),
                ["CameraRecoil"] = new(-12, -5, true),
                ["Convergence"] = new(5, 12, true),
                ["AimSpeed"] = new(-6, -2, true),
                ["AimStability"] = new(5, 10, true),
                ["Handling"] = new(-3, 2, true),
                ["Ergonomics"] = new(4, 10, true),
            }),
            ["stock_ads_support"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-8, -4, true),
                ["HorizontalRecoil"] = new(-5, -2, true),
                ["CameraRecoil"] = new(-8, -4, true),
                ["Convergence"] = new(6, 14, true),
                ["AimSpeed"] = new(-4, 0, true),
                ["AimStability"] = new(8, 15, true),
                ["Handling"] = new(-2, 5, true),
                ["Ergonomics"] = new(3, 8, true),
            }),
            ["stock_buttpad"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-7, -2, true),
                ["HorizontalRecoil"] = new(-4, 0, true),
                ["CameraRecoil"] = new(-5, -1, true),
                ["Convergence"] = new(0, 0, true),
                ["AimSpeed"] = new(2, 8, true),
                ["AimStability"] = new(1, 8, true),
                ["Handling"] = new(2, 5, true),
                ["Ergonomics"] = new(4, 11, true),
            }),
            ["stock"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-12, -7, true),
                ["HorizontalRecoil"] = new(-8, -4, true),
                ["CameraRecoil"] = new(-8, -5, true),
                ["Convergence"] = new(5, 18, true),
                ["AimSpeed"] = new(-5, 5, true),
                ["AimStability"] = new(7, 12, true),
                ["Handling"] = new(-2, 6, true),
                ["Ergonomics"] = new(8, 15, true),
            }),
            ["buffer_adapter"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-2, 3, true),
                ["HorizontalRecoil"] = new(-1, 3, true),
                ["Ergonomics"] = new(-1, 5, true),
            }),
            ["stock_adapter"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["CameraRecoil"] = new(0, 0, true),
                ["Convergence"] = new(0, 0, true),
                ["AimSpeed"] = new(0, 0, true),
                ["AimStability"] = new(0, 0, true),
                ["Handling"] = new(0, 0, true),
                ["Ergonomics"] = new(0, 0, true),
                ["DurabilityBurnModificator"] = new(1, 1, false),
                ["Loudness"] = new(0, 0, true),
            }),
            ["pistol_grip"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-3, 1, true),
                ["HorizontalRecoil"] = new(-2, 2, true),
                ["Dispersion"] = new(-3, 3, true),
                ["Ergonomics"] = new(2, 7, true),
                ["Accuracy"] = new(0, 0, true),
                ["AimSpeed"] = new(1, 5, true),
                ["AimStability"] = new(1, 5, true),
                ["Handling"] = new(2, 8, true),
            }),
            ["ubgl"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["Dispersion"] = new(0, 0, true),
                ["CameraRecoil"] = new(0, 0, true),
                ["AimSpeed"] = new(0, 0, true),
                ["Ergonomics"] = new(0, 0, true),
                ["Accuracy"] = new(-15, 0, true),
                ["AutoROF"] = new(0, 0, true),
                ["SemiROF"] = new(0, 0, true),
                ["ModMalfunctionChance"] = new(0, 0, true),
                ["HeatFactor"] = new(1, 1, true),
                ["CoolFactor"] = new(1, 1, true),
            }),
            ["bayonet"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-4, -3, true),
                ["HorizontalRecoil"] = new(-2, -2, true),
                ["Dispersion"] = new(-1, 0, true),
                ["CameraRecoil"] = new(-1, -1, true),
                ["AutoROF"] = new(0, 0, true),
                ["SemiROF"] = new(0, 0, true),
                ["ModMalfunctionChance"] = new(0, 0, true),
                ["Accuracy"] = new(-15, -12, true),
                ["HeatFactor"] = new(1, 1, true),
                ["CoolFactor"] = new(1, 1, true),
                ["DurabilityBurnModificator"] = new(1, 1, false),
                ["Velocity"] = new(0, 0, true),
                ["RecoilAngle"] = new(0, 0, true),
                ["Ergonomics"] = new(-5, -3, true),
                ["Convergence"] = new(0, 0, true),
                ["MeleeDamage"] = new(65, 112, true),
                ["MeleePen"] = new(18, 40, true),
                ["Flash"] = new(-56, -49, true),
                ["AimSpeed"] = new(0, 0, true),
                ["Loudness"] = new(1, 8, true),
            }),
            ["foregrip"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-8, -3, true),
                ["HorizontalRecoil"] = new(-5, -2, true),
                ["CameraRecoil"] = new(-7, -1, true),
                ["Dispersion"] = new(0, 0, true),
                ["Convergence"] = new(-5, 2, true),
                ["AimSpeed"] = new(3, 10, true),
                ["AimStability"] = new(5, 12, true),
                ["Handling"] = new(8, 18, true),
                ["Ergonomics"] = new(6, 16, true),
                ["Accuracy"] = new(0, 0, true),
            }),
            ["receiver"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["AutoROF"] = new(0, 2, true),
                ["SemiROF"] = new(0, 5, true),
                ["ModMalfunctionChance"] = new(-5, 5, true),
                ["Accuracy"] = new(-5, 5, true),
                ["ChamberSpeed"] = new(0, 40, true),
                ["HeatFactor"] = new(0.95, 1.05),
                ["CoolFactor"] = new(0.95, 1.05),
                ["Ergonomics"] = new(0, 7, true),
                ["Convergence"] = new(0, 10, true),
            }),
            ["mount"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["Dispersion"] = new(0, 0, true),
                ["Ergonomics"] = new(-1, 1, true),
                ["AimStability"] = new(0, 0, true),
                ["Handling"] = new(0, 0, true),
                ["Accuracy"] = new(0, 0, true),
                ["HeatFactor"] = new(0.95, 1.03),
                ["CoolFactor"] = new(0.92, 1.06),
                ["AimSpeed"] = new(-5, 3, true),
                ["DurabilityBurnModificator"] = new(1, 1, false),
            }),
            ["flashlight_laser"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["Ergonomics"] = new(-2, 0, true),
                ["Handling"] = new(-4, -2, true),
            }),
            ["catch"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(0, 0, true),
                ["HorizontalRecoil"] = new(0, 0, true),
                ["AutoROF"] = new(0, 0, true),
                ["SemiROF"] = new(0, 0, true),
                ["AimSpeed"] = new(0, 0, true),
                ["ReloadSpeed"] = new(0, 5, true),
                ["ChamberSpeed"] = new(2.5, 8.5),
                ["Ergonomics"] = new(0, 0, true),
                ["Accuracy"] = new(0, 1, true),
                ["FixSpeed"] = new(0, 0, true),
                ["HeatFactor"] = new(1, 1, true),
                ["CoolFactor"] = new(1, 1, true),
                ["DurabilityBurnModificator"] = new(1, 1),
                ["LoyaltyLevel"] = new(1, 3, true),
                ["ModMalfunctionChance"] = new(-1, 1, true),
            }),
            ["hammer"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ModMalfunctionChance"] = new(-2, 2, true),
                ["SemiROF"] = new(0, 7.5),
                ["Accuracy"] = new(0, 15, true),
                ["Ergonomics"] = new(-1, 2, true),
            }),
            ["trigger"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["ModMalfunctionChance"] = new(-1, 2, true),
                ["SemiROF"] = new(0, 5, true),
                ["Accuracy"] = new(0, 15, true),
                ["Ergonomics"] = new(-1, 2, true),
            }),
            ["charging_handle"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-1, 0, true),
                ["HorizontalRecoil"] = new(-1, 0, true),
                ["ChamberSpeed"] = new(-5, 40, true),
                ["ModMalfunctionChance"] = new(-1, 4, true),
                ["Ergonomics"] = new(-1, 1, true),
                ["ReloadSpeed"] = new(0, 0, true),
                ["FixSpeed"] = new(0, 0, true),
            }),
            ["bipod"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-3, 0, true),
                ["HorizontalRecoil"] = new(-2, 0, true),
                ["Accuracy"] = new(0, 3, true),
                ["AimStability"] = new(0, 3, true),
                ["Ergonomics"] = new(-3, -1, true),
                ["Handling"] = new(-5, -2, true),
            }),
            ["stock_rear_hook"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(2, 2, true),
            }),
            ["optic_eyecup"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(-1, 1, true),
            }),
            ["optic_killflash"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(1, 1, true),
            }),
            ["rail_panel"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ergonomics"] = new(1, 2, true),
                ["CoolFactor"] = new(1.02, 1.02),
            }),
            ["barrel_short"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-2, 2, true),
                ["HorizontalRecoil"] = new(0, 2, true),
                ["Dispersion"] = new(5, 12, true),
                ["CenterOfImpact"] = new(0.05, 0.15),
                ["Velocity"] = new(-6, -2, true),
                ["Accuracy"] = new(-20, -5, true),
                ["HeatFactor"] = new(1.1, 1.3),
                ["CoolFactor"] = new(0.9, 0.95),
                ["Convergence"] = new(10, 25, true),
                ["DurabilityBurnModificator"] = new(1.1, 1.3),
                ["RecoilAngle"] = new(5, 15, true),
                ["ShotgunDispersion"] = new(0.8, 2),
            }),
            ["barrel_medium"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-4, -1, true),
                ["HorizontalRecoil"] = new(-3, 1, true),
                ["Dispersion"] = new(-1, 7, true),
                ["CenterOfImpact"] = new(0.02, 0.05),
                ["Velocity"] = new(-1, 3, true),
                ["Accuracy"] = new(-2, 5, true),
                ["HeatFactor"] = new(1, 1.15),
                ["CoolFactor"] = new(0.95, 1.05),
                ["Convergence"] = new(0, 10, true),
                ["DurabilityBurnModificator"] = new(0.95, 1.05),
                ["RecoilAngle"] = new(-5, 5, true),
                ["ShotgunDispersion"] = new(0.8, 2),
            }),
            ["barrel_integral_suppressed"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-14, -8, true),
                ["HorizontalRecoil"] = new(-8, -5, true),
                ["Dispersion"] = new(-5, 0, true),
                ["Velocity"] = new(-3, 3, true),
                ["Accuracy"] = new(-5, 5, true),
                ["HeatFactor"] = new(1.05, 1.25),
                ["CoolFactor"] = new(1.0, 1.08),
                ["DurabilityBurnModificator"] = new(1.15, 1.35),
                ["ShotgunDispersion"] = new(0.8, 2),
            }),
            ["barrel_long"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-6, -3, true),
                ["HorizontalRecoil"] = new(-5, -3, true),
                ["Dispersion"] = new(-8, -2, true),
                ["CenterOfImpact"] = new(0.005, 0.02),
                ["Velocity"] = new(4, 15, true),
                ["Accuracy"] = new(10, 15, true),
                ["HeatFactor"] = new(0.9, 1.1),
                ["CoolFactor"] = new(1, 1.15),
                ["Convergence"] = new(-15, -5, true),
                ["DurabilityBurnModificator"] = new(0.7, 0.9),
                ["RecoilAngle"] = new(-15, -5, true),
                ["ShotgunDispersion"] = new(0.8, 2),
            }),
            ["handguard_short"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-3, -1, true),
                ["HorizontalRecoil"] = new(-2, -1, true),
                ["HeatFactor"] = new(1.05, 1.1),
                ["CoolFactor"] = new(0.8, 0.95),
                ["AimStability"] = new(1, 5, true),
                ["AimSpeed"] = new(2, 8, true),
                ["Handling"] = new(5, 12, true),
                ["Ergonomics"] = new(4, 10, true),
                ["Accuracy"] = new(-8.0, 3.0, false),
                ["Dispersion"] = new(-4.0, -1.0, false),
                ["DurabilityBurnModificator"] = new(1.0, 1.0),
            }),
            ["handguard_medium"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-7, -4, true),
                ["HorizontalRecoil"] = new(-6, -2, true),
                ["HeatFactor"] = new(0.95, 1.05),
                ["CoolFactor"] = new(0.9, 1.0),
                ["AimStability"] = new(5, 10, true),
                ["AimSpeed"] = new(0, 5, true),
                ["Handling"] = new(2, 8, true),
                ["Ergonomics"] = new(1, 6, true),
                ["Accuracy"] = new(-2.0, 7.0, false),
                ["Dispersion"] = new(-3.0, 1.0, false),
                ["DurabilityBurnModificator"] = new(1.0, 1.0),
            }),
            ["handguard_long"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-10, -6, true),
                ["HorizontalRecoil"] = new(-7, -3, true),
                ["HeatFactor"] = new(0.88, 0.95),
                ["CoolFactor"] = new(0.85, 1.05),
                ["AimStability"] = new(10, 20, true),
                ["AimSpeed"] = new(-8, -2, true),
                ["Handling"] = new(-8, -2, true),
                ["Ergonomics"] = new(-5, 2, true),
                ["Accuracy"] = new(-2.0, 10.0, false),
                ["Dispersion"] = new(-3, -2, false),
                ["DurabilityBurnModificator"] = new(0.9, 1.0),
            }),
        });

    private static IReadOnlyDictionary<string, NumericRange> CreateRanges(Dictionary<string, NumericRange> ranges)
    {
        return new ReadOnlyDictionary<string, NumericRange>(ranges);
    }

    public static AttachmentRules CreateDefaultRules()
    {
        return new AttachmentRules
        {
            ModClampRules = ModClampRules,
            ModParentBaseProfiles = ModParentBaseProfiles,
            ModProfileRanges = ModProfileRanges,
        };
    }
}