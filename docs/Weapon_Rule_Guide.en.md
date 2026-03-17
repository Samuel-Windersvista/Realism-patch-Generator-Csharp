# Weapon Rule Guide v1.1

This document covers the current C# weapon rule pipeline. The source of truth is rules/weapon_rules.json, and the document is aligned with the current behavior of RealismPatchGenerator.Core/WeaponRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The current weapon rule file contains five core data groups:

- weaponParentGroups: groups weapons into broad categories by parentId
- gunClampRules: final safety clamp ranges for weapons
- weaponProfileRanges: base weapon profile ranges
- weaponCaliberRuleModifiers: additive modifiers by caliber or ammo-family semantics
- weaponStockRuleModifiers: additive modifiers by stock or structural form

## 2. Base Weapon Groups

weaponParentGroups currently covers these main categories:

- assault
- pistol
- smg
- sniper
- shotgun
- machinegun
- launcher

This step determines which base profile a weapon enters first.

## 3. Global Clamp Ranges

The current gunClampRules are:

- Ergonomics: 10 to 100
- VerticalRecoil: 10 to 700
- HorizontalRecoil: 20 to 700
- Convergence: 1 to 40
- LoyaltyLevel: 1 to 5

This means that no matter how base profiles, caliber modifiers, and stock modifiers stack together, the final values are still pulled back into this safe range.

## 4. Base Profile Ranges

weaponProfileRanges currently covers:

- assault
- pistol
- smg
- sniper
- shotgun
- machinegun
- launcher

These base profiles mainly control:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- Dispersion
- VisualMulti
- Ergonomics
- RecoilIntensity

Some representative current ranges:

- assault: VerticalRecoil 80 to 110, HorizontalRecoil 140 to 185, Ergonomics 85 to 95
- pistol: VerticalRecoil 325 to 525, HorizontalRecoil 250 to 380, Ergonomics 92 to 100
- smg: VerticalRecoil 37 to 64, HorizontalRecoil 70 to 120, Ergonomics 88 to 98
- sniper: VerticalRecoil 115 to 185, HorizontalRecoil 150 to 300, Ergonomics 68 to 83
- shotgun: VerticalRecoil 245 to 425, HorizontalRecoil 240 to 460, ShotgunDispersion fixed at 1
- machinegun: VerticalRecoil 150 to 245, HorizontalRecoil 200 to 360, Ergonomics 70 to 90
- launcher: VerticalRecoil 185 to 365, HorizontalRecoil 240 to 500, Ergonomics 45 to 68

## 5. Caliber and Ammo-Family Modifiers

weaponCaliberRuleModifiers currently covers:

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

These modifiers mainly affect:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- Velocity
- RecoilIntensity
- ShotgunDispersion

Some visible current traits:

- full_power_rifle and full_power_rifle_rimmed noticeably increase recoil and velocity adjustments
- magnum_heavy adds a large extra VerticalRecoil adjustment range of 80 to 180
- subsonic_heavy_9x39 lowers Velocity and adds heavier recoil characteristics
- shotgun_shell_12g preserves the standard recoil and spread correction for mainstream 12-gauge shotguns
- shotgun_shell_20g leans toward lighter recoil and easier control
- shotgun_shell_23x75 significantly increases recoil and spread volatility for large-bore shotguns
- pdw_high_pen_small follows a small-caliber high-penetration route with higher Velocity and tighter convergence

## 6. Stock and Structural Modifiers

weaponStockRuleModifiers currently covers:

- fixed_stock
- folding_stock_extended
- folding_stock_collapsed
- bullpup
- stockless

This layer mainly affects:

- VerticalRecoil
- HorizontalRecoil
- Convergence
- CameraRecoil
- VisualMulti
- Ergonomics
- BaseReloadSpeedMulti
- BaseChamberCheckSpeed
- RecoilIntensity

Some representative current traits:

- fixed_stock is more stable and usually gives lower HorizontalRecoil and higher Convergence
- folding_stock_collapsed increases recoil and visual disturbance, but Ergonomics tends to improve
- bullpup reduces part of the recoil profile but usually sacrifices BaseReloadSpeedMulti
- stockless amplifies VisualMulti and CameraRecoil, emphasizing compactness at the cost of control

## 7. Documentation Mapping

If you want to modify current weapon rules, start with these entry points:

- rules/weapon_rules.json: source of truth for the rules
- docs/规则说明.md: mapping between GUI categories and rule files
- docs/使用说明.md: day-to-day workflow documentation

## 8. Tuning Recommendations

- If you want to change the overall style of a weapon class, edit weaponProfileRanges first
- If you want to tune the overall feel of a caliber family, edit weaponCaliberRuleModifiers
- If you want to tune bullpup, folding stock, or stockless structural differences, edit weaponStockRuleModifiers
- After changes, generate a small output sample first and then run the audit to confirm there is no broad out-of-range behavior
