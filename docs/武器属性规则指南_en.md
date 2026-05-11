# Weapon Rule Guide (v2.7)

This document describes the current C# weapon rule pipeline. Source of truth: RealismItemRules/weapon_rules.json, aligned with RealismPatchGenerator.Core/WeaponRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The weapon rule file contains 6 core groups:

- weaponParentGroups: major class mapping by parentId
- gunClampRules: final safety clamp ranges
- gunPriceRanges: base profile price ranges
- weaponProfileRanges: base weapon profile ranges
- weaponCaliberRuleModifiers: caliber/ammo semantic modifiers
- weaponStockRuleModifiers: stock/structure modifiers

## 2. Base Weapon Classes

Current weaponParentGroups include:

- assault
- pistol
- smg
- sniper
- shotgun
- machinegun
- launcher

This determines which base profile a weapon enters first.

## 3. Global Clamp Ranges

Current gunClampRules:

- Ergonomics: 10 to 100
- VerticalRecoil: 10 to 700
- HorizontalRecoil: 20 to 700
- Convergence: 1 to 40
- Price: 5000 to 250000
- LoyaltyLevel: 1 to 5

All stacked modifiers are clamped into these boundaries at the end.

## 4. Price Ranges

gunPriceRanges currently includes:

- assault: 35000 to 90000
- pistol: 12000 to 45000
- smg: 22000 to 65000
- sniper: 50000 to 150000
- shotgun: 25000 to 80000
- machinegun: 70000 to 180000
- launcher: 90000 to 250000

Generator logic identifies profile first, then combines ergonomics, recoil, dispersion, rate of fire, caliber tier, stock form, and weight signals to compute price position inside the profile range.

## 5. Base Profile Ranges

weaponProfileRanges currently includes:

- assault: VerticalRecoil 95 to 130, HorizontalRecoil 155 to 210, Convergence 2 to 25, Dispersion 4 to 8, VisualMulti 1.05 to 1.25, Ergonomics 85 to 95, RecoilIntensity 0.14 to 0.26
- pistol: VerticalRecoil 325 to 525, HorizontalRecoil 250 to 380, Convergence 12 to 18, Dispersion 10 to 18, VisualMulti 2.0 to 2.6, Ergonomics 92 to 100, BaseTorque -2 to -1
- smg: VerticalRecoil 55 to 82, HorizontalRecoil 95 to 145, Convergence 16 to 22, Dispersion 6 to 12, VisualMulti 0.85 to 1.15, Ergonomics 88 to 98, RecoilIntensity 0.10 to 0.19
- sniper: VerticalRecoil 115 to 185, HorizontalRecoil 150 to 300, Convergence 8 to 13, Dispersion 0.5 to 3, VisualMulti 1.1 to 1.8, Ergonomics 68 to 83
- shotgun: VerticalRecoil 245 to 425, HorizontalRecoil 240 to 460, Dispersion 15 to 30, VisualMulti 1.8 to 2.3, Ergonomics 68 to 88, RecoilIntensity 0.32 to 0.52, ShotgunDispersion fixed at 1
- machinegun: VerticalRecoil 150 to 245, HorizontalRecoil 200 to 360, Convergence 6 to 14, Dispersion 6 to 14, VisualMulti 1.3 to 1.7, Ergonomics 70 to 90, RecoilIntensity 0.3 to 0.5
- launcher: VerticalRecoil 185 to 365, HorizontalRecoil 240 to 500, Convergence 2 to 10, Dispersion 8 to 18, VisualMulti 1.6 to 2.6, Ergonomics 45 to 68, RecoilIntensity 0.28 to 0.5

Major controlled fields:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- Dispersion
- VisualMulti
- Ergonomics
- RecoilIntensity
- ShotgunDispersion (shotguns)
- BaseTorque (pistols)

Representative examples:

- assault emphasizes controllable rifle recoil with high ergonomics and moderate visual kick
- pistol keeps very high ergonomics but much higher recoil and visual kick than shoulder-fired classes
- smg favors low recoil and high ergonomics
- sniper prioritizes precision behavior over ergonomics
- shotgun keeps fixed shotgun dispersion plus heavy recoil
- machinegun trades handling for sustained-fire control
- launcher stays low-ergonomics with heavy recoil and broad visual kick

## 6. Caliber and Ammo-Type Modifiers

Current weaponCaliberRuleModifiers include:

- pistol_caliber
- small_high_velocity
- intermediate_rifle_58x42
- intermediate_rifle_762x39
- subsonic_heavy_9x39
- full_power_rifle
- full_power_rifle_rimmed
- magnum_heavy
- shotgun_shell_12g
- shotgun_shell_20g
- shotgun_shell_23x75
- pdw_high_pen_small

Main affected fields:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- Velocity
- RecoilIntensity
- ShotgunDispersion

Practical direction:

- full_power_rifle / full_power_rifle_rimmed significantly raise recoil and velocity
- magnum_heavy pushes VerticalRecoil into a much heavier bonus range
- subsonic_heavy_9x39 lowers Velocity and adds a heavier recoil signature
- shotgun_shell_12g keeps mainstream 12-gauge shotgun recoil/dispersion behavior
- shotgun_shell_20g trends lighter and easier to control
- shotgun_shell_23x75 significantly increases large-bore shotgun recoil and dispersion swing
- pdw_high_pen_small favors a small-caliber high-penetration route with tighter convergence and higher velocity

## 7. Stock and Structure Modifiers

Current weaponStockRuleModifiers include:

- fixed_stock
- folding_stock_extended
- folding_stock_collapsed
- bullpup
- stockless

Main affected fields:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- CameraRecoil
- VisualMulti
- Ergonomics
- BaseReloadSpeedMulti
- BaseChamberCheckSpeed
- RecoilIntensity

Practical direction after the latest recoil retune:

- fixed_stock stays the most planted option and usually gives lower horizontal recoil plus better convergence
- folding_stock_extended stays in the middle ground: more stable than collapsed/stockless setups, but still less planted than a fixed stock
- folding_stock_collapsed raises recoil and visual disturbance, but usually gains ergonomics in return
- bullpup now keeps only a mild recoil advantage and still pays the reload-speed tradeoff
- stockless setups now explicitly increase recoil, visual kick, and instability instead of receiving hidden recoil discounts

## 8. Current Document Mapping

If you are changing current weapon rules, read these first:

- RealismItemRules/weapon_rules.json: source of truth for rule data
- docs/规则说明_en.md: GUI category and rule-file mapping
- docs/使用说明_en.md: day-to-day workflow and generation usage

## 9. Practical Tuning Recommendations

- for broad per-class behavior, tune weaponProfileRanges
- for caliber-level handling feel, tune weaponCaliberRuleModifiers
- for stock/bullpup/stockless structure differences, tune weaponStockRuleModifiers
- after edits, generate a small output sample and run audit before full generation
