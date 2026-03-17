# SPT Realism Value Range Editor Generator v1.2

SPT Realism Value Range Editor Generator is a standalone C# toolset for editing realism value ranges, generating patch output, auditing generated results, and applying item-specific exceptions.

SPT 现实主义数值范围编辑生成器是一个独立的 C# 工具集，用于编辑现实主义数值规则、生成补丁输出、审计生成结果，以及对特定物品应用例外覆盖。

Current version / 当前版本: v1.2

## Overview / 简介

This project consolidates the GUI editor, CLI generator, rule files, item exception management, and output auditing into one .NET solution.

本项目将 GUI 编辑器、CLI 生成器、规则文件、例外物品管理和输出审计统一收拢到同一个 .NET 解决方案中。

## Main Features / 主要功能

- Edit weapon, attachment, ammo, and gear rule ranges in the GUI.
- 在 GUI 中编辑武器、附件、弹药、装备四大类规则范围。
- Generate patch output from the GUI or the CLI.
- 通过 GUI 或 CLI 生成补丁输出。
- Audit generated output and write reports into audit_reports.
- 审计生成结果，并将报告写入 audit_reports。
- Apply item-specific exception fields through rules/item_exceptions.json.
- 通过 rules/item_exceptions.json 对个别物品应用字段级例外。
- Support CURRENT_PATCH, STANDARD, CLONE, ITEMTOCLONE, VIR, and TEMPLATE_ID input styles.
- 支持 CURRENT_PATCH、STANDARD、CLONE、ITEMTOCLONE、VIR、TEMPLATE_ID 六类输入结构。
- Split shotgun handling into 12g, 20g, and 23x75 on both ammo and weapon sides.
- 霰弹规则已在弹药与武器两侧拆分为 12g、20g、23x75。

## Quick Start / 快速开始

1. Put your source JSON files under input.
1. 将待处理 JSON 放入 input。
1. Launch the GUI or run the CLI generator.
1. 启动 GUI，或直接运行 CLI 生成器。
1. Review generated files under output.
1. 在 output 中检查生成结果。
1. Run the audit if you want a rule-violation report.
1. 如果需要规则检查报告，再运行审计。

Run GUI:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

The GUI also supports an optional Seed input. Leave it blank to resample on each run, enter a fixed unsigned integer to reproduce the same output, use Clear to switch back to random generation, or use Use Last to restore the most recent generation seed.

GUI 也支持可选的 Seed 输入框。留空时每次生成重新采样，输入固定无符号整数时可复现相同输出；还可以一键清空 seed，或把最近一次生成实际使用的 seed 填回输入框。

Run generation:

```powershell
dotnet run --project RealismPatchGenerator.Cli
```

Run generation with a fixed seed:

```powershell
dotnet run --project RealismPatchGenerator.Cli -- --seed 123456
```

Run audit:

```powershell
dotnet run --project RealismPatchGenerator.Cli -- audit
```

Common audit parameters:

- --output-dir
- --report-file
- --include-ok
- --include-template-exports
- --fail-on-violations

## Directory Layout / 目录约定

The program uses the repository root as its default data root.

程序默认以仓库根目录为数据根。

- input
- 现实主义物品模板
- output
- audit_reports
- rules

## Current Behavior Notes / 当前行为说明

- output is not cleared as a whole directory before each run; only current target files are overwritten.
- output 不会在每次运行前整目录清空，只覆盖当前目标文件。
- each generation samples fresh values within the configured ranges, so repeated runs can produce different numeric results for the same item.
- 每次生成都会在配置范围内重新采样，因此同一物品在重复生成时可能出现不同数值结果。
- output preserves input source order so generated files follow the source file item order for easier manual review.
- output 会保留输入源顺序，生成结果按源文件中的条目顺序写出，便于人工核对。
- consumable and ordinary cosmetic items are outside the main audit scope.
- consumable 和普通 cosmetic 不属于主要审计范围。
- mod_profile_unresolved attachment entries are excluded from main audit noise.
- attachment 中的 mod_profile_unresolved 不计入主要审计噪音。
- Item exceptions are exempted per field during audit instead of skipping whole items.
- item_exceptions 在审计时按字段豁免，而不是整件物品跳过。

## Item Exceptions / 例外物品

The Item Exceptions window supports searching generated items by Name, loading their current top-level fields, editing allowed fields, and saving per-item overrides into rules/item_exceptions.json.

例外物品窗口支持按 Name 搜索已生成物品、读取当前顶层字段、编辑允许字段，并将每个物品的覆盖规则保存到 rules/item_exceptions.json。

## Documentation / 文档入口

- Chinese user guide / 中文使用说明: docs/使用说明.md
- English user guide / 英文使用说明: docs/User_Guide.en.md
- Chinese rule overview / 中文规则说明: docs/规则说明.md
- English rule overview / 英文规则说明: docs/Rule_Overview.en.md
- Weapon rules / 武器规则: docs/武器属性规则指南.md and docs/Weapon_Rule_Guide.en.md
- Attachment rules / 附件规则: docs/附件属性规则指南.md and docs/Attachment_Rule_Guide.en.md
- Ammo rules / 弹药规则: docs/弹药属性规则指南.md and docs/Ammo_Rule_Guide.en.md
- Gear rules / 装备规则: docs/装备属性规则指南.md and docs/Gear_Rule_Guide.en.md
- SPTHub quick guide / SPTHub 简版说明: docs/SPTHub_Quick_Guide_Bilingual.md
- SPTHub English description / SPTHub 英文发布文案: docs/SPTHub_Description.en.md
- SPTHub v1.2 release notes / SPTHub v1.2 发布摘要: docs/SPTHub_Release_Notes_v1.2.en.md
- Changelog / 更新日志: CHANGELOG.md

## Release Notes / 发布说明

The repository currently contains both a full self-contained release package and a lightweight framework-dependent release package.

仓库当前同时提供完整自包含发布包和轻量级 framework-dependent 发布包。

- Full release: larger package, no preinstalled runtime required.
- 完整版：体积较大，但目标机器不需要预装运行时。
- Lightweight release: much smaller package, but the target machine must already have the matching .NET runtime installed.
- 轻量版：体积小很多，但目标机器需要预装对应 .NET 运行时。
