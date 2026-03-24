# SPT Realism Patch Generator v2.0

这是当前 C# 版 SPT Realism 补丁生成器仓库。

项目当前聚焦 3 件事：

- 基于模板和规则生成 Realism 补丁
- 在 GUI 中编辑规则范围
- 通过 item_exceptions.json 管理个别物品的最终例外覆盖

## 当前状态

- 当前版本：v2.0
- 当前主要入口：GUI、CLI
- 核心生成入口：RealismPatchGenerator.Core/RealismPatchGenerator.cs
- 当前有效生成大类：武器、附件、弹药、装备
- consumable 不在当前有效生成链路内

## 当前支持的输入情况

当前核心生成器直接支持以下输入物品格式：

1. RealismStandardTemplate
2. Moxo_Template
3. Mixed_templates

其中 Moxo_Template 的识别特征为：

- 条目包含 clone
- 同时包含 item 或 items
- 输出 Name 优先使用 locales.Name

Mixed_templates 的识别特征为：

- 同一个文件内部同时存在 clone + item/items 条目
- 同时也可能存在不带 clone 的 direct item/items 条目
- 无 clone 条目会按 item._parent 或 handbook 信息推断 Realism 基底补丁

仓库内的 input/user_templates 已统计出 4 类第三方输入源结构族：

1. WTT_templates
2. RaidOverhaul_templates
3. Mixed_templates
4. Moxo_Template

这 4 类结构族的统计结果见：

- docs/MOD物品数据结构统计报告.md

需要注意的是，“已统计识别到 4 类第三方源结构”不等于“当前核心生成器对 4 类都已实现完整直出支持”。README 这里描述的是当前程序实际行为，不再保留过时的兼容列表宣传口径。

## 当前生成规则

生成器当前遵循以下固定约束：

1. 输出条目顺序必须与输入源文件中的物品顺序一致
2. 只有 RealismStandardTemplate 且来源于 input/attatchments、input/gear、input/weapons 的输出文件保持原文件名
3. 其他当前支持的输出文件名继续在源文件名后追加 _realism_patch
4. output 不会在每次运行前整目录清空，只覆盖本次需要写出的目标文件
5. item_exceptions 会在自动规则处理完成后作为最终覆盖层应用

对 Moxo_Template，当前还额外遵循以下行为：

- 支持 clone 到模板库物品
- 支持同一源文件内 clone 到前面已生成的物品
- 只会保留克隆基底中存在的有效字段，不会把 Prefab 这类非 Realism 标准字段直接泄漏到输出补丁中

对 Mixed_templates，当前还额外遵循以下行为：

- 同文件中的 clone 条目优先复用 Moxo 路径处理
- clone 基底不可用时，会退回到 direct item/items 路径构造 Realism 基底补丁
- direct item/items 条目同样会过滤掉 Prefab 这类非 Realism 标准字段

## 目录说明

程序默认以仓库根目录作为数据根。当前运行时关键目录：

- input：输入源文件
- RealismItemTemplates：结构模板目录
- RealismItemRules：规则目录
- output：生成结果目录
- docs：说明文档

仓库里还保留了一些用于整理、备份、比对或调查的目录，例如中文模板目录、rules、artifacts、input备份、可用的已输出结果。这些目录可能对维护有帮助，但不代表都是当前核心生成流程的直接读取入口。

## 推荐使用方式

开发环境下启动 GUI：

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

开发环境下直接用 CLI 生成：

```powershell
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output
```

CLI 额外支持：

- `--seed <uint>`：指定随机种子
- `--logs`：在终端打印完整生成日志
- `--input-file <path>`：只处理指定输入文件，可重复传入
- `--input-dir <path>`：只处理指定输入目录，可重复传入

CLI 示例：

```powershell
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output --seed 123456 --logs
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output --input-file attatchments/HandguardTemplates.json
dotnet run --project .\RealismPatchGenerator.Cli\RealismPatchGenerator.Cli.csproj -- . .\output --input-dir user_templates
```

`--input-file` 和 `--input-dir` 既可以写成相对 `input` 根目录的路径，也可以写成带 `input/` 前缀的路径。

常规工作流：

1. 将输入 JSON 放入 input
2. 启动 GUI
3. 调整规则并保存到 RealismItemRules
4. 生成补丁到 output
5. 检查生成结果
6. 如有必要，再用例外物品功能写入 item_exceptions.json 做最终覆盖

## 规则与例外物品

主规则文件位于 RealismItemRules：

- weapon_rules.json
- attachment_rules.json
- ammo_rules.json
- gear_rules.json
- item_exceptions.json

其中：

- 前四个文件定义各大类的范围与修正规则
- item_exceptions.json 用于对具体 ItemID 做最终字段覆盖

例外物品的设计目标不是替代整类规则，而是处理少量确实需要单独落地的对象。

## 文档索引

如果要了解当前实现，请优先看这些文档：

- docs/使用说明.md
- docs/规则说明.md
- docs/补丁生成流程说明.md
- docs/MOD物品数据结构统计报告.md
- docs/规则文件与文档同步对照清单.md

## 发布说明

当前发布仍区分两种包：

- 完整包：自带运行时，适合普通用户直接使用
- 轻量包：不带运行时，要求目标机器预装匹配的 .NET Desktop Runtime

打包脚本位于：

- scripts/build-release.ps1
