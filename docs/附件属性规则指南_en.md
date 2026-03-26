# Attachment Rule Guide (v2.0)

This document describes the current C# attachment rule pipeline. Source of truth: RealismItemRules/attachment_rules.json, aligned with RealismPatchGenerator.Core/AttachmentRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The attachment rule file has 4 core groups:

- modClampRules: final safety clamp ranges
- modPriceRanges: profile-based price ranges
- modParentBaseProfiles: default profile mapping by parentId
- modProfileRanges: per-profile field ranges

## 2. Global Clamp Ranges

Current modClampRules:

- VerticalRecoil: -35 to 35
- HorizontalRecoil: -35 to 35
- Dispersion: -55 to 55
- Loudness: -45 to 50
- Accuracy: -15 to 15
- Price: 300 to 120000
- LoyaltyLevel: 1 to 4

All generated attachment values are clamped into these boundaries.

## 3. Price Ranges

modPriceRanges provides profile-specific price bands, for example:

- muzzle_suppressor: 18000 to 65000
- scope_magnified: 15000 to 90000
- scope_red_dot: 7000 to 35000
- magazine_drum: 18000 to 65000
- receiver: 10000 to 45000
- ubgl: 40000 to 120000
- handguard_short / medium / long: layered ranges from 5000 to 36000

The generator combines recoil, handling, suppression/flash behavior, loading behavior, barrel length, magazine capacity, and other signals to compute price position, then clamps into the profile price band.

## 4. Current Attachment Profiles

modProfileRanges currently includes:

- muzzle_suppressor, muzzle_suppressor_compact, muzzle_flashhider, muzzle_brake, muzzle_thread, muzzle_adapter
- magazine_compact, magazine_standard, magazine_extended, magazine_drum, magazine
- scope_magnified, scope_red_dot, iron_sight, optic_eyecup, optic_killflash
- stock_fixed, stock_folding, stock_ads_support, stock_buttpad, stock, buffer_adapter, stock_adapter, stock_rear_hook
- pistol_grip, foregrip, receiver, mount, catch, hammer, trigger, charging_handle
- barrel_short, barrel_medium, barrel_integral_suppressed, barrel_long, handguard_short, handguard_medium, handguard_long
- gasblock, flashlight_laser, bipod, rail_panel

## 5. Typical Fields

Common fields across attachment profiles:

- Ergonomics
- CameraRecoil
- VerticalRecoil
- HorizontalRecoil
- Dispersion
- Accuracy
- Velocity
- Loudness
- Flash
- ModMalfunctionChance
- DurabilityBurnModificator
- AimSpeed
- AimStability
- Handling
- ReloadSpeed
- LoadUnloadModifier
- CheckTimeModifier

## 6. Representative Profile Ranges

Examples:

- muzzle_suppressor: Loudness -40 to -20, Flash -80 to -30, AimSpeed -20 to -8, DurabilityBurnModificator 1.2 to 1.5
- muzzle_brake: VerticalRecoil -20 to -12, HorizontalRecoil -18 to -10, but Loudness rises to 10 to 20
- magazine_compact: Ergonomics 0 to 6, ReloadSpeed 0 to 8, Handling 0 to 5
- magazine_drum: Ergonomics -15 to -8, ReloadSpeed -20 to -5, Handling -12 to -4
- scope_red_dot / scope_magnified mainly trade off Ergonomics, Accuracy, AimSpeed, and AimStability
- barrel_long and handguard_long favor stability, velocity, and control; short variants favor agility

## 7. Tuning Recommendations

- for broad profile behavior, edit modProfileRanges first
- for reducing extreme outliers, edit modClampRules first
- for incorrect default profile mapping by parentId, check modParentBaseProfiles

## 8. Audit and Exceptions

- attachment entries with mod_profile_unresolved are excluded from primary noise accounting
- fields explicitly covered in item_exceptions are exempted at field level in audit
- item exceptions should handle a small number of special cases, not replace full-category attachment tuning
