# Ammo Rule Guide (v2.0)

This document describes the current C# ammo rule pipeline. Source of truth: RealismItemRules/ammo_rules.json, aligned with RealismPatchGenerator.Core/AmmoRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The ammo rule file has 3 core groups:

- ammoProfileRanges: caliber base ranges
- ammoSpecialModifiers: special ammo-type modifiers
- ammoPenetrationModifiers: penetration-tier modifiers

## 2. Current Base Caliber Profiles

ammoProfileRanges currently includes:

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

These profiles primarily control:

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

Examples:

- rifle_545x39: InitialSpeed 840 to 930, Damage 38 to 58, PenetrationPower 36 to 82
- rifle_556x45: InitialSpeed 860 to 980, Damage 40 to 62, PenetrationPower 40 to 88
- rifle_762x39: InitialSpeed 680 to 760, Damage 52 to 74, PenetrationPower 28 to 72
- rifle_762x51: InitialSpeed 790 to 900, Damage 58 to 82, PenetrationPower 52 to 108
- rifle_9x39: InitialSpeed 270 to 330, Damage 60 to 78, PenetrationPower 45 to 85
- pistol_compact: InitialSpeed 280 to 430, Damage 44 to 78, PenetrationPower 8 to 36
- shotgun_shell_12g: InitialSpeed 320 to 520, Damage 125 to 235, BulletMassGram 26 to 46
- shotgun_shell_20g: InitialSpeed 330 to 530, Damage 95 to 185, BulletMassGram 18 to 32
- shotgun_shell_23x75: InitialSpeed 270 to 420, Damage 140 to 270, BulletMassGram 38 to 62

## 4. Special Ammo Modifiers

Current ammoSpecialModifiers include:

- ap_extreme
- tracer
- ap_high
- subsonic_heavy
- expanding
- shot_shell_payload
- ball_standard

These differentiate AP, tracer, expansion, subsonic, and shotgun payload behavior within the same caliber.

## 5. Penetration Tier Modifiers

Current ammoPenetrationModifiers include:

- pen_lvl_1 to pen_lvl_11

This layer spreads differences in damage, penetration, heat, durability burn, and malfunction risk across penetration tiers.

## 6. Current Rule Characteristics

- higher penetration tiers usually come with higher HeatFactor, ArmorDamage, and malfunction risk
- expanding rounds emphasize flesh damage and bleeding
- subsonic_heavy emphasizes lower velocity, heavier projectile, and distinct recoil behavior
- shotgun base profiles are split into 12g/20g/23x75
- shot_shell_payload further differentiates buckshot, flechette, slug, etc.

## 7. Tuning Recommendations

- adjust ammoProfileRanges for broad caliber behavior
- adjust ammoSpecialModifiers for AP/subsonic/expanding style differences in the same caliber
- adjust ammoPenetrationModifiers to widen or narrow penetration-tier spacing

## 8. Audit Notes

- ammo is in primary audit scope
- fields explicitly overridden by item_exceptions are exempted at field level
- this document provides structure and representative ranges; exact values are in RealismItemRules/ammo_rules.json
