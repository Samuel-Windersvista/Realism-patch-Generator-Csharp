# SPT Realism Patch Generator v2.7

当前仓库是 C# 版 SPT Realism 补丁生成器。

它现在主要负责三件事：

- 基于模板与规则生成 Realism 标准补丁
- 通过 GUI 编辑武器、附件、弹药、装备规则范围
- 通过 `item_exceptions.json` 对少量特殊物品做最终覆盖

## 运行要求

- 开发与本地运行：`.NET 10 SDK`
- GUI：`Windows + net10.0-windows`
- CLI / Tests：`net10.0`

如果只是使用发版包：

- 完整包：自带运行时，可直接使用
- 轻量包：需要目标机器预装匹配的 `.NET Desktop Runtime`

## 当前能力范围

- 当前主要入口：GUI、CLI
- 核心生成入口：`RealismPatchGenerator.Core/RealismPatchGenerator.cs`
- 当前主要生成大类：武器、附件、弹药、装备
- `consumable` 当前不作为独立规则链路重点，但仍保留基础兜底生成与统计支持

## 快速开始

### GUI（推荐）

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

### CLI

```powershell
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj
```

### 测试

```powershell
dotnet test .\RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj
```

## 基本工作流

1. 将输入 JSON 放入 `input/`
2. 启动 GUI，调整规则并保存到 `RealismItemRules/`
3. 生成补丁到 `output/`
4. 检查生成结果
5. 如有少量特殊物品，再通过 `item_exceptions.json` 做最终覆盖

## CLI 参数

- 位置参数 `[basePath] [outputPath]`：可选
- `--seed <uint>`：固定随机种子，用于复现结果
- `--include <path>`：只生成指定输入文件或子路径

示例：

```powershell
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output --seed 123456
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\artifacts\perf-small --seed 123456 --include user_templates/file.json
```

## 当前支持的输入格式

当前核心直接支持 12 种输入格式：

1. `RealismStandardTemplate`
2. `WttArmory_templates`
3. `Epic_templates`
4. `ConsortiumOfThings_templates`
5. `Requisitions_templates`
6. `EcoAttachment_templates`
7. `Artem_templates`
8. `WttStandalone_templates`
9. `SptBattlepass_templates`
10. `RaidOverhaulTemplate`
11. `MoxoTemplate`
12. `MixedTemplate`

其中 WTT family 当前正式支持 8 个子类：

- Armory
- Epic
- ConsortiumOfThings
- Requisitions
- EcoAttachment
- Artem
- WttStandalone
- SptBattlepass

更完整的识别特征、处理路径与输出规则见：

- `docs/补丁生成流程说明.md`

## 规则与输出边界

主规则文件位于 `RealismItemRules/`：

- `weapon_rules.json`
- `attachment_rules.json`
- `ammo_rules.json`
- `gear_rules.json`
- `item_exceptions.json`

说明：

- 前四个文件定义范围规则与各类修正逻辑
- `item_exceptions.json` 是最终覆盖层，不替代整类规则
- 输入字段可以参与识别、克隆和推断，但最终输出只保留 Realism 标准补丁字段

## 项目结构

| 路径 | 作用 |
|---|---|
| `RealismPatchGenerator.Core/` | 核心生成引擎 |
| `RealismPatchGenerator.Gui/` | WinForms 图形界面 |
| `RealismPatchGenerator.Cli/` | 命令行入口 |
| `RealismPatchGenerator.Tests/` | xUnit 测试项目 |
| `RealismItemTemplates/` | 输出结构模板 |
| `RealismItemRules/` | 规则配置与例外物品 |
| `input/` | 输入源 JSON |
| `output/` | 生成结果 |
| `docs/` | 使用说明、规则指南与技术说明 |
| `scripts/` | 构建与发布脚本 |
| `artifacts/` | 构建产物与临时结果 |

仓库中还保留了一些维护用途目录，例如 `input备份/`、`可用的已输出结果/`，它们不属于当前核心生成流程的直接读取入口。

## 解决方案与开发入口

- SDK 风格解决方案：`RealismPatchGenerator.slnx`
- 兼容保留解决方案：`Realism-patch-Generator-Csharp.sln`

## 发布

发布脚本：

- `scripts/build-release.ps1`

常见用法：

```powershell
.\scripts\build-release.ps1
.\scripts\build-release.ps1 -BuildBoth
.\scripts\build-release.ps1 -FrameworkDependent
```

## 文档索引

建议按下面顺序阅读：

### 1. 用户入门

- `docs/使用说明.md`：GUI / CLI 使用方式、Seed、例外物品与发包说明
- `docs/规则说明.md`：规则文件职责、GUI 分类与 JSON 结构映射

### 2. 规则指南

- `docs/武器属性规则指南.md`
- `docs/附件属性规则指南.md`
- `docs/弹药属性规则指南.md`
- `docs/装备属性规则指南.md`

### 3. 技术说明

- `docs/补丁生成流程说明.md`：输入识别、patch 构造、规则修正与 output 写出流程
- `docs/现实主义“Realism”装备补丁属性规则说明.md`：标准输出字段边界与字段语义

所有保留文档均提供对应英文副本（`*_en.md`）。

## 更新日志

- `CHANGELOG.md`
