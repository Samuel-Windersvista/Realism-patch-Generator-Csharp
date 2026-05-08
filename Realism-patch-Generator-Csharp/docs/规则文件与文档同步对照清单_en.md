# Rule and Documentation Sync Checklist (v2.0)

Purpose: after changing rules, template mapping, item-exception workflow, or audit logic, quickly confirm code and docs are still aligned.

## A. Code Verification Scope

Primary files to verify:

- RealismPatchGenerator.Core/RuleSetLoader.cs
- RealismPatchGenerator.Core/RealismPatchGenerator.cs
- RealismPatchGenerator.Core/OutputRuleAuditor.cs
- RealismPatchGenerator.Core/WeaponRuleData.cs
- RealismPatchGenerator.Core/AttachmentRuleData.cs
- RealismPatchGenerator.Core/AmmoRuleData.cs
- RealismPatchGenerator.Core/GearRuleData.cs
- RealismPatchGenerator.Core/ItemExceptionImportService.cs
- RealismPatchGenerator.Core/ItemExceptionFieldGuardService.cs
- RealismPatchGenerator.Gui/Form1.cs
- RealismPatchGenerator.Gui/ItemExceptionsForm.cs
- RealismItemRules/weapon_rules.json
- RealismItemRules/attachment_rules.json
- RealismItemRules/ammo_rules.json
- RealismItemRules/gear_rules.json
- RealismItemRules/item_exceptions.json

## B. Main Docs in docs/

Prioritized documents:

- docs/使用说明.md
- docs/规则说明.md
- docs/武器属性规则指南.md
- docs/附件属性规则指南.md
- docs/弹药属性规则指南.md
- docs/装备属性规则指南.md

## C. Minimum Checks After Each Change

### C1. GUI

- startup command still: dotnet run --project RealismPatchGenerator.Gui
- docs still clearly state generation and audit are executed from GUI
- button names in docs still match current GUI labels

### C2. Item Exceptions

- exception window still searches output by Name
- behavior description of "Add/Modify Field" and "Save Item" is still accurate
- item_exceptions.json location and scope are correct

### C3. Audit Scope

- consumable and regular cosmetic are still outside primary audit scope
- mod_profile_unresolved is still excluded as attachment audit noise
- exception behavior is still field-level exemption, not full-item skip

### C4. Output Behavior

- docs still state output directory is not wiped entirely
- docs still state only target files are overwritten
- docs still state RealismStandardTemplate under input/attatchments, input/gear, input/weapons keeps original filename
- docs still state other supported outputs append _realism_patch
- docs still state output item order follows source input order
- template-consistency explanation still covers the four major item format families

### C5. Version

- docs titles and version notes are updated to the release version
- CHANGELOG.md and README.md are synced to the same version

## D. Recommended Release Sequence

1. Update code, rules, and template-related logic.
2. Run dotnet build RealismPatchGenerator.slnx.
3. Generate a small output sample and validate key files.
4. Run audit from GUI and inspect audit_reports.
5. Sync docs and version metadata.
6. Run full generation and manual regression checks.
