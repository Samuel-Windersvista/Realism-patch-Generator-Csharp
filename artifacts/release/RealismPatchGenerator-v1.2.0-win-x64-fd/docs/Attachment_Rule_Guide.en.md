# Attachment Rule Guide v1.1

This document covers the current C# attachment rule pipeline. The source of truth is rules/attachment_rules.json, and the document is aligned with the current behavior of RealismPatchGenerator.Core/AttachmentRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The current attachment rule file contains three core data groups:

- modClampRules: final safety clamp ranges
- modParentBaseProfiles: default profiles assigned by parentId
- modProfileRanges: field ranges for each attachment profile

## 2. Global Clamp Ranges

The current modClampRules are:

- VerticalRecoil: -35 to 35
- HorizontalRecoil: -35 to 35
- Dispersion: -55 to 55
- Loudness: -45 to 50
- Accuracy: -15 to 15
- LoyaltyLevel: 1 to 4

Any attachment profile result is ultimately forced back into this boundary layer.

## 3. Current Attachment Profiles

modProfileRanges currently covers these groups:

- Muzzle: muzzle_suppressor, muzzle_suppressor_compact, muzzle_flashhider, muzzle_brake, muzzle_thread, muzzle_adapter
- Magazine: magazine_compact, magazine_standard, magazine_extended, magazine_drum, magazine
- Sights and optics: scope_magnified, scope_red_dot, iron_sight, optic_eyecup, optic_killflash
- Stocks: stock_fixed, stock_folding, stock_ads_support, stock_buttpad, stock, buffer_adapter, stock_adapter, stock_rear_hook
- Grips and structure parts: pistol_grip, foregrip, receiver, mount, catch, hammer, trigger, charging_handle
- Barrels and handguards: barrel_short, barrel_medium, barrel_integral_suppressed, barrel_long, handguard_short, handguard_medium, handguard_long
- Other: gasblock, flashlight_laser, bipod, rail_panel

## 4. Common Fields

Attachment profiles commonly cover fields such as:

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

## 5. Representative Current Ranges

Some representative current profiles:

- muzzle_suppressor: Loudness -40 to -20, Flash -80 to -30, AimSpeed -20 to -8, DurabilityBurnModificator 1.2 to 1.5
- muzzle_brake: VerticalRecoil -20 to -12, HorizontalRecoil -18 to -10, but Loudness rises to 10 to 20
- magazine_compact: Ergonomics 0 to 6, ReloadSpeed 0 to 8, Handling 0 to 5
- magazine_drum: Ergonomics -15 to -8, ReloadSpeed -20 to -5, Handling -12 to -4
- scope_red_dot and scope_magnified mainly trade off between Ergonomics, Accuracy, AimSpeed, and AimStability
- barrel_long and handguard_long favor stability, velocity, and control, while shorter variants favor agility and lighter handling

## 6. Tuning Recommendations

- If you want to change the overall feel of one attachment family, edit the matching profile under modProfileRanges
- If you only want to pull in extreme values, adjust modClampRules first
- If a parentId keeps resolving to the wrong default profile, inspect modParentBaseProfiles

## 7. Audit and Exception Notes

- mod_profile_unresolved in attachments is currently excluded from the main audit noise
- Fields explicitly overridden in item_exceptions are exempted per field during audit
- Item exceptions are better for a small number of unusual attachments and should not replace category-level tuning in attachment_rules.json
