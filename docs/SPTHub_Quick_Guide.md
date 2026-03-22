# SPT Hub Release Quick Guide / SPTHub 发布简版说明

SPT Realism Value Range Generator v1.30.0 is a standalone C# toolset for editing realism value ranges, generating patch output, auditing generated results, and applying item-specific exceptions.

SPT 现实主义数值范围编辑生成器 v1.30.0 是一个独立的 C# 工具集，用于编辑现实主义数值规则、生成补丁输出、审计生成结果，以及对特定物品应用字段级例外。

## Key Features / 核心功能

- GUI-based editing for weapon, attachment, ammo, and gear rules.
- 图形化编辑武器、附件、弹药、装备四大类规则。
- GUI generation and audit workflow.
- 支持 GUI 生成与审计工作流。
- Item-specific exception overrides through RealismItemRules/item_exceptions.json.
- 支持通过 RealismItemRules/item_exceptions.json 对个别物品做例外覆盖。
- Template-driven structure audit for weapons, attachments, ammo, and gear.
- 武器、附件、弹药、装备四类均支持模板驱动结构检修。
- Shotgun handling split into 12g, 20g, and 23x75 on both ammo and weapon sides.
- 霰弹规则已在弹药与武器两侧拆分为 12g、20g、23x75。

## Quick Use / 快速使用

1. Put source JSON files into input.
1. 将源 JSON 放入 input。
1. Run the GUI.
1. 运行 GUI。
1. Check generated results under output.
1. 在 output 中查看生成结果。
1. Re-running generation will resample values inside the configured ranges, so the same item can change between runs.
1. 重复执行生成会在配置范围内重新采样，因此同一物品在不同轮生成之间可能变化。
1. Run audit if you need a violation report.
1. 如果需要违规报告，再执行审计。

GUI:

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

- GUI seed box: leave it blank for fresh randomness, enter a fixed seed to reproduce, clear it to go back to random generation, or reuse the most recently used seed.
- GUI Seed 框：留空表示每次重新随机，输入固定 seed 可复现结果，也可以一键清空，或回填最近一次实际使用的 seed。

Audit and generation are both executed inside the GUI.

生成与审计均在 GUI 内执行。

## Audit Scope / 审计范围说明

- Weapons, attachments, ammo, and gear are included in the main audit scope.
- 武器、附件、弹药、装备属于主要审计范围。
- consumable and ordinary cosmetic items are excluded.
- consumable 和普通 cosmetic 不纳入主要审计范围。
- mod_profile_unresolved attachment entries are excluded from main noise results.
- attachment 中的 mod_profile_unresolved 不计入主要噪音结果。
- Item exceptions are exempted per field rather than skipping entire items.
- item_exceptions 按字段豁免，而不是整件物品跳过。

## Release Package Notes / 发布包说明

- Full package: larger size, no preinstalled runtime required. Recommended for most users.
- 完整包：体积较大，但不需要预装运行时。大多数用户直接选这个。
- Lightweight package: much smaller size, requires matching .NET runtime on the target machine. Use it only when the runtime is already installed.
- 轻量包：体积更小，但目标机器需要预装对应 .NET 运行时。只有确认目标机器已经安装运行时时才建议选用。
- Release output now includes both archives side by side: `RealismPatchGenerator-v<version>-win-x64.zip` and `RealismPatchGenerator-v<version>-win-x64-fd.zip`.
- 当前发布会同时提供两种压缩包：`RealismPatchGenerator-v<版本>-win-x64.zip` 和 `RealismPatchGenerator-v<版本>-win-x64-fd.zip`。

## Documentation / 文档入口

- Full bilingual project overview: README.md
- 项目双语总览：README.md
- Chinese guide: docs/使用说明.md
- 中文使用说明：docs/使用说明.md
- English guide: docs/User_Guide.en.md
- 英文使用说明：docs/User_Guide.en.md
