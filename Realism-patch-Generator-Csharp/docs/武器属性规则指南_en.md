# Weapon Rule Guide (v2.0)

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


Major controlled fields:


Representative examples:


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

- bullpup now keeps only a mild recoil advantage and still pays the reload-speed tradeoff
- stockless setups now explicitly increase recoil, visual kick, and instability instead of receiving hidden recoil discounts

## 8. Practical Tuning Recommendations

- for broad per-class behavior, tune weaponProfileRanges
- for caliber-level handling feel, tune weaponCaliberRuleModifiers
- for stock/bullpup/stockless structure differences, tune weaponStockRuleModifiers
- after edits, generate a small output sample and run audit before full generation
