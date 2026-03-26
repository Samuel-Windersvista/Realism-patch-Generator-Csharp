# Gear Rule Guide (v2.0)

This document describes the current C# gear rule pipeline. Source of truth: RealismItemRules/gear_rules.json, aligned with RealismPatchGenerator.Core/GearRuleData.cs and RealismPatchGenerator.Core/RealismPatchGenerator.cs.

## 1. Rule File Structure

The gear rule file has 3 core groups:

- gearClampRules: final safety clamp ranges
- gearProfileRanges: field ranges per gear profile
- gearPriceRanges: price ranges per gear profile

## 2. Global Clamp Ranges

Current gearClampRules:

- ReloadSpeedMulti: 0.85 to 1.25
- Comfort: 0.6 to 1.4
- speedPenaltyPercent: -40 to 10
- Price: 500 to 150000

## 3. Pricing Rules

gearPriceRanges covers the same profile set as gearProfileRanges and controls final output Price.

- Price no longer defaults to source Price or HandbookPrice
- generator identifies profile first, then computes position in range using final performance fields
- pricing combines armor class, spall behavior, comfort, mobility penalties, capacity, headset gain, gas/radiation protection, and related signals

Representative ranges:

- armor_vest_light: 12000 to 25000
- armor_vest_heavy: 22000 to 35000
- armor_chest_rig_heavy: 26000 to 38000
- helmet_light: 6000 to 15000
- helmet_heavy: 18000 to 30000
- backpack_compact: 12000 to 18000
- backpack_full: 18000 to 26000
- cosmetic_gasmask: 5000 to 12000

## 4. Current Gear Profiles

gearProfileRanges currently includes:

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

## 5. Common Fields

These profiles mainly control:

- SpallReduction
- ReloadSpeedMulti
- Comfort
- speedPenaltyPercent
- weaponErgonomicPenalty
- dB
- GasProtection
- RadProtection
- Price

## 6. Representative Profile Behaviors

Examples:

- armor_vest_light: SpallReduction 0.15 to 0.55, Comfort 0.9 to 1.08, speedPenaltyPercent -4.5 to 0
- armor_vest_heavy: SpallReduction 0.55 to 0.92, Comfort 1 to 1.14, speedPenaltyPercent -8 to -0.8
- armor_plate_soft: SpallReduction 0.1 to 0.45
- armor_plate_hard: SpallReduction 0.18 to 0.85
- backpack_compact: Comfort 0.9 to 1.18, speedPenaltyPercent -2.8 to -0.6
- backpack_full: Comfort 0.74 to 0.96, speedPenaltyPercent -4.8 to -2
- headset: dB 19 to 26
- cosmetic_gasmask: GasProtection 0.75 to 0.96, RadProtection 0.5 to 0.92, weaponErgonomicPenalty -20 to -2

## 7. Current Characteristics

- gear rules emphasize functional and penalty fields
- Price is now inside the gear-specific rule system
- fields like IsGasMask, GasProtection, RadProtection, MaskToUse should be evaluated with both generated output and gear rules
- regular cosmetic is still outside primary audit scope; cosmetic items with gas/radiation semantics map to cosmetic_gasmask

## 8. Tuning Recommendations

- tune gearProfileRanges for broad behavior
- tune gearClampRules to remove outliers
- tune gearPriceRanges for class-level pricing
- use item_exceptions only for a small number of structural special cases

## 9. Audit Notes

- consumable and regular cosmetic are outside primary audit scope
- fields explicitly covered in item_exceptions are exempted at field level
- this document describes structure and representative ranges; exact values are in RealismItemRules/gear_rules.json

## 10. Common Field Semantics

- SpallReduction: anti-fragmentation/spall mitigation
- ReloadSpeedMulti: reload speed multiplier
- Comfort: wearing comfort/load friendliness
- speedPenaltyPercent: movement speed penalty
- weaponErgonomicPenalty: ergonomic penalty applied to weapon handling
- dB: headset amplification/sound profile indicator
- GasProtection: gas protection level
- RadProtection: radiation protection level
- mousePenalty: control sensitivity penalty (not actively recomputed by current gear rules)
- Price: final generated gear price inside profile price range
