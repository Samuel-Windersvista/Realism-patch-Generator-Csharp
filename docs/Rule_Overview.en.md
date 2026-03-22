# SPT Realism Value Range Generator v1.30.0 Rule Overview

This document explains the responsibility of each rule file under the current RealismItemRules directory and how the GUI categories map to the underlying JSON structure.

## 1. Rule File List

The main rule files are located under RealismItemRules:

- RealismItemRules/weapon_rules.json
- RealismItemRules/attachment_rules.json
- RealismItemRules/ammo_rules.json
- RealismItemRules/gear_rules.json
- RealismItemRules/item_exceptions.json

The first four files define range rules. item_exceptions.json defines final field overrides for specific items.

## 2. Range Node Structure

Most editable rule nodes use this structure:

```json
{
  "min": 0.0,
  "max": 0.0,
  "preferInt": false
}
```

Field meanings:

- min: lower bound used during generation
- max: upper bound used during generation
- preferInt: whether this field should preferably be handled as an integer

## 3. The Four Main Rule Groups

### 3.1 Weapons

weapon_rules.json mainly contains:

- global weapon clamp rules
- base weapon rules
- weapon caliber modifiers
- weapon stock modifiers

Shotgun caliber modifiers are currently split into 12g, 20g, and 23x75 so different shotgun systems can retain distinct recoil and spread characteristics.

### 3.2 Attachments

attachment_rules.json mainly contains:

- global attachment clamp rules
- attachment profile ranges

### 3.3 Ammo

ammo_rules.json mainly contains:

- base caliber ranges
- special ammo modifiers
- penetration-tier modifiers

Shotgun base profiles are currently split into 12g, 20g, and 23x75, then further differentiated by shot_shell_payload for buckshot, flechette, slug, and similar payloads.

### 3.4 Gear

gear_rules.json mainly contains:

- global gear clamp rules
- gear profile ranges

## 4. How the GUI Maps These Rules

The GUI does not expose the raw JSON directly. Instead, it expands rules into:

- major category
- classification
- profile
- field range

The goal is to keep day-to-day tuning focused on ranges and values rather than turning the full rule file into a raw JSON text editor.

## 5. Relationship Between Item Exceptions and Rules

item_exceptions.json does not replace the four main rule groups. It applies targeted overrides to specific ItemIDs at the final stage of generation.

It is useful for cases such as:

- unusual template field structures
- specific items that must keep fields outside the normal range
- gear items that must preserve fields such as IsGasMask, MaskToUse, or GasProtection

## 6. Adjustment Recommendations

- Tune range rules first, then decide whether item exceptions are really needed
- Change only a small number of fields at a time so generated output and audit results are easy to review
- If extreme values appear, check the clamp rules for the matching category first
- When editing item exceptions, use the real generated object from output as the baseline whenever possible

## 7. What the GUI Does Not Do

- The GUI does not directly modify source templates under input
- The GUI does not replace output-based regression validation
- item_exceptions should not be abused as a replacement for category-level rules
