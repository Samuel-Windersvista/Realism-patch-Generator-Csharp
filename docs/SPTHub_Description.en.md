# SPT Realism Value Range Editor Generator v1.2

SPT Realism Value Range Editor Generator is a standalone C# toolset for editing realism value ranges, generating patch output, auditing generated results, and applying item-specific exceptions.

This project combines a GUI editor, a CLI generator, rule-based tuning for weapons, attachments, ammo, and gear, plus an audit workflow in one .NET solution.

Version 1.2 adds a complete seed workflow for reproducible generation. By default, each run uses a fresh runtime seed and can resample values inside configured ranges. When reproducibility is needed, both the CLI and GUI can use a fixed seed to generate the same result again.

## Highlights

- GUI-based editing for weapon, attachment, ammo, and gear rules
- CLI generation and audit workflow
- Fixed-seed generation in both GUI and CLI
- GUI seed controls for clear/reset and reuse of the most recently used seed
- Item-specific exception overrides through rules/item_exceptions.json
- Shotgun handling split into 12g, 20g, and 23x75 on both ammo and weapon sides
- Output order preserved from the input source files for easier manual review

## Quick Usage

- Put source JSON files into input
- Run the GUI or CLI generator
- Review generated files under output
- Run the audit if you need a rule-violation report

CLI fixed-seed example:

```powershell
dotnet run --project RealismPatchGenerator.Cli -- --seed 123456
```

## Notes

- Weapons, attachments, ammo, and gear are included in the main audit scope
- consumable and ordinary cosmetic items are excluded from the main audit scope
- mod_profile_unresolved attachment entries are excluded from main audit noise
- Item exceptions are exempted per field rather than skipping entire items

## Release Packages

- Full package: larger size, no preinstalled runtime required
- Lightweight package: much smaller size, requires a matching .NET runtime on the target machine
