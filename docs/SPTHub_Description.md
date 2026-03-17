# SPT Realism Value Range Editor Generator v1.2

SPT Realism Value Range Editor Generator is a standalone C# toolset for editing realism value ranges, generating patch output, auditing generated results, and applying item-specific exceptions.

SPT 现实主义数值范围编辑生成器 v1.2 是一个独立的 C# 工具集，用于编辑现实主义数值规则、生成补丁输出、审计生成结果，并对特定物品应用字段级例外覆盖。

This project combines a GUI editor, a CLI generator, rule files, item exception management, and an audit workflow in one .NET solution. It is designed for users who want to maintain realism tuning data in a more structured way, generate consistent patch output, and verify whether the generated results still follow the intended rule ranges.

本项目将 GUI 编辑器、CLI 生成器、规则文件、例外物品管理和输出审计统一收拢到同一个 .NET 解决方案中。它适合希望以更结构化方式维护现实主义数值规则、生成稳定补丁结果，并在生成后继续验证输出是否遵循预期范围的用户。

Version 1.2 adds a complete seed workflow for reproducible generation. By default, each run uses a fresh runtime seed and can resample values inside configured ranges. When reproducibility is needed, both the CLI and GUI can use a fixed seed to generate the same result again.

v1.2 进一步补全了随机 seed 工作流。默认情况下，每次生成都会使用新的运行时 seed，并在配置范围内重新采样；如果需要复现某一轮结果，CLI 和 GUI 都可以使用固定 seed 再次生成出同样的输出。

## Project Overview / 项目概览

SPT Realism Value Range Editor Generator focuses on the full rule-to-output workflow for realism data:

SPT 现实主义数值范围编辑生成器聚焦于“规则到输出”的完整现实主义数据工作流：

- Edit rule ranges for weapons, attachments, ammo, and gear in the GUI.
- 在 GUI 中编辑武器、附件、弹药、装备四大类规则范围。
- Generate patch output from either the GUI or the CLI.
- 通过 GUI 或 CLI 生成补丁输出。
- Audit the generated output against current rule files.
- 使用当前规则文件对生成结果做规则审计。
- Apply per-item field overrides through item exceptions.
- 通过 item exceptions 对单个物品应用字段级例外覆盖。
- Keep output order aligned with the input source file order for easier manual review.
- 保持输出顺序与输入源文件条目顺序一致，便于人工核对。

## Main Capabilities / 主要能力

- GUI-based editing for weapon, attachment, ammo, and gear rules.
- 图形化编辑武器、附件、弹药、装备四大类规则。
- CLI generation and audit workflow for batch use.
- 支持命令行批量生成与审计工作流。
- Fixed-seed generation in both GUI and CLI for reproducible runs.
- GUI 与 CLI 均支持固定 seed 生成，用于复现同一轮结果。
- GUI seed controls for clear/reset behavior and quick reuse of the most recently used seed.
- GUI 提供 seed 清空、回到随机模式以及回填最近一次使用 seed 的交互。
- Item-specific exception overrides through rules/item_exceptions.json.
- 支持通过 rules/item_exceptions.json 对个别物品做字段级例外覆盖。
- Support for CURRENT_PATCH, STANDARD, CLONE, ITEMTOCLONE, VIR, and TEMPLATE_ID input styles.
- 支持 CURRENT_PATCH、STANDARD、CLONE、ITEMTOCLONE、VIR、TEMPLATE_ID 六类输入结构。
- Shotgun handling split into 12g, 20g, and 23x75 on both ammo and weapon sides.
- 霰弹规则已在弹药与武器两侧拆分为 12g、20g、23x75。

## Typical Workflow / 典型使用流程

1. Put source JSON files into input.
1. 将源 JSON 文件放入 input。
1. Open the GUI to review or edit rule ranges.
1. 打开 GUI 检查或调整规则范围。
1. Save changes back into the rules directory.
1. 将修改保存回 rules 目录。
1. Run generation from the GUI or CLI.
1. 通过 GUI 或 CLI 执行生成。
1. Review generated files under output.
1. 在 output 中检查生成结果。
1. Run the audit if you need a rule-violation report.
1. 如果需要规则违规报告，再执行审计。
1. Use item exceptions for a small number of structurally unusual items.
1. 对少量结构特殊的物品，再使用 item exceptions 做定向处理。

## Seed and Reproducibility / 随机 Seed 与结果复现

By default, generation uses a fresh runtime seed, so repeated runs can produce different numeric values for the same item as long as those values stay inside the configured ranges.

默认情况下，生成器会使用新的运行时 seed，因此同一物品在多次生成之间可能出现不同数值，只要这些数值仍然落在配置好的范围内。

When you need to reproduce a specific run:

当你需要复现某一轮生成结果时：

- CLI supports --seed for fixed-seed generation.
- CLI 可通过 --seed 指定固定 seed。
- GUI supports an optional seed box.
- GUI 提供可选的 seed 输入框。
- GUI can clear the current seed to return to random generation.
- GUI 可清空当前 seed，回到随机生成模式。
- GUI can also reuse the most recently used generation seed.
- GUI 还可以一键回填最近一次实际使用的 seed。

CLI fixed-seed example:

CLI 固定 seed 示例：

```powershell
dotnet run --project RealismPatchGenerator.Cli -- --seed 123456
```

## Audit Scope / 审计范围

The main audit scope currently covers weapons, attachments, ammo, and gear.

当前主要审计范围覆盖武器、附件、弹药和装备。

- consumable and ordinary cosmetic items are outside the main audit scope.
- consumable 和普通 cosmetic 不属于主要审计范围。
- mod_profile_unresolved attachment entries are excluded from main audit noise.
- attachment 中的 mod_profile_unresolved 不计入主要审计噪音。
- Item exceptions are exempted per field rather than skipping whole items.
- item_exceptions 在审计时按字段豁免，而不是整件物品跳过。

## Current Behavior Notes / 当前行为说明

- output is not cleared as a whole directory before each run; only current target files are overwritten.
- output 不会在每次运行前整目录清空，只覆盖当前目标文件。
- output preserves input source order so generated files remain easier to inspect manually.
- output 会保留输入源顺序，因此生成文件更便于人工逐项核对。
- The generator is built around rule-driven numeric ranges instead of hardcoded one-off item edits.
- 生成器以规则驱动的数值范围为核心，而不是依赖零散的单物品硬编码修改。

## Release Packages / 发布包说明

- Full package: larger size, no preinstalled runtime required.
- 完整包：体积较大，但目标机器不需要预装运行时。
- Lightweight package: much smaller size, requires a matching .NET runtime on the target machine.
- 轻量包：体积更小，但目标机器需要预装对应的 .NET 运行时。

## Documentation / 文档入口

- Full bilingual project overview: README.md
- 项目双语总览：README.md
- Chinese guide: docs/使用说明.md
- 中文使用说明：docs/使用说明.md
- English guide: docs/User_Guide.en.md
- 英文使用说明：docs/User_Guide.en.md
- SPTHub quick guide: docs/SPTHub_Quick_Guide_Bilingual.md
- SPTHub 简版说明：docs/SPTHub_Quick_Guide_Bilingual.md
- v1.2 release notes: docs/SPTHub_Release_Notes_v1.2.en.md
- v1.2 更新说明：docs/SPTHub_Release_Notes_v1.2.en.md
