# SPT Realism Range Editor Generator v2.1 User Guide

This document reflects the current C# project status and covers GUI workflow, rule editing, item exceptions, and patch generation.

## 1. Version Positioning

- Version: v2.1
- Runtime shape: standalone .NET GUI application (full package or lightweight package)
- Main focus: rule editing, patch generation, and item exceptions in one project

## 2. Directory Convention

The application uses repository root as data root and depends on:

- input
- RealismItemTemplates
- RealismItemRules
- output

Notes:

- RealismItemRules stores the four major rule files and RealismItemRules/item_exceptions.json
- output stores generated patches

## 3. GUI Workflow

Start GUI:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

Main interface layout:

- top: Save All, Reload, Generate Patches, language switch, search
- right of output path: optional Seed box (empty = re-sample every run; fixed uint = reproducible output), plus clear and reuse-last-seed actions
- left: rule category tree
- center: rule range table
- bottom: field description, exception overview, runtime logs
- extra entry: Item Exceptions window for final per-ItemID overrides

Typical operation order:

1. reload rules and confirm current on-disk state
2. select category in left tree and tune ranges in center table
3. click Save All to write back to RealismItemRules
4. click Generate Patches to output into output
5. if a few items need special handling, use Item Exceptions for targeted overrides

## 4. Item Exception Management

Use item exceptions when a specific ItemID must preserve or force specific fields.

Current window flow:

1. search generated output items by Name
2. load an item and inspect current top-level fields
3. edit existing field values or add allowed fields for the item category
4. click Add/Modify Field to update the field list
5. click Save Item to write to RealismItemRules/item_exceptions.json

Behavior constraints:

- addable fields are restricted to the current item category field pool
- suggested ranges come from current rule data
- numeric values are normalized and safely clamped on save
- structure standard follows RealismItemTemplates; value range standard follows RealismItemRules

## 5. Output Behavior

Current behavior:

- writes output into output
- does not wipe the entire output directory first
- only overwrites target files for current run
- only RealismStandardTemplate under input/attatchments, input/gear, input/weapons keeps original filename
- other supported formats use source filename + _realism_patch.json
- values are sampled within ranges each run (unless fixed seed is used)
- output item order strictly follows source input item order
- currently supported formats:
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
- item_exceptions is applied as final override layer after automatic rule processing

## 6. Release Packages

- full package: no preinstalled .NET runtime required
- lightweight package: smaller size, requires matching .NET Desktop Runtime on target machine
- packaging script: scripts/build-release.ps1

## 7. Entry Points

Recommended primary entry is GUI. CLI keeps a one-click generation entry.

Start GUI in development:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

One-click generation with CLI:

```powershell
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj
```

Optional CLI parameters:

- [basePath] [outputPath] (positional, optional)
- --seed <uint>

CLI boundary:

- generation only
- rule editing, exception management, and interactive checks are handled in GUI

For reproducible generation, fill a fixed seed in GUI; clear it to return to random mode.

## 8. Current Boundaries

- GUI optimization is still Chinese-first
- GUI automation tests are not fully completed yet
- item exception window is production-usable but should still be validated against real output regressions
