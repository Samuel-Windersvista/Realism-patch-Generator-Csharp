using System.Collections.ObjectModel;

namespace RealismPatchGenerator.Core;

internal static class AmmoRuleData
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoProfileRanges =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["rifle_545x39"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(840, 930, true), ["BulletMassGram"] = new(3.2, 4.2), ["Damage"] = new(38, 58, true), ["PenetrationPower"] = new(36, 82, true),
                ["ammoRec"] = new(-8, 8, true), ["ammoAccr"] = new(-2, 12, true), ["ArmorDamage"] = new(1.08, 1.16), ["HeatFactor"] = new(0.95, 1.15),
                ["HeavyBleedingDelta"] = new(0.00, 0.12), ["LightBleedingDelta"] = new(0.04, 0.20), ["DurabilityBurnModificator"] = new(0.90, 1.15), ["BallisticCoeficient"] = new(0.16, 0.28),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["rifle_556x45"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(860, 980, true), ["BulletMassGram"] = new(3.6, 4.8), ["Damage"] = new(40, 62, true), ["PenetrationPower"] = new(40, 88, true),
                ["ammoRec"] = new(-6, 10, true), ["ammoAccr"] = new(-4, 10, true), ["ArmorDamage"] = new(1.09, 1.17), ["HeatFactor"] = new(0.96, 1.18),
                ["HeavyBleedingDelta"] = new(0.00, 0.14), ["LightBleedingDelta"] = new(0.04, 0.22), ["DurabilityBurnModificator"] = new(0.92, 1.18), ["BallisticCoeficient"] = new(0.18, 0.31),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["rifle_762x39"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(680, 760, true), ["BulletMassGram"] = new(7.5, 8.5), ["Damage"] = new(52, 74, true), ["PenetrationPower"] = new(28, 72, true),
                ["ammoRec"] = new(4, 18, true), ["ammoAccr"] = new(-8, 6, true), ["ArmorDamage"] = new(1.07, 1.15), ["HeatFactor"] = new(1.00, 1.20),
                ["HeavyBleedingDelta"] = new(0.02, 0.20), ["LightBleedingDelta"] = new(0.06, 0.28), ["DurabilityBurnModificator"] = new(0.95, 1.25), ["BallisticCoeficient"] = new(0.24, 0.36),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["rifle_762x51"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(790, 900, true), ["BulletMassGram"] = new(9.3, 11.8), ["Damage"] = new(58, 82, true), ["PenetrationPower"] = new(52, 108, true),
                ["ammoRec"] = new(10, 26, true), ["ammoAccr"] = new(-10, 4, true), ["ArmorDamage"] = new(1.12, 1.20), ["HeatFactor"] = new(1.05, 1.32),
                ["HeavyBleedingDelta"] = new(0.02, 0.28), ["LightBleedingDelta"] = new(0.08, 0.34), ["DurabilityBurnModificator"] = new(1.05, 1.40), ["BallisticCoeficient"] = new(0.30, 0.46),
                ["MalfMisfireChance"] = new(0.001, 0.015), ["MisfireChance"] = new(0.001, 0.015), ["MalfFeedChance"] = new(0.001, 0.015),
            }),
            ["rifle_9x39"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(270, 330, true), ["BulletMassGram"] = new(15.0, 18.5), ["Damage"] = new(60, 78, true), ["PenetrationPower"] = new(45, 85, true),
                ["ammoRec"] = new(6, 15, true), ["ammoAccr"] = new(-10, -5, true), ["ArmorDamage"] = new(1.0, 1.15), ["HeatFactor"] = new(1.05, 1.30),
                ["HeavyBleedingDelta"] = new(0.02, 0.30), ["LightBleedingDelta"] = new(0.08, 0.34), ["DurabilityBurnModificator"] = new(1.10, 1.45), ["BallisticCoeficient"] = new(0.34, 0.54),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["rifle_300blk"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(520, 760, true), ["BulletMassGram"] = new(7.0, 13.0), ["Damage"] = new(50, 72, true), ["PenetrationPower"] = new(24, 70, true),
                ["ammoRec"] = new(2, 20, true), ["ammoAccr"] = new(-10, 6, true), ["ArmorDamage"] = new(1.06, 1.15), ["HeatFactor"] = new(1.00, 1.22),
                ["HeavyBleedingDelta"] = new(0.01, 0.22), ["LightBleedingDelta"] = new(0.06, 0.30), ["DurabilityBurnModificator"] = new(0.98, 1.30), ["BallisticCoeficient"] = new(0.24, 0.48),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["pistol_compact"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(280, 430, true), ["BulletMassGram"] = new(4.0, 10.0), ["Damage"] = new(44, 78, true), ["PenetrationPower"] = new(8, 36, true),
                ["ammoRec"] = new(-8, 10, true), ["ammoAccr"] = new(-6, 8, true), ["ArmorDamage"] = new(1.00, 1.08), ["HeatFactor"] = new(0.85, 1.05),
                ["HeavyBleedingDelta"] = new(0.0, 0.18), ["LightBleedingDelta"] = new(0.08, 0.40), ["DurabilityBurnModificator"] = new(0.75, 1.00), ["BallisticCoeficient"] = new(0.09, 0.20),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["pdw_small_high_velocity"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(560, 780, true), ["BulletMassGram"] = new(1.8, 3.8), ["Damage"] = new(36, 65, true), ["PenetrationPower"] = new(18, 54, true),
                ["ammoRec"] = new(-14, 2, true), ["ammoAccr"] = new(0, 14, true), ["ArmorDamage"] = new(1.04, 1.12), ["HeatFactor"] = new(0.95, 1.15),
                ["HeavyBleedingDelta"] = new(0.0, 0.10), ["LightBleedingDelta"] = new(0.04, 0.22), ["DurabilityBurnModificator"] = new(0.85, 1.15), ["BallisticCoeficient"] = new(0.11, 0.22),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["intermediate_rifle"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(650, 940, true), ["BulletMassGram"] = new(3.2, 11.0), ["Damage"] = new(58, 74, true), ["PenetrationPower"] = new(58, 89, true),
                ["ammoRec"] = new(-4, 10, true), ["ammoAccr"] = new(-3, 5, true), ["ArmorDamage"] = new(1.06, 1.14), ["HeatFactor"] = new(0.95, 1.20),
                ["HeavyBleedingDelta"] = new(0.0, 0.20), ["LightBleedingDelta"] = new(0.06, 0.28), ["DurabilityBurnModificator"] = new(0.90, 1.20), ["BallisticCoeficient"] = new(0.13, 0.32),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["full_power_rifle"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(720, 980, true), ["BulletMassGram"] = new(8.0, 17.5), ["Damage"] = new(55, 90, true), ["PenetrationPower"] = new(48, 104, true),
                ["ammoRec"] = new(6, 28, true), ["ammoAccr"] = new(-8, 8, true), ["ArmorDamage"] = new(1.10, 1.20), ["HeatFactor"] = new(1.00, 1.30),
                ["HeavyBleedingDelta"] = new(0.02, 0.30), ["LightBleedingDelta"] = new(0.08, 0.36), ["DurabilityBurnModificator"] = new(1.00, 1.35), ["BallisticCoeficient"] = new(0.20, 0.42),
                ["MalfMisfireChance"] = new(0.001, 0.015), ["MisfireChance"] = new(0.001, 0.015), ["MalfFeedChance"] = new(0.001, 0.015),
            }),
            ["magnum_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(820, 1020, true), ["BulletMassGram"] = new(12.0, 28.0), ["Damage"] = new(70, 115, true), ["PenetrationPower"] = new(58, 118, true),
                ["ammoRec"] = new(16, 44, true), ["ammoAccr"] = new(-10, 6, true), ["ArmorDamage"] = new(1.12, 1.20), ["HeatFactor"] = new(1.10, 1.45),
                ["HeavyBleedingDelta"] = new(0.08, 0.40), ["LightBleedingDelta"] = new(0.10, 0.42), ["DurabilityBurnModificator"] = new(1.15, 1.60), ["BallisticCoeficient"] = new(0.28, 0.58),
                ["MalfMisfireChance"] = new(0.001, 0.015), ["MisfireChance"] = new(0.001, 0.015), ["MalfFeedChance"] = new(0.001, 0.015),
            }),
            ["shotgun_shell"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(320, 520, true), ["BulletMassGram"] = new(22.0, 46.0), ["Damage"] = new(110, 235, true), ["PenetrationPower"] = new(1, 30, true),
                ["ammoRec"] = new(20, 52, true), ["ammoAccr"] = new(-24, -5, true), ["ArmorDamage"] = new(1.00, 1.10), ["HeatFactor"] = new(0.95, 1.25),
                ["HeavyBleedingDelta"] = new(0.08, 0.55), ["LightBleedingDelta"] = new(0.18, 0.60), ["DurabilityBurnModificator"] = new(1.00, 1.35), ["BallisticCoeficient"] = new(0.03, 0.12),
                ["MalfMisfireChance"] = new(0.001, 0.008), ["MisfireChance"] = new(0.001, 0.008), ["MalfFeedChance"] = new(0.001, 0.008),
            }),
            ["anti_materiel_50bmg"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(840, 930, true), ["BulletMassGram"] = new(38.0, 52.0), ["Damage"] = new(150, 220, true), ["PenetrationPower"] = new(80, 130, true),
                ["ammoRec"] = new(35, 75, true), ["ammoAccr"] = new(-14, 5, true), ["ArmorDamage"] = new(1.14, 1.20), ["HeatFactor"] = new(1.20, 1.70),
                ["HeavyBleedingDelta"] = new(0.25, 0.80), ["LightBleedingDelta"] = new(0.30, 0.90), ["DurabilityBurnModificator"] = new(1.40, 2.10), ["BallisticCoeficient"] = new(0.55, 0.95),
                ["MalfMisfireChance"] = new(0.001, 0.015), ["MisfireChance"] = new(0.001, 0.015), ["MalfFeedChance"] = new(0.001, 0.015),
            }),
        });

    public static readonly IReadOnlyList<(string Profile, string[] Keywords)> AmmoProfileKeywords =
    [
        ("rifle_762x51", ["7.62x51", "762x51", "7.62 nato", "762 nato", "7.62x63", "762x63", "308", ".308"]),
        ("rifle_762x39", ["7.62x39", "762x39"]),
        ("rifle_556x45", ["5.56x45", "556x45", "5.56 nato", "556 nato", ".223", "223 rem", "caliber556x45"]),
        ("rifle_545x39", ["5.45x39", "545x39", "caliber545x39"]),
        ("rifle_9x39", ["9x39"]),
        ("rifle_300blk", [".300 aac", "300 blackout", ".300 blk"]),
        ("anti_materiel_50bmg", [".50 bmg", "50bmg", "12.7x99", "12,7x99"]),
        ("magnum_heavy", [".338", "338", ".300 wm", "300 wm", "300 win mag", "300 winchester magnum", "8.6 blackout", "86 blackout", "12.7x55", "7.62x54", "7.62x54r"]),
        ("shotgun_shell", ["12x70", "12x76", "12 gauge", "20 gauge", "12/70", "12/76", "20/70", "23x75"]),
        ("full_power_rifle", ["7.62x51", "7.62 nato", "7.62x63", "7.62x67", "7.92x57", "792x57", "8mm mauser", "6.8", "6x38", "6 mm arc", "6mm arc", "6.5 creedmoor", "6.5x48", "6.5 creed"]),
        ("intermediate_rifle", ["5.8x42", "58x42", "caliber58x42", "6.5 grendel"]),
        ("pdw_small_high_velocity", ["4.6x30", "5.7x28"]),
        ("pistol_compact", ["9x18", "9x19", "9x21", ".45 acp", "10x25", "357", "40 s&w"]),
    ];

    public static readonly IReadOnlyList<(string Profile, string[] Keywords)> AmmoSpecialKeywords =
    [
        ("ap_extreme", ["m993", "m61", "7n39", "7n37", "7n40", "7n42", "snb", "slaap", "m995", "995"]),
        ("tracer", ["tracer", "m62", "m856", "856", "m856a1", "856a1", "t46m", "bt"]),
        ("ap_high", ["ap", "api", "bs", "bp", "pbm", "pp", "upz", "cbj", "ss190", "l191", "m855a1", "855a1"]),
        ("subsonic_heavy", ["subsonic", "us", "pab9", "sb193", "sp6", "spp"]),
        ("expanding", ["hp", "jhp", "jsp", "sp", "rip", "hydra", "hydra_shok", "vmax", "r37f", "r37x", "piranha", "warmageddon"]),
        ("shot_shell_payload", ["buckshot", "flechette", "slug"]),
        ("ball_standard", ["fmj", "fmj43", "gzh", "pst", "pso", "prs", "ps", "ppo", "psv", "akbs", "lrn", "lrnpc"]),
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoSpecialModifiers =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ap_extreme"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["PenetrationPower"] = new(8, 16, true), ["Damage"] = new(-8, -2, true), ["ArmorDamage"] = new(0.02, 0.04), ["HeatFactor"] = new(0.08, 0.20),
                ["DurabilityBurnModificator"] = new(0.12, 0.28), ["ammoRec"] = new(4, 12, true), ["ammoAccr"] = new(-8, -2, true),
                ["MalfMisfireChance"] = new(0.001, 0.004), ["MisfireChance"] = new(0.001, 0.004), ["MalfFeedChance"] = new(0.001, 0.004),
            }),
            ["tracer"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(12, 36, true), ["PenetrationPower"] = new(0, 6, true), ["Damage"] = new(-2, 4, true), ["HeatFactor"] = new(0.03, 0.10),
                ["DurabilityBurnModificator"] = new(0.03, 0.10), ["ammoRec"] = new(1, 6, true), ["ammoAccr"] = new(-3, 2, true), ["LightBleedingDelta"] = new(0.00, 0.04),
            }),
            ["ap_high"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["PenetrationPower"] = new(4, 10, true), ["Damage"] = new(-4, 2, true), ["ArmorDamage"] = new(0.01, 0.03), ["HeatFactor"] = new(0.04, 0.14),
                ["DurabilityBurnModificator"] = new(0.06, 0.20), ["ammoRec"] = new(2, 8, true), ["ammoAccr"] = new(-5, -1, true),
                ["MalfMisfireChance"] = new(0.0005, 0.003), ["MisfireChance"] = new(0.0005, 0.003), ["MalfFeedChance"] = new(0.0005, 0.003),
            }),
            ["subsonic_heavy"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(-120, -35, true), ["BulletMassGram"] = new(1.5, 4.0), ["PenetrationPower"] = new(-4, 3, true), ["Damage"] = new(4, 12, true),
                ["BallisticCoeficient"] = new(0.02, 0.08), ["ammoRec"] = new(0, 8, true), ["ammoAccr"] = new(-3, 3, true),
            }),
            ["expanding"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["PenetrationPower"] = new(-10, -3, true), ["Damage"] = new(8, 20, true), ["ArmorDamage"] = new(-0.015, 0.005), ["HeatFactor"] = new(-0.04, 0.04),
                ["HeavyBleedingDelta"] = new(0.05, 0.15), ["LightBleedingDelta"] = new(0.05, 0.15), ["ammoRec"] = new(-4, 2, true), ["ammoAccr"] = new(0, 6, true),
            }),
            ["shot_shell_payload"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(-30, 20, true), ["BulletMassGram"] = new(2.0, 8.0), ["PenetrationPower"] = new(-8, 2, true), ["Damage"] = new(12, 30, true),
                ["HeavyBleedingDelta"] = new(0.08, 0.24), ["LightBleedingDelta"] = new(0.08, 0.20), ["ammoAccr"] = new(-6, 0, true),
            }),
            ["ball_standard"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["PenetrationPower"] = new(-2, 4, true), ["Damage"] = new(-2, 8, true), ["ArmorDamage"] = new(-0.005, 0.01), ["HeatFactor"] = new(-0.02, 0.06),
                ["DurabilityBurnModificator"] = new(-0.03, 0.10), ["ammoRec"] = new(-2, 4, true), ["ammoAccr"] = new(-1, 4, true),
            }),
        });

    public static readonly IReadOnlyDictionary<string, NumericRange> AmmoPenetrationTiers =
        new ReadOnlyDictionary<string, NumericRange>(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
        {
            ["pen_lvl_1"] = new(1, 10, true), ["pen_lvl_2"] = new(11, 20, true), ["pen_lvl_3"] = new(21, 30, true), ["pen_lvl_4"] = new(31, 40, true),
            ["pen_lvl_5"] = new(41, 50, true), ["pen_lvl_6"] = new(51, 60, true), ["pen_lvl_7"] = new(61, 70, true), ["pen_lvl_8"] = new(71, 80, true),
            ["pen_lvl_9"] = new(81, 90, true), ["pen_lvl_10"] = new(91, 100, true), ["pen_lvl_11"] = new(101, 130, true),
        });

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>> AmmoPenetrationModifiers =
        new ReadOnlyDictionary<string, IReadOnlyDictionary<string, NumericRange>>(new Dictionary<string, IReadOnlyDictionary<string, NumericRange>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pen_lvl_1"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(-40, -5, true), ["BulletMassGram"] = new(0.5, 2.5), ["Damage"] = new(14, 34, true), ["PenetrationPower"] = new(-9, -4, true),
                ["ammoRec"] = new(-12, -2, true), ["ammoAccr"] = new(4, 14, true), ["ArmorDamage"] = new(-0.02, -0.005), ["HeatFactor"] = new(-0.12, -0.03),
                ["HeavyBleedingDelta"] = new(0.08, 0.24), ["LightBleedingDelta"] = new(0.08, 0.20), ["DurabilityBurnModificator"] = new(-0.20, -0.06), ["BallisticCoeficient"] = new(-0.03, 0.02),
                ["MalfMisfireChance"] = new(-0.001, 0.000), ["MisfireChance"] = new(-0.001, 0.000), ["MalfFeedChance"] = new(-0.001, 0.000),
            }),
            ["pen_lvl_2"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(-18, 8, true), ["BulletMassGram"] = new(0.0, 1.5), ["Damage"] = new(10, 24, true), ["PenetrationPower"] = new(-7, -2, true),
                ["ammoRec"] = new(-8, 2, true), ["ammoAccr"] = new(0, 8, true), ["ArmorDamage"] = new(-0.015, -0.003), ["HeatFactor"] = new(-0.08, 0.02),
                ["HeavyBleedingDelta"] = new(0.04, 0.14), ["LightBleedingDelta"] = new(0.04, 0.12), ["DurabilityBurnModificator"] = new(-0.12, 0.00), ["BallisticCoeficient"] = new(-0.02, 0.03),
                ["MalfMisfireChance"] = new(-0.001, 0.000), ["MisfireChance"] = new(-0.001, 0.000), ["MalfFeedChance"] = new(-0.001, 0.000),
            }),
            ["pen_lvl_3"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(-6, 10, true), ["BulletMassGram"] = new(-0.5, 1.0), ["Damage"] = new(4, 16, true), ["PenetrationPower"] = new(-5, 0, true),
                ["ammoRec"] = new(-2, 5, true), ["ammoAccr"] = new(-4, 4, true), ["ArmorDamage"] = new(-0.005, 0.01), ["HeatFactor"] = new(-0.03, 0.05),
                ["HeavyBleedingDelta"] = new(-0.01, 0.05), ["LightBleedingDelta"] = new(-0.01, 0.05), ["DurabilityBurnModificator"] = new(-0.05, 0.08), ["BallisticCoeficient"] = new(-0.01, 0.04),
                ["MalfMisfireChance"] = new(-0.0005, 0.001), ["MisfireChance"] = new(-0.0005, 0.001), ["MalfFeedChance"] = new(-0.0005, 0.001),
            }),
            ["pen_lvl_4"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(8, 28, true), ["BulletMassGram"] = new(-1.0, 0.5), ["Damage"] = new(-4, 8, true), ["PenetrationPower"] = new(-3, 2, true),
                ["ammoRec"] = new(4, 15, true), ["ammoAccr"] = new(-10, -2, true), ["ArmorDamage"] = new(0.005, 0.02), ["HeatFactor"] = new(0.02, 0.10),
                ["HeavyBleedingDelta"] = new(-0.05, 0.02), ["LightBleedingDelta"] = new(-0.05, 0.02), ["DurabilityBurnModificator"] = new(0.06, 0.20), ["BallisticCoeficient"] = new(0.01, 0.06),
                ["MalfMisfireChance"] = new(0.001, 0.004), ["MisfireChance"] = new(0.001, 0.004), ["MalfFeedChance"] = new(0.001, 0.004),
            }),
            ["pen_lvl_5"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(14, 36, true), ["BulletMassGram"] = new(-1.5, 0.5), ["Damage"] = new(-6, 4, true), ["PenetrationPower"] = new(-1, 3, true),
                ["ammoRec"] = new(8, 22, true), ["ammoAccr"] = new(-14, -4, true), ["ArmorDamage"] = new(0.00, 0.015), ["HeatFactor"] = new(0.08, 0.18),
                ["HeavyBleedingDelta"] = new(-0.08, 0.00), ["LightBleedingDelta"] = new(-0.08, 0.00), ["DurabilityBurnModificator"] = new(0.12, 0.30), ["BallisticCoeficient"] = new(0.02, 0.07),
                ["MalfMisfireChance"] = new(0.002, 0.006), ["MisfireChance"] = new(0.002, 0.006), ["MalfFeedChance"] = new(0.002, 0.006),
            }),
            ["pen_lvl_6"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(16, 38, true), ["BulletMassGram"] = new(-1.8, 0.3), ["Damage"] = new(-10, 0, true), ["PenetrationPower"] = new(1, 5, true),
                ["ammoRec"] = new(10, 24, true), ["ammoAccr"] = new(-16, -5, true), ["ArmorDamage"] = new(0.005, 0.02), ["HeatFactor"] = new(0.08, 0.18),
                ["HeavyBleedingDelta"] = new(-0.08, -0.01), ["LightBleedingDelta"] = new(-0.08, -0.01), ["DurabilityBurnModificator"] = new(0.12, 0.30), ["BallisticCoeficient"] = new(0.02, 0.07),
                ["MalfMisfireChance"] = new(0.002, 0.006), ["MisfireChance"] = new(0.002, 0.006), ["MalfFeedChance"] = new(0.002, 0.006),
            }),
            ["pen_lvl_7"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(18, 40, true), ["BulletMassGram"] = new(-2.0, 0.2), ["Damage"] = new(-14, -3, true), ["PenetrationPower"] = new(3, 8, true),
                ["ammoRec"] = new(12, 28, true), ["ammoAccr"] = new(-18, -6, true), ["ArmorDamage"] = new(0.01, 0.025), ["HeatFactor"] = new(0.10, 0.20),
                ["HeavyBleedingDelta"] = new(-0.10, -0.02), ["LightBleedingDelta"] = new(-0.10, -0.02), ["DurabilityBurnModificator"] = new(0.16, 0.34), ["BallisticCoeficient"] = new(0.03, 0.08),
                ["MalfMisfireChance"] = new(0.003, 0.007), ["MisfireChance"] = new(0.003, 0.007), ["MalfFeedChance"] = new(0.003, 0.007),
            }),
            ["pen_lvl_8"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(20, 42, true), ["BulletMassGram"] = new(-2.0, 0.1), ["Damage"] = new(-18, -6, true), ["PenetrationPower"] = new(6, 11, true),
                ["ammoRec"] = new(14, 32, true), ["ammoAccr"] = new(-19, -7, true), ["ArmorDamage"] = new(0.015, 0.03), ["HeatFactor"] = new(0.10, 0.22),
                ["HeavyBleedingDelta"] = new(-0.11, -0.03), ["LightBleedingDelta"] = new(-0.11, -0.03), ["DurabilityBurnModificator"] = new(0.18, 0.38), ["BallisticCoeficient"] = new(0.03, 0.09),
                ["MalfMisfireChance"] = new(0.003, 0.008), ["MisfireChance"] = new(0.003, 0.008), ["MalfFeedChance"] = new(0.003, 0.008),
            }),
            ["pen_lvl_9"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(22, 44, true), ["BulletMassGram"] = new(-2.1, 0.0), ["Damage"] = new(-24, -10, true), ["PenetrationPower"] = new(9, 15, true),
                ["ammoRec"] = new(15, 34, true), ["ammoAccr"] = new(-20, -8, true), ["ArmorDamage"] = new(0.02, 0.035), ["HeatFactor"] = new(0.11, 0.23),
                ["HeavyBleedingDelta"] = new(-0.12, -0.04), ["LightBleedingDelta"] = new(-0.12, -0.04), ["DurabilityBurnModificator"] = new(0.20, 0.40), ["BallisticCoeficient"] = new(0.03, 0.10),
                ["MalfMisfireChance"] = new(0.004, 0.009), ["MisfireChance"] = new(0.004, 0.009), ["MalfFeedChance"] = new(0.004, 0.009),
            }),
            ["pen_lvl_10"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(20, 45, true), ["BulletMassGram"] = new(-2.0, 0.0), ["Damage"] = new(-30, -12, true), ["PenetrationPower"] = new(12, 20, true),
                ["ammoRec"] = new(15, 35, true), ["ammoAccr"] = new(-20, -8, true), ["ArmorDamage"] = new(0.025, 0.04), ["HeatFactor"] = new(0.10, 0.22),
                ["HeavyBleedingDelta"] = new(-0.10, -0.02), ["LightBleedingDelta"] = new(-0.10, -0.02), ["DurabilityBurnModificator"] = new(0.20, 0.40), ["BallisticCoeficient"] = new(0.03, 0.10),
                ["MalfMisfireChance"] = new(0.005, 0.010), ["MisfireChance"] = new(0.005, 0.010), ["MalfFeedChance"] = new(0.005, 0.010),
            }),
            ["pen_lvl_11"] = CreateRanges(new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase)
            {
                ["InitialSpeed"] = new(22, 48, true), ["BulletMassGram"] = new(-2.2, 0.0), ["Damage"] = new(-36, -16, true), ["PenetrationPower"] = new(14, 24, true),
                ["ammoRec"] = new(18, 40, true), ["ammoAccr"] = new(-24, -10, true), ["ArmorDamage"] = new(0.03, 0.045), ["HeatFactor"] = new(0.12, 0.24),
                ["HeavyBleedingDelta"] = new(-0.12, -0.04), ["LightBleedingDelta"] = new(-0.12, -0.04), ["DurabilityBurnModificator"] = new(0.24, 0.45), ["BallisticCoeficient"] = new(0.04, 0.11),
                ["MalfMisfireChance"] = new(0.006, 0.012), ["MisfireChance"] = new(0.006, 0.012), ["MalfFeedChance"] = new(0.006, 0.012),
            }),
        });

    private static IReadOnlyDictionary<string, NumericRange> CreateRanges(Dictionary<string, NumericRange> ranges)
    {
        return new ReadOnlyDictionary<string, NumericRange>(ranges);
    }

    public static AmmoRules CreateDefaultRules()
    {
        return new AmmoRules
        {
            AmmoProfileRanges = AmmoProfileRanges,
            AmmoProfileKeywords = AmmoProfileKeywords
                .Select(entry => new KeywordProfile(entry.Profile, entry.Keywords))
                .ToArray(),
            AmmoSpecialKeywords = AmmoSpecialKeywords
                .Select(entry => new KeywordProfile(entry.Profile, entry.Keywords))
                .ToArray(),
            AmmoSpecialModifiers = AmmoSpecialModifiers,
            AmmoPenetrationTiers = AmmoPenetrationTiers,
            AmmoPenetrationModifiers = AmmoPenetrationModifiers,
        };
    }
}