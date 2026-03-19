# SPT Realism Value Range Generator v1.22 User Guide

This document reflects the current state of the C# project and covers the GUI, rule editing, item exception management, and audit workflow.

## 1. Version Positioning

- Version: v1.22
- Runtime form: standalone .NET GUI tool
- Current focus: rule editing, patch generation, audit checks, and item exception overrides are all handled inside one solution

## 2. Directory Conventions

By default, the program uses the repository root as its data root and depends on these directories:

- input
- 现实主义物品模板
- output
- audit_reports
- rules

In practice:

- rules stores the four main rule files plus rules/item_exceptions.json
- output stores generated patch results
- audit_reports stores audit reports

## 3. GUI Workflow

Start the GUI:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

Main window layout:

- Top: Save All, Reload, Generate Patch, Check Items That Do Not Follow Rules, language toggle, search
- To the right of the output path: an optional Seed box; leave it blank to resample on each run, enter a fixed unsigned integer to reproduce the result, clear it to return to random generation, or restore the most recently used seed
- Left: rule category tree
- Center: rule range editor table
- Bottom: field descriptions, exception overview, runtime log
- Extra entry: the Item Exceptions window is used to override final fields for specific ItemIDs

Typical usage order:

1. Reload rules to confirm the current disk state.
2. Select a category on the left and adjust value ranges in the center.
3. Click Save All to write rules back into the rules directory.
4. Click Generate Patch to write results into output.
5. Click Check Items That Do Not Follow Rules to generate an audit report and review flagged items.
6. If a small number of items must deviate from the general rules, open the Item Exceptions window and add targeted overrides.

## 4. Item Exception Management

Item exceptions are used when a specific ItemID must preserve or force a specific top-level field value.

Current window flow:

1. Search by Name and only search items that have already been generated into output.
2. After loading a search result, the right side automatically displays the current top-level fields of that item.
3. In the same editor area, choose a field, edit its value, or add a new field that is allowed for the item's major category.
4. Click Add/Update Field to write the current field and value back into the field list.
5. Click Save Item to write the exception config for that ItemID into rules/item_exceptions.json.

Current behavior constraints:

- New fields may only come from the field pool allowed for the item's category
- Suggested value ranges come from the latest rule data, not hardcoded constants
- Numeric values are normalized and safely clamped on save to avoid extreme out-of-range values
- Output structure is kept as close as possible to the corresponding category format in 现实主义物品模板

## 5. Output and Audit

Current generation behavior:

- Results are written into output
- The whole output directory is not cleared in advance
- Only the target files for the current run are overwritten
- Each generation samples fresh values within the configured ranges, so repeated runs can produce different numeric results for the same item
- Output order preserves the item order from the input source files to make manual review easier
- Shotgun handling now applies separate ammo base ranges and weapon caliber modifiers for 12g, 20g, and 23x75

Current audit behavior:

- Weapons, attachments, ammo, and gear can be audited
- consumable and ordinary cosmetic items are not part of the main rule-audit scope
- mod_profile_unresolved entries in attachments are excluded from unresolved attachment noise
- Fields explicitly overridden in item_exceptions are exempted per field rather than skipping the whole item

## 6. Launch Mode

The standalone CLI entry point has been removed. Generation and audit are now performed through the GUI only.

Start the GUI in a development environment:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

If you need to reproduce a specific generation run:

- Enter a fixed unsigned integer in the GUI Seed box
- Click Generate Patch to run with that explicit seed
- Clear the Seed box to return to random generation

## 7. Current Boundaries

- The GUI still prioritizes the Chinese-language experience
- Automated GUI interaction tests are still incomplete; current tests mainly cover the core library and rule logic
- The Item Exceptions window is usable, but final validation should still be based on real output results
