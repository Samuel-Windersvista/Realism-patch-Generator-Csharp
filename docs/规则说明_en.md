# SPT Realism Range Editor Generator v2.0 Rule Guide

This document explains the responsibility of each rule file under RealismItemRules and how GUI categories map to the actual JSON structure.

## 1. Rule File List

The main rule files are located in RealismItemRules:

- RealismItemRules/weapon_rules.json
- RealismItemRules/attachment_rules.json
- RealismItemRules/ammo_rules.json
- RealismItemRules/gear_rules.json
- RealismItemRules/item_exceptions.json

The first four define range rules. item_exceptions.json defines final per-item field overrides.

## 2. Range Node Structure

Most editable rule nodes use this shape:

```json
{
  "min": 0.0,
  "max": 0.0,
  "preferInt": false
}
```

Field meanings:

- min: lower bound for generation
- max: upper bound for generation
- preferInt: whether this field should prefer integer values

## 3. Four Major Rule Groups

### 3.1 Weapons

weapon_rules.json mainly includes:

- global weapon clamping
- base weapon profiles
- weapon caliber modifiers
- weapon stock modifiers

Shotgun caliber modifiers are split into 12g, 20g, and 23x75 profiles.

### 3.2 Attachments

attachment_rules.json mainly includes:

- global attachment clamping
- attachment profile ranges

### 3.3 Ammo

ammo_rules.json mainly includes:

- caliber base ranges
- special ammo modifiers
- penetration-tier modifiers

Shotgun base profiles are split into 12g, 20g, and 23x75 and then adjusted with shot_shell_payload behavior.

### 3.4 Gear

gear_rules.json mainly includes:

- global gear clamping
- gear profile ranges
- gear profile price ranges

## 4. How the GUI Maps These Rules

The GUI does not expose raw JSON directly. It expands rules into:

- major category
- subgroup
- profile
- field ranges

This keeps tuning focused on ranges and values rather than direct raw JSON editing.

## 5. Relationship Between Exceptions and Rules

item_exceptions.json does not replace the four major rule files. It applies final per-ItemID patch overrides at the end of generation.

It is suitable for:

- special template structures
- preserving fields that need to exceed normal ranges
- handling gear-specific fields such as IsGasMask, MaskToUse, and GasProtection

## 6. Tuning Recommendations

- tune range rules first, then use item exceptions only if needed
- change only a small number of fields per iteration
- if extreme values appear, check corresponding clamp rules first
- use actual generated output objects as the editing baseline for exceptions

## 7. Current Non-Goals

- GUI does not directly modify input templates
- GUI does not replace output regression validation
- item_exceptions should not be used as a replacement for full category rules
