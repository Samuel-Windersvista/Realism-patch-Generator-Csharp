# Ammo Rule Guide v1.30.0

This document covers the current C# ammo rule pipeline. The source of truth is RealismItemRules/ammo_rules.json, and the document is aligned with the current behavior of RealismPatchGenerator.Core/AmmoRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The current ammo rule file contains three core data groups:

- ammoProfileRanges: base caliber ranges
- ammoSpecialModifiers: special ammo-type modifiers
- ammoPenetrationModifiers: penetration-tier modifiers

## 2. Current Base Caliber Profiles

ammoProfileRanges currently covers:

- rifle_545x39
- rifle_556x45
- rifle_762x39
- rifle_762x51
- rifle_9x39
- rifle_300blk
- pistol_compact
- pdw_small_high_velocity
- intermediate_rifle
- full_power_rifle
- magnum_heavy
- shotgun_shell_12g
- shotgun_shell_20g
- shotgun_shell_23x75
- anti_materiel_50bmg

These base caliber profiles jointly control:

- InitialSpeed
- BulletMassGram
- Damage
- PenetrationPower
- ammoRec
- ammoAccr
- ArmorDamage
- HeatFactor
- HeavyBleedingDelta
- LightBleedingDelta
- DurabilityBurnModificator
- BallisticCoeficient
- MalfMisfireChance
- MisfireChance
- MalfFeedChance

## 3. Representative Base Ranges

Some representative base profiles at the current stage:

- rifle_545x39: InitialSpeed 840 to 930, Damage 38 to 58, PenetrationPower 36 to 82
- rifle_556x45: InitialSpeed 860 to 980, Damage 40 to 62, PenetrationPower 40 to 88
- rifle_762x39: InitialSpeed 680 to 760, Damage 52 to 74, PenetrationPower 28 to 72
- rifle_762x51: InitialSpeed 790 to 900, Damage 58 to 82, PenetrationPower 52 to 108
- rifle_9x39: InitialSpeed 270 to 330, Damage 60 to 78, PenetrationPower 45 to 85
- pistol_compact: InitialSpeed 280 to 430, Damage 44 to 78, PenetrationPower 8 to 36
- shotgun_shell_12g: InitialSpeed 320 to 520, Damage 125 to 235, BulletMassGram 26 to 46
- shotgun_shell_20g: InitialSpeed 330 to 530, Damage 95 to 185, BulletMassGram 18 to 32
- shotgun_shell_23x75: InitialSpeed 270 to 420, Damage 140 to 270, BulletMassGram 38 to 62

## 4. Special Ammo-Type Modifiers

ammoSpecialModifiers currently covers:

- ap_extreme
- tracer
- ap_high
- subsonic_heavy
- expanding
- shot_shell_payload
- ball_standard

This layer is used to separate AP, tracer, expanding, subsonic, and shotgun payload behavior within the same caliber family.

## 5. Penetration-Tier Modifiers

ammoPenetrationModifiers currently covers:

- pen_lvl_1
- pen_lvl_2
- pen_lvl_3
- pen_lvl_4
- pen_lvl_5
- pen_lvl_6
- pen_lvl_7
- pen_lvl_8
- pen_lvl_9
- pen_lvl_10
- pen_lvl_11

This layer separates damage, penetration, heat, durability burn, and malfunction risk between different penetration tiers inside the same caliber family.

## 6. Current Rule Characteristics

- High-penetration tiers usually come with higher HeatFactor, ArmorDamage, and malfunction risk
- expanding focuses more on flesh damage and bleed effects
- subsonic_heavy leans toward lower speed, heavier projectiles, and distinct recoil-related behavior
- Shotgun base profiles are now split into 12 gauge, 20 gauge, and 23x75, so caliber differences are absorbed by ammoProfileRanges first
- shot_shell_payload adds extra separation between buckshot, flechette, slug, and similar payload types inside the same shotgun caliber

## 7. Tuning Recommendations

- If you want to change the overall style of a caliber family, edit ammoProfileRanges first
- If you want to tune AP, subsonic, or expanding behavior within a caliber, edit ammoSpecialModifiers
- If you want to widen or narrow the differences between penetration levels, edit ammoPenetrationModifiers

## 8. Audit Notes

- Ammo is part of the main audit scope
- Fields explicitly overridden in item_exceptions are exempted per field
- This document lists structure and representative ranges only; the full source of truth is RealismItemRules/ammo_rules.json
