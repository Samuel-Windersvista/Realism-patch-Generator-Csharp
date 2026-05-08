# Patch Generation Flow Guide

This document describes how the current C# Realism Patch Generator turns input JSON into output patches.

It is the English companion for docs/补丁生成流程说明.md and reflects the current implementation behavior.

## 1. Core Features

The current core directly supports these input formats:

- RealismStandardTemplate
- WttArmory_templates
- Epic_templates
- ConsortiumOfThings_templates
- Requisitions_templates
- EcoAttachment_templates
- Artem_templates
- WttStandalone_templates
- SptBattlepass_templates
- RaidOverhaul_templates
- Moxo_Template
- Mixed_templates

Other itemTplToClone sources are not connected unless explicitly subclassed.

Other important behavior:

- Moxo_Template supports clone + item/items and prioritizes locales.Name for output Name
- Mixed_templates can contain clone + item/items and direct item/items in the same file
- only RealismStandardTemplate under input/attatchments, input/gear, input/weapons keeps original filename
- other supported outputs use source-filename + _realism_patch.json
- output item order is preserved from source input order
- final output keeps only Realism-standard fields

## 2. Entry Points

Both GUI and CLI use the same core generator:

- core implementation: RealismPatchGenerator.Core/RealismPatchGenerator.cs
- GUI entry: RealismPatchGenerator.Gui/Form1.cs
- CLI entry: RealismPatchGenerator.Cli/Program.cs

## 3. Startup Steps

When RealismPatchGenerator is created, it prepares runtime context:

- resolves base path and data directories
- initializes runtime seed (or uses provided seed)
- loads rules via RuleSetLoader
- loads item exceptions

Key directories:

- input
- RealismItemTemplates
- RealismItemRules
- output

## 4. File Discovery and Format Detection

Generation scans JSON files under input and detects format per file.

Detection chain conceptually includes:

- RealismStandardTemplate markers
- itemTplToClone family (WTT subclass routing by filename patterns)
- ItemToClone family (RaidOverhaul)
- clone + item/items family (Moxo / Mixed)

Unsupported files are skipped with logs.

## 5. Build Pipeline

For each item in each recognized file:

1. resolve clone/template base when needed
2. extract source item info (name/type/category cues)
3. build patch object from standard template and rule ranges
4. apply subclass-specific parent/template hint logic
5. apply field allowlist and remove non-standard fields
6. apply item_exceptions as final override layer
7. keep source order when writing output

## 6. Rules and Sampling

Range fields are sampled within min/max bounds.

- if seed is fixed, generation is reproducible
- if seed is empty/random, repeated runs can produce different values within allowed ranges

Main rule files:

- RealismItemRules/weapon_rules.json
- RealismItemRules/attachment_rules.json
- RealismItemRules/ammo_rules.json
- RealismItemRules/gear_rules.json
- RealismItemRules/item_exceptions.json

## 7. Output Writing Rules

- output directory is not globally cleared before each run
- only target files in current run are overwritten
- filename behavior:
  - keep original filename only for RealismStandardTemplate under input/attatchments, input/gear, input/weapons
  - append _realism_patch for all other supported structures

## 8. Logging and Statistics

Generation returns:

- output path
- used seed
- per-category counts (Weapons, Attachments, Ammo, Gear, Consumables)
- total count
- runtime logs

## 9. Validation Recommendations

Recommended workflow after logic/rule changes:

1. run dotnet build
2. run tests
3. run a small focused generation sample
4. inspect output structure and key fields
5. run full generation when sample is clean

## 10. Boundary Notes

Current primary generation focus:

- weapons
- attachments
- ammo
- gear

consumable is not a primary production path in the current workflow.

This English file is a companion copy for international readability; for exhaustive step-by-step details and full Chinese examples, refer to docs/补丁生成流程说明.md.
