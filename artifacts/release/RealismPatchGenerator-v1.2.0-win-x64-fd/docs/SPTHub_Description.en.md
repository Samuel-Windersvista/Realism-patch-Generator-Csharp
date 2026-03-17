# SPT Realism Value Range Editor Generator

SPT Realism Value Range Editor Generator is a standalone C# toolset for editing realism value ranges, generating patch output, auditing generated results, and applying item-specific exceptions.

This project combines a GUI editor, a CLI generator, rule-based tuning for weapons, attachments, ammo, and gear, plus an audit workflow in one .NET solution.

## Features

- GUI-based editing for weapon, attachment, ammo, and gear rules
- CLI generation and audit workflow
- Item-specific exception overrides through rules/item_exceptions.json
- Support for CURRENT_PATCH, STANDARD, CLONE, ITEMTOCLONE, VIR, and TEMPLATE_ID input styles
- Shotgun handling split into 12g, 20g, and 23x75 on both ammo and weapon sides

## Usage

Put your source JSON files into input, run the GUI or CLI generator, review generated files under output, and run the audit if you need a rule-violation report.

GUI:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

CLI generate:

```powershell
dotnet run --project RealismPatchGenerator.Cli
```

CLI audit:

```powershell
dotnet run --project RealismPatchGenerator.Cli -- audit
```

## Notes

- Weapons, attachments, ammo, and gear are included in the main audit scope
- consumable and ordinary cosmetic items are excluded from the main audit scope
- mod_profile_unresolved attachment entries are excluded from main audit noise
- Item exceptions are exempted per field rather than skipping entire items

## Release Packages

- Full package: larger size, no preinstalled runtime required
- Lightweight package: much smaller size, requires a matching .NET runtime on the target machine

## Documentation

- README.md
- docs/User_Guide.en.md
- docs/Rule_Overview.en.md
- docs/SPTHub_Quick_Guide_Bilingual.md
