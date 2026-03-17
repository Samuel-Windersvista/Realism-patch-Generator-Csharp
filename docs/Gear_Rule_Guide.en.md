# Gear Rule Guide v1.1

This document covers the current C# gear rule pipeline. The source of truth is rules/gear_rules.json, and the document is aligned with the current behavior of RealismPatchGenerator.Core/GearRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The current gear rule file contains two core data groups:

- gearClampRules: final safety clamp ranges
- gearProfileRanges: field ranges for each gear profile

## 2. Global Clamp Ranges

The current gearClampRules are:

- ReloadSpeedMulti: 0.85 to 1.25
- Comfort: 0.6 to 1.4
- speedPenaltyPercent: -40 to 10

## 3. Current Gear Profiles

gearProfileRanges currently covers:

- armor_vest_light
- armor_vest_heavy
- armor_chest_rig_light
- armor_chest_rig_heavy
- chest_rig_light
- chest_rig_heavy
- helmet_light
- helmet_heavy
- armor_component_accessory
- armor_component_faceshield
- armor_mask_decorative
- armor_mask_ballistic
- armor_plate_hard
- armor_plate_helmet
- armor_plate_soft
- backpack_compact
- backpack_full
- back_panel
- belt_harness
- headset
- cosmetic_headwear
- protective_eyewear_standard
- protective_eyewear_ballistic
- cosmetic_gasmask

## 4. Common Fields

These profiles mainly cover:

- SpallReduction
- ReloadSpeedMulti
- Comfort
- speedPenaltyPercent
- weaponErgonomicPenalty
- dB
- GasProtection
- RadProtection

## 5. Representative Current Ranges

Some representative current profiles:

- armor_vest_light: SpallReduction 0.15 to 0.55, Comfort 0.9 to 1.08, speedPenaltyPercent -4.5 to 0
- armor_vest_heavy: SpallReduction 0.55 to 0.92, Comfort 1 to 1.14, speedPenaltyPercent -8 to -0.8
- armor_plate_soft: SpallReduction 0.1 to 0.45
- armor_plate_hard: SpallReduction 0.18 to 0.85
- backpack_compact: Comfort 0.9 to 1.18, speedPenaltyPercent -2.8 to -0.6
- backpack_full: Comfort 0.74 to 0.96, speedPenaltyPercent -4.8 to -2
- headset: dB 19 to 26
- cosmetic_gasmask: GasProtection 0.75 to 0.96, RadProtection 0.5 to 0.92, weaponErgonomicPenalty -20 to -2

## 6. Current Rule Characteristics

- Gear rules focus on functional fields and penalty fields rather than economic values
- Fields such as IsGasMask, GasProtection, RadProtection, and MaskToUse should now be judged together with real output results and gear rules
- Ordinary cosmetic items are still outside the main rule-audit scope, but entries with gas-mask or radiation-protection semantics can still resolve to cosmetic_gasmask

## 7. Tuning Recommendations

- If you want to change the overall feel of a gear family, edit gearProfileRanges first
- If you only want to pull in extreme values, edit gearClampRules first
- For a small number of structurally unusual items, use item_exceptions for fine adjustments rather than forcing category-wide workarounds

## 8. Audit Notes

- consumable and ordinary cosmetic items are not in the main audit scope
- Fields explicitly overridden in item_exceptions are exempted per field
- This document describes structure and representative ranges only; the full source of truth is rules/gear_rules.json

## 9. Common Field Explanations

- SpallReduction: resistance to spall or secondary fragment damage. Lower values usually mean stronger suppression of secondary fragment impact, while a value near 1 means little additional intervention.
- ReloadSpeedMulti: reload speed multiplier. Values above 1 usually mean faster reloads; values below 1 mean reload speed is hindered.
- Comfort: wearing comfort or load friendliness. Higher values usually mean lighter burden and better sustained usability.
- speedPenaltyPercent: movement speed penalty percentage. More negative values usually mean a larger mobility loss.
- weaponErgonomicPenalty: ergonomic penalty applied to weapon handling. More negative values usually mean worse ADS, handling, and readying behavior.
- dB: sound gain or pickup strength for headset items, used to describe how strongly environmental sound is amplified.
- GasProtection: gas protection capability. Higher values usually mean better performance in toxic environments.
- RadProtection: radiation protection capability. Higher values usually mean better protection against radiation.
- mousePenalty: mouse handling penalty. Current gear rules do not actively recompute it, but it usually represents the negative effect on control sensitivity after equipping the item.

## 10. Documentation Strategy

This guide describes the current gear rule structure, covered fields, and profile resolution logic. For exact numeric ranges, use rules/gear_rules.json as the source of truth. For field semantics, cross-check this guide with the weapon, attachment, and ammo rule guides when needed.
