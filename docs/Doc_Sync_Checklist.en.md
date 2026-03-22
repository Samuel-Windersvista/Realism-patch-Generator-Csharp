# Rule and Documentation Sync Checklist v1.30.0

Purpose: after each adjustment to rules, template mappings, item exception flow, or audit logic, quickly confirm that the codebase and docs are still aligned.

## A. Code Files to Review

Baseline files to verify:

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

## B. Main Documents Under docs

These files are the current priority set:

- docs/使用说明.md
- docs/规则说明.md
- docs/武器属性规则指南.md
- docs/附件属性规则指南.md
- docs/弹药属性规则指南.md
- docs/装备属性规则指南.md

## C. Minimum Checks After Each Change

### C1. GUI

- Confirm the GUI launch command is still dotnet run --project RealismPatchGenerator.Gui
- Confirm the docs still state that generation and audit are performed through the GUI
- Confirm the button names in the user guide still match the current GUI labels

### C2. Item Exceptions

- Confirm the exception window still searches output results by Name only
- Confirm the documented behavior of Add/Update Field and Save Item is still accurate
- Confirm the save path and scope of item_exceptions.json are still described correctly

### C3. Audit Scope

- consumable and ordinary cosmetic items are still outside the main rule-audit scope
- mod_profile_unresolved is still excluded from main attachment-audit noise
- Exception-based audit bypass is still per-field, not whole-item skipping

### C4. Output Behavior

- Confirm the docs still describe that output is not cleared as a whole directory
- Confirm the docs still describe that generation overwrites target files only
- Confirm the template-consistency notes still cover all four main item groups

### C5. Versioning

- Confirm titles and version notes in docs are all updated to v1.30.0
- Confirm CHANGELOG.md and README.md are also synchronized to the same version

## D. Recommended Release Order

1. Modify code, rules, and template-related logic.
2. Run dotnet build RealismPatchGenerator.slnx.
3. Generate a small output sample and check the key file structures.
4. Run the audit from the GUI and review audit_reports.
5. Synchronize docs and version numbers.
6. Perform full generation and manual regression checks last.
