# SPT Realism Value Range Generator v1.30.0

SPT Realism Value Range Generator v1.30.0 is a toolset designed specifically for SPT Realism Mod 1.6.4.

Current version: v1.30.0

When the Realism mod is updated to SPT4, this program will be updated accordingly.

## compatible mods / 兼容的MOD all for 3.11.4

- [WTT - Pack 'n' Strap](https://forge.sp-tarkov.com/mod/1278/wtt-pack-n-strap)
- [Tactical Gear Component](https://forge.sp-tarkov.com/mod/1125/tactical-gear-component)
- [SPT Battlepass](https://forge.sp-tarkov.com/mod/2098/spt-battlepass)
- [WTT- Armory 1.2.1](https://forge.sp-tarkov.com/mod/2246/wtt-armory)
- [Sig Sauer MCX VIRTUS multi carlibre rifle](https://forge.sp-tarkov.com/mod/1373/sig-sauer-mcx-virtus-multi-carlibre-rifle)
- [Eukyre's AK-50 backport](https://forge.sp-tarkov.com/mod/2215/eukyres-ak-50-backport)
- [50 BMG Expansion and Remaster](https://forge.sp-tarkov.com/mod/2224/50-bmg-expansion-and-remaster)
- [Resonant AK from COD:MW2019 - ReUpload](https://forge.sp-tarkov.com/mod/2170/resonant-ak-from-codmw2019-reupload)
- [Mag Tape](https://forge.sp-tarkov.com/mod/1018/mag-tape)
- [BlackCore](https://forge.sp-tarkov.com/mod/985/blackcore)
- [Epic's All in One 3.1.4](https://forge.sp-tarkov.com/mod/1263/epics-all-in-one)
- [Eco's Attachment Emporium 1.2.1](https://forge.sp-tarkov.com/mod/2288/ecos-attachment-emporium)
- [ECOT - Eukyre's Consortium of Things 1.1.0](https://forge.sp-tarkov.com/mod/2195/ecot-eukyres-consortium-of-things#versions)
- [WTT- Artem 2.1.2](https://forge.sp-tarkov.com/mod/1023/wtt-artem)
- Echoes of Tarkov - Requisitions (outdate)
- Raid Overhaul (outdate)

## English

### Overview

This project consolidates the GUI editor, rule files, item exception management, and output auditing into one .NET solution.

### Main Features

- Generate Realism patches quickly.
- Use a logical structure rule system based on RealismItemTemplates and numeric ranges based on RealismItemRules to standardize generated patch values.
- Allow users to adjust the value ranges themselves.
- Generate patch values through a pseudorandom seed-based system.
- Use item_exceptions.json to customize exceptions for specific item properties, allowing flexible stat design without breaking overall balance.
- Provide an audit feature to automatically check whether generated patches violate the rules.
- Support reading and generating six common item-mod data structure styles used in SPT 3.11.4, with fallback handling to remain as compatible as possible when the input style cannot be fully recognized.

### Quick Start

1. Put your source JSON files under input.
1. Launch the GUI.
1. Review generated files under output.
1. Run the built-in GUI audit if you want a rule-violation report.

Release package notes:

- Full package: no preinstalled .NET runtime required, but the package is larger. Recommended for most users.
- Lightweight package: much smaller, but the target machine must already have the matching .NET Desktop Runtime installed. Recommended only if you already know the target machine has the required runtime.
- Current release output provides both package types side by side: `RealismPatchGenerator-v<version>-win-x64.zip` and `RealismPatchGenerator-v<version>-win-x64-fd.zip`.

### Directory Layout

The program uses the repository root as its default data root.

- input
- RealismItemTemplates
- RealismItemRules
- output
- audit_reports

### Current Behavior Notes

- output is not cleared as a whole directory before each run; only current target files are overwritten.
- each generation samples fresh values within the configured ranges, so repeated runs can produce different numeric results for the same item.
- output preserves input source order so generated files follow the source file item order for easier manual review.
- audit now checks both numeric rule ranges and template-driven output structure for weapons, attachments, ammo, and gear.
- consumable and ordinary cosmetic items are outside the generation scope.
- Item exceptions are exempted per field during audit instead of skipping whole items.

### Item Exceptions

The Item Exceptions window supports searching generated items by Name, loading their current top-level fields, editing allowed fields, and saving per-item overrides into RealismItemRules/item_exceptions.json.

## 中文

SPT 现实主义数值范围编辑生成器 v1.30.0 是专门为SPT Realism Mod 1.6.4设计的工具集。

当前版本: v1.30.0

当现实主义MOD更新到SPT4后，本程序将同步更新。

### 简介

本项目将 GUI 编辑器、规则文件、例外物品管理和输出检修统一收拢到同一个 .NET 解决方案中。

### 主要功能

- 可快捷地生成现实主义补丁
- 使用一套符合逻辑的规则体系（RealismItemTemplates）和数值范围（RealismItemRules）来规范生成的补丁数值
- 数值范围可由用户自行调整
- 补丁的数值由随机种子系统进行伪随机生成
- 通过 item_exceptions.json，可对个别物品的属性进行例外定制。在不破坏数值平衡的情况下，可随意设计属性。
- 检修功能，可自动化检查生成的补丁是否违反规则。
- 支持读取和生成SPT3.11.4版本的六种不同编写习惯的物品MOD数据结构，并用兜底机制尽可能兼容未能识别的编写规范。

### 快速开始

1. 将待处理 JSON 放入 input。
1. 启动 GUI。
1. 在 output 中检查生成结果。
1. 如果需要规则检查报告，再通过 GUI 执行检修。

发布包说明:

- 完整包: 体积较大，但目标机器不需要预装 .NET 运行时。一般用户优先选这个。
- 轻量包: 体积更小，但目标机器需要预装匹配的 .NET Desktop Runtime。只有在你明确知道目标机器已经装好运行时的情况下才建议选这个。
- 当前发布会同时提供两种压缩包: `RealismPatchGenerator-v<版本>-win-x64.zip` 和 `RealismPatchGenerator-v<版本>-win-x64-fd.zip`。

### 目录约定

程序默认以仓库根目录为数据根。

- input
- RealismItemTemplates
- RealismItemRules
- output
- audit_reports

- 输出结构以 RealismItemTemplates 为结构标准；最终数值范围以 RealismItemRules 为唯一标准。

### 当前行为说明

- output 不会在每次运行前整目录清空，只覆盖当前目标文件。
- 每次生成都会在配置范围内重新采样，因此同一物品在重复生成时可能出现不同数值结果。
- output 会保留输入源顺序，生成结果按源文件中的条目顺序写出，便于人工核对。
- 检修现在会同时检查数值范围和模板结构，武器、附件、弹药、装备四类都按 RealismItemTemplates 做结构校验。
- consumable 和普通 cosmetic 不在物品生成范围内。
- item_exceptions 在检修时按字段豁免，而不是整件物品跳过。

### 例外物品

例外物品窗口支持按 Name 搜索已生成物品、读取当前顶层字段、编辑允许字段，并将每个物品的覆盖规则保存到 rules/item_exceptions.json。
