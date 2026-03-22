using System.Collections.ObjectModel;

namespace RealismPatchGenerator.Core;

internal readonly record struct NumericRange(double Min, double Max, bool PreferInt = false);

internal static class WeaponRuleData
{
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> WeaponParentGroups =
        new ReadOnlyDictionary<string, IReadOnlySet<string>>(new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["assault"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447b5f14bdc2d61278b4567",
                "5447b5fc4bdc2d87278b4567",
            },
            ["pistol"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447b5cf4bdc2d65278b4567",
            },
            ["smg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447b5e04bdc2d62278b4567",
            },
            ["sniper"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447b6194bdc2d67278b4567",
                "5447b6254bdc2dc3278b4568",
            },
            ["shotgun"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447b6094bdc2dc3278b4567",
            },
            ["machinegun"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447bed64bdc2d97278b4568",
            },
            ["launcher"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "5447bedf4bdc2d87278b4568",
            },
        });

    public static readonly IReadOnlyDictionary<string, NumericRange> GunClampRules =
        new ReadOnlyDictionary<string, NumericRange>(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ergonomics"] = new(10, 100, true),
            ["VerticalRecoil"] = new(10, 700, true),
            ["HorizontalRecoil"] = new(20, 700, true),
            ["Convergence"] = new(1, 40, true),
            ["LoyaltyLevel"] = new(1, 5, true),
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponProfileRanges =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["assault"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(80, 110, true),
                ["HorizontalRecoil"] = new(140, 185, true),
                ["Convergence"] = new(2, 25, true),
                ["Dispersion"] = new(4, 8, true),
                ["VisualMulti"] = new(1.05, 1.25),
                ["Ergonomics"] = new(85, 95, true),
                ["RecoilIntensity"] = new(0.12, 0.22),
            }),
            ["pistol"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(325, 525, true),
                ["HorizontalRecoil"] = new(250, 380, true),
                ["Convergence"] = new(12, 18, true),
                ["Dispersion"] = new(10, 18, true),
                ["VisualMulti"] = new(2.0, 2.6),
                ["Ergonomics"] = new(92, 100, true),
                ["BaseTorque"] = new(-2.0, -1.0),
            }),
            ["smg"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(37, 64, true),
                ["HorizontalRecoil"] = new(70, 120, true),
                ["Convergence"] = new(16, 22, true),
                ["Dispersion"] = new(6, 12, true),
                ["VisualMulti"] = new(0.85, 1.15),
                ["Ergonomics"] = new(88, 98, true),
                ["RecoilIntensity"] = new(0.08, 0.16),
            }),
            ["sniper"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(115, 185, true),
                ["HorizontalRecoil"] = new(150, 300, true),
                ["Convergence"] = new(8, 13, true),
                ["Dispersion"] = new(0.5, 3.0),
                ["VisualMulti"] = new(1.1, 1.8),
                ["Ergonomics"] = new(68, 83, true),
            }),
            ["shotgun"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(245, 425, true),
                ["HorizontalRecoil"] = new(240, 460, true),
                ["Dispersion"] = new(15, 30, true),
                ["VisualMulti"] = new(1.8, 2.3),
                ["Ergonomics"] = new(68, 88, true),
                ["RecoilIntensity"] = new(0.32, 0.52),
                ["ShotgunDispersion"] = new(1, 1, true),
            }),
            ["machinegun"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(150, 245, true),
                ["HorizontalRecoil"] = new(200, 360, true),
                ["Convergence"] = new(6, 14, true),
                ["Dispersion"] = new(6, 14, true),
                ["VisualMulti"] = new(1.3, 1.7),
                ["Ergonomics"] = new(70, 90, true),
                ["RecoilIntensity"] = new(0.3, 0.5),
            }),
            ["launcher"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(185, 365, true),
                ["HorizontalRecoil"] = new(240, 500, true),
                ["Convergence"] = new(2, 10, true),
                ["Dispersion"] = new(8, 18, true),
                ["VisualMulti"] = new(1.6, 2.6),
                ["Ergonomics"] = new(45, 68, true),
                ["RecoilIntensity"] = new(0.28, 0.5),
            }),
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponCaliberRuleModifiers =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pistol_caliber"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(10, 25, true),
                ["HorizontalRecoil"] = new(12, 55, true),
                ["Convergence"] = new(1, 4, true),
                ["Velocity"] = new(-3, 2, true),
                ["RecoilIntensity"] = new(0.01, 0.06),
            }),
            ["small_high_velocity"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-5, 5, true),
                ["HorizontalRecoil"] = new(-5, 5, true),
                ["Convergence"] = new(0, 3, true),
                ["Velocity"] = new(0, 3, true),
                ["RecoilIntensity"] = new(-0.03, 0.02),
            }),
            ["intermediate_rifle_58x42"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(12, 12, true),
                ["HorizontalRecoil"] = new(18, 18, true),
                ["Convergence"] = new(-1, 3, true),
                ["Velocity"] = new(3, 3, true),
                ["RecoilIntensity"] = new(-0.01, 0.04),
            }),
            ["intermediate_rifle_762x39"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(8, 45, true),
                ["HorizontalRecoil"] = new(4, 32, true),
                ["Convergence"] = new(-1, 2, true),
                ["Velocity"] = new(2, 6, true),
                ["RecoilIntensity"] = new(0.01, 0.06),
            }),
            ["subsonic_heavy_9x39"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-45, -60, true),
                ["HorizontalRecoil"] = new(-20, -35, true),
                ["Convergence"] = new(-4, 2, true),
                ["Velocity"] = new(-8, -6, true),
                ["RecoilIntensity"] = new(0.03, 0.09),
            }),
            ["full_power_rifle"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(20, 90, true),
                ["HorizontalRecoil"] = new(14, 65, true),
                ["Convergence"] = new(-3, 1, true),
                ["Velocity"] = new(5, 15, true),
                ["RecoilIntensity"] = new(0.02, 0.1),
            }),
            ["full_power_rifle_rimmed"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(30, 105, true),
                ["HorizontalRecoil"] = new(18, 75, true),
                ["Convergence"] = new(-4, 0, true),
                ["Velocity"] = new(7, 15, true),
                ["RecoilIntensity"] = new(0.03, 0.11),
            }),
            ["magnum_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(80, 180, true),
                ["HorizontalRecoil"] = new(50, 130, true),
                ["Convergence"] = new(-5, -1, true),
                ["Velocity"] = new(10, 25, true),
                ["RecoilIntensity"] = new(0.08, 0.2),
            }),
            ["shotgun_shell_12g"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(55, 120, true),
                ["HorizontalRecoil"] = new(25, 85, true),
                ["ShotgunDispersion"] = new(1, 3, true),
                ["Convergence"] = new(-2, 2, true),
                ["RecoilIntensity"] = new(0.07, 0.16),
            }),
            ["shotgun_shell_20g"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(20, 70, true),
                ["HorizontalRecoil"] = new(10, 55, true),
                ["ShotgunDispersion"] = new(0, 2, true),
                ["Convergence"] = new(0, 4, true),
                ["RecoilIntensity"] = new(0.02, 0.10),
            }),
            ["shotgun_shell_23x75"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(95, 180, true),
                ["HorizontalRecoil"] = new(40, 115, true),
                ["ShotgunDispersion"] = new(2, 5, true),
                ["Convergence"] = new(-4, 0, true),
                ["RecoilIntensity"] = new(0.12, 0.28),
            }),
            ["pdw_high_pen_small"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-15, 10, true),
                ["HorizontalRecoil"] = new(-20, 10, true),
                ["Convergence"] = new(2, 6, true),
                ["Velocity"] = new(5, 12, true),
                ["RecoilIntensity"] = new(-0.05, 0.02),
            }),
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> WeaponStockRuleModifiers =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["fixed_stock"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-5, 5, true),
                ["HorizontalRecoil"] = new(-10, -3, true),
                ["Convergence"] = new(2, 6, true),
                ["CameraRecoil"] = new(-0.02, -0.006),
                ["VisualMulti"] = new(-0.08, -0.02),
                ["Ergonomics"] = new(-4, 2, true),
                ["BaseReloadSpeedMulti"] = new(0.98, 1.05),
            }),
            ["folding_stock_extended"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(2, 12, true),
                ["HorizontalRecoil"] = new(-5, 3, true),
                ["Convergence"] = new(0, 3, true),
                ["CameraRecoil"] = new(-0.01, 0.004),
                ["VisualMulti"] = new(-0.03, 0.04),
                ["Ergonomics"] = new(0, 5, true),
                ["BaseReloadSpeedMulti"] = new(0.98, 1.03),
            }),
            ["folding_stock_collapsed"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(20, 65, true),
                ["HorizontalRecoil"] = new(8, 45, true),
                ["Convergence"] = new(-6, -2, true),
                ["CameraRecoil"] = new(0.01, 0.05),
                ["Ergonomics"] = new(3, 12, true),
                ["VisualMulti"] = new(0.08, 0.25),
            }),
            ["bullpup"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-12, 2, true),
                ["HorizontalRecoil"] = new(-14, -5, true),
                ["Convergence"] = new(3, 8, true),
                ["CameraRecoil"] = new(0.004, 0.02),
                ["VisualMulti"] = new(0.03, 0.12),
                ["Ergonomics"] = new(-6, 2, true),
                ["BaseReloadSpeedMulti"] = new(0.84, 0.95),
                ["BaseChamberCheckSpeed"] = new(0.9, 1.05),
            }),
            ["stockless"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["VerticalRecoil"] = new(-10, 4, true),
                ["HorizontalRecoil"] = new(-12, -4, true),
                ["Convergence"] = new(1, 6, true),
                ["CameraRecoil"] = new(0.02, 0.07),
                ["VisualMulti"] = new(0.15, 0.55),
                ["RecoilIntensity"] = new(0.02, 0.1),
            }),
        });

    public static readonly IReadOnlyDictionary<string, string> TemplateFileToWeaponProfile =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AssaultRifleTemplates.json"] = "assault",
            ["AssaultCarbineTemplates.json"] = "assault",
            ["PistolTemplates.json"] = "pistol",
            ["SMGTemplates.json"] = "smg",
            ["MarksmanRifleTemplates.json"] = "sniper",
            ["SniperRifleTemplates.json"] = "sniper",
            ["ShotgunTemplates.json"] = "shotgun",
            ["MachinegunTemplates.json"] = "machinegun",
            ["GrenadeLauncherTemplates.json"] = "launcher",
            ["SpecialWeaponTemplates.json"] = "assault",
        });

    public static readonly IReadOnlyList<(string Profile, string[] Keywords)> CaliberProfileKeywords =
    [
        ("magnum_heavy", [".338", "338lm", "338 lapua", ".300 wm", "300wm", "300 win mag", "12.7x", "50 bmg"]),
        ("shotgun_shell_12g", ["12g", "12 ga", "12ga", "12 gauge", "12x70", "12x76"]),
        ("shotgun_shell_20g", ["20g", "20 ga", "20ga", "20 gauge", "20x70"]),
        ("shotgun_shell_23x75", ["23x75", "ks23"]),
        ("pdw_high_pen_small", ["4.6x30", "5.7x28"]),
        ("full_power_rifle_rimmed", ["7.62x54", "54r", "7.62x54r"]),
        ("full_power_rifle", ["7.62x51", "308", ".308", "6.8"]),
        ("intermediate_rifle_762x39", ["7.62x39"]),
        ("intermediate_rifle_58x42", ["5.8x42", "58x42", "caliber58x42", "5.8x42mm"]),
        ("subsonic_heavy_9x39", ["9x39"]),
        ("small_high_velocity", ["5.45x39", "5.56x45", "5.56", "223", ".223"]),
        ("pistol_caliber", ["9x19", "9mm", ".45", "45acp", "10mm"]),
    ];

    private static IReadOnlyDictionary<string, NumericRange> CreateRanges(Dictionary<string, NumericRange> ranges)
    {
        return new ReadOnlyDictionary<string, NumericRange>(ranges);
    }

    public static WeaponRules CreateDefaultRules()
    {
        return new WeaponRules
        {
            WeaponParentGroups = WeaponParentGroups,
            GunClampRules = GunClampRules,
            WeaponProfileRanges = WeaponProfileRanges,
            WeaponCaliberRuleModifiers = WeaponCaliberRuleModifiers,
            WeaponStockRuleModifiers = WeaponStockRuleModifiers,
            TemplateFileToWeaponProfile = TemplateFileToWeaponProfile,
            CaliberProfileKeywords = CaliberProfileKeywords
                .Select(entry => new KeywordProfile(entry.Profile, entry.Keywords))
                .ToArray(),
        };
    }
}