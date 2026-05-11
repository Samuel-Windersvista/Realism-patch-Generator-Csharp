# 更新日志 / Changelog

> 中文版本在前，英文镜像版本在后。
> Chinese release history appears first, followed by an English mirror.

## 中文

## v2.7

### 本轮调整

- 收敛 `RealismPatchGenerator` 的边界整理成果，补齐 `TemplateRepository`、`TemplateMetadataCache`、`PatchStore`、`PatchTextInferenceHelpers` 与 `PatchFieldPermissionService` 的当前落地状态。
- 修复标准 `TemplateID` clone 在 alias 解析过宽时误命中无关模板的问题，避免 clone 继承到错误的武器分类。
- 修复 Requisitions / WTT fallback 场景下 `ChamberSpeed` 被错误过滤的问题，保证合法字段在 fallback attachment patch 中继续保留。
- 清理并更新 README、CHANGELOG 与核心 docs 到 v2.7，移除失效的 audit / `audit_reports` 叙述，并清理内部计划型文档不进入正式发版面。
- 发布脚本默认版本更新到 v2.7，发包时不再携带 `audit_reports` 空目录与内部计划文档。

### 本轮验证

- `dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --no-restore` 全量通过，结果为 `39/39`。
- 与 `TemplateID` clone alias、WTT/Requisitions fallback 相关的聚焦回归测试已补跑通过。

## v2.5

这次版本的重点不是增加新功能，而是把补丁生成链彻底分拆后，对主流程进行性能优化。现在生成器已经补上了基线计时、规则上下文缓存、alias 预索引、流式 JSON 读取和流式输出写出，在固定 seed 的小中大三档样本里都确认输出不变，同时整体耗时和内存分配都明显下降。
简单说，v2.5 是一个以性能优化为核心的发布版，重点是让生成更快、更省内存，而且结果仍然稳定可复现。

## v2.1

### 项目重构

- 重构整个项目，重新梳理生成器、规则加载、模板识别与输出落盘流程。
- 删除功能不完善的审计功能。

## v1.30.0

### 本轮调整

- 重新梳理规则的定制和输出逻辑，让输出结果更清晰、可控和稳定。
- 增强对现有MOD物品数据类型的兼容性。
- 增加兜底生成功能，对程序规则未能识别的武器装备MOD，也尽可能尝试进行基础兼容。
- 对检修功能进行改进，现在不但可以检查数值范围是否合规，还会检查物品属性是否合乎模板，极大程度避免数据结构上的错误。
- 清理旧代码。
- 全量复核 README 与 docs 文档集，统一到 GUI-only、模板驱动结构检修和当前发布方式。
- 默认发布脚本版本提升到 v1.30.0，并补充轻量运行时依赖包说明。

### 本轮修复

- 修复补丁的输出结构出错的问题
- 修复部分附件补丁字段丢失的问题
- 清理了目前所有的检修警告问题。

### 本轮验证

- 逐个检查补丁子类是否符合重新整理的规则逻辑
- 补充对应子类的生成与审计验证
- 完整测试复跑通过

## v1.22

### 新增

- GUI 新增可选 Seed 输入框，支持固定 seed 生成、清空 seed 回到随机模式，以及回填最近一次实际使用的 seed

### 调整

- 移除独立 CLI 项目入口，生成与审计流程统一收口到 GUI
- 发布脚本不再构建或打包 CLI 可执行文件
- 生成器默认改为每次运行使用新的运行时种子，重复生成时会在规则范围内重新采样
- 生成结果对象现在会显式返回本次实际使用的 seed，便于 GUI 和后续集成稳定复用
- README、使用说明和 SPTHub 文档同步改为 GUI-only 工作流，并补充随机采样、固定 seed 和 GUI seed 工作流说明
- 项目版本号更新到 v1.22
- 例外物品窗口的默认左右分栏与搜索区初始布局重新调整，减少首次打开时的拥挤感

### 修复

- 修复例外物品窗口中搜索提示文本与按钮在英文界面下的重叠问题
- 修复例外物品窗口首次打开时分栏尺寸过宽、需要手动拖动才能恢复到合适比例的问题
- 修复发布版中打开例外物品窗口时，SplitContainer 在初始化阶段因最小面板宽度与默认分栏距离冲突而崩溃的问题

### 验证

- 新增集成测试，确认相同 seed 会生成完全一致的输出

## v1.1

### 调整

- 弹药规则中的霰弹基础档拆分为 shotgun_shell_12g、shotgun_shell_20g、shotgun_shell_23x75
- 武器规则中的霰弹口径补修同步拆分为 12g、20g、23x75 三档
- GUI profile 名称、README 和主说明文档同步更新到霰弹分口径设计
- C# 版 output 改为保留输入源文件中的条目顺序，与 Python 版输出顺序对齐，便于人工核对
- README 与中英文使用说明补充当前输出顺序行为说明

### 验证

- 新增审计测试，确认 ammo_profile 与 weapon caliber_profile 能正确命中新霰弹档位
- 新增集成测试，确认生成结果会保留输入条目顺序
- 重新生成 output 并全量对比 Python 版，210 个同名 JSON 文件的 ItemID 顺序全部一致
- CLI 审计复跑通过，当前 output 审计结果为 0 违规、0 警告

### 修复

- 修正一处 user_templates 集成测试中的输出文件名预期，消除无关测试噪音

### 当前保留项

- 仍需基于实际生成结果继续微调 12g、20g、23x75 三档的手感范围

## v1.0

### 新增

- 例外物品管理窗口重构为按 Name 搜索 output 物品的工作流
- 例外物品编辑区支持按当前物品大类新增允许字段
- 新增字段编辑支持规则驱动的建议范围和保存时安全夹紧
- 例外物品窗口增加明确的“保存物品”流程
- 主文档集统一整理为当前 C# 项目的 v1.0 版本

### 调整

- “新增字段”按钮文案更新为“新增/修改字段”，与当前实际行为一致
- 例外物品窗口只搜索 output 结果，不再按旧说明搜索 input
- 输出与文档说明统一为“按目标文件覆盖写出，不整目录清空 output”
- docs 目录删除重复别名文档，只保留主文档集
- README 同步到 v1.0，并补充例外物品与审计现状说明

### 修复

- 修复例外物品窗口中备注框、字段按钮和保存按钮的布局可见性问题
- 修复装备类特殊字段候选与例外编辑流程的多处可用性问题

### 当前保留项

- GUI 自动化交互测试尚未补齐
- 仍需继续做更细的数值回归和界面打磨

## v0.9

### 新增

- 新增结构化规则范围编辑 GUI
- 新增左侧规则分类树与中部范围编辑表
- 新增字段说明面板与运行日志面板
- 新增 profile 中文友好名称映射
- 新增保存、重新加载、生成补丁、检查未遵循规则物品工作流
- 新增使用说明文档与规则说明文档
- 新增例外物品管理窗口，可按 ItemID 保存最终字段覆盖
- 新增 rules/item_exceptions.json 配置文件，并接入生成与检查链路
- 新增例外物品字段导入能力，可从 output 或 input 现有物品导入顶层字段
- 新增例外物品字段表格编辑器，替代整段原始 JSON 覆盖编辑
- 新增主界面例外物品只读总览页，便于快速查看当前配置命中项

### 调整

- GUI 名称更新为 SPT现实主义数值范围编辑生成器 v0.9
- 中文界面作为当前优先优化方向
- 将“执行审计”相关中文表述统一调整为“检查未遵循规则物品”
- 左侧规则分类栏宽度按当前中文阅读体验重新调整
- 规则范围表移除冗余的档位列，将空间留给核心编辑字段

### 保留问题 / 后续计划

- 英文界面仍保留基础支持，但尚未继续细修
- GUI 自动化交互测试尚未补齐
- 部分 profile 中文名称仍可继续按项目术语优化

---

## English Mirror

## v2.7

### Changes This Release

- Consolidated the boundary-refactor results around `RealismPatchGenerator`, and documented the currently landed state of `TemplateRepository`, `TemplateMetadataCache`, `PatchStore`, `PatchTextInferenceHelpers`, and `PatchFieldPermissionService`.
- Fixed overly broad alias resolution for standard `TemplateID` clones so unrelated templates are no longer matched, preventing clones from inheriting the wrong weapon classification.
- Fixed `ChamberSpeed` being filtered out in Requisitions / WTT fallback scenarios so valid fields remain in fallback attachment patches.
- Refreshed README, CHANGELOG, and core docs for v2.7; removed obsolete audit / `audit_reports` references; and removed internal planning docs from the formal release surface.
- Updated the release script default version to v2.7, and release packages no longer include the empty `audit_reports` directory or internal planning docs.

### Verification

- `dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --no-restore` passed in full, with a result of `39/39`.
- Focused regression tests covering `TemplateID` clone alias resolution and WTT/Requisitions fallback were re-run successfully.

## v2.5

This release was not about adding new features. Its focus was performance optimization after the patch-generation pipeline had been fully split into clearer stages. The generator now includes baseline timing, rule-context caching, alias pre-indexing, streaming JSON reads, and streaming output writing. With a fixed seed, small/medium/large sample runs all produced identical results, while total runtime and memory allocation both dropped significantly.

In short, v2.5 is a performance-focused release: faster generation, lower memory usage, and still stable, reproducible results.

## v2.1

### Project Refactor

- Refactored the project and reorganized the generator, rule loading, template recognition, and output writing flow.
- Removed the incomplete auditing functionality.

## v1.30.0

### Changes This Release

- Reworked rule customization and output logic so generated results are clearer, more controllable, and more stable.
- Improved compatibility with current MOD item data types.
- Added broader fallback generation so weapon and equipment mods that the rules do not fully recognize can still receive basic compatibility handling where possible.
- Improved the checking flow so it now validates not only numeric ranges, but also whether item properties match template structure, significantly reducing structural data errors.
- Cleaned up legacy code.
- Re-reviewed README and the docs set end to end and aligned them to the GUI-only workflow, template-driven structure checking, and the current release model.
- Raised the default release-script version to v1.30.0 and added notes about the lightweight runtime-dependent package.

### Fixes This Release

- Fixed incorrect output structure in generated patches.
- Fixed missing fields in some attachment patches.
- Cleared all current checking warnings.

### Verification

- Rechecked patch subclasses one by one against the reorganized rule logic.
- Added matching generation and checking validation for the affected subclasses.
- Re-ran the full test pass successfully.

## v1.22

### Added

- Added an optional Seed input box to the GUI, supporting fixed-seed generation, clearing the seed to return to random mode, and restoring the most recently used seed.

### Changes

- Removed the standalone CLI project entry point and consolidated generation and checking into the GUI.
- The release script no longer builds or packages the CLI executable.
- The generator now defaults to a fresh runtime seed for each run, so repeated generation resamples values within rule ranges.
- The generation result now explicitly returns the actual seed used in the run, making GUI reuse and downstream integration more stable.
- Updated README, the user guide, and the SPTHub docs to the GUI-only workflow, with notes covering random sampling, fixed seeds, and the GUI seed workflow.
- Updated the project version to v1.22.
- Adjusted the default split layout and initial search-area layout in the Item Exceptions window to reduce crowding on first open.

### Fixes

- Fixed overlap between the search hint text and buttons in the Item Exceptions window under the English UI.
- Fixed the issue where the Item Exceptions window initially opened with an overly wide split and needed manual dragging to recover a usable ratio.
- Fixed a release-build crash when opening the Item Exceptions window, caused by a SplitContainer initialization conflict between minimum panel width and the default splitter distance.

### Verification

- Added an integration test to confirm that identical seeds produce identical output.

## v1.1

### Changes

- Split the base shotgun ammo rules into `shotgun_shell_12g`, `shotgun_shell_20g`, and `shotgun_shell_23x75`.
- Split the shotgun caliber adjustment in weapon rules into separate 12g, 20g, and 23x75 profiles.
- Updated GUI profile names, README, and the main guide to match the shotgun-by-caliber design.
- Changed the C# output to preserve item order from input source files so it matches Python output ordering and is easier to review manually.
- Updated README and the Chinese/English user guides to document the current output ordering behavior.

### Verification

- Added audit tests to confirm that `ammo_profile` and weapon `caliber_profile` correctly map to the new shotgun profiles.
- Added an integration test to confirm that generated output preserves input item ordering.
- Regenerated output and compared it against the Python version; all 210 same-named JSON files matched in ItemID order.
- Re-ran CLI audit successfully; the current output audit result is 0 violations and 0 warnings.

### Fixes

- Corrected one expected output filename in a `user_templates` integration test to remove unrelated test noise.

### Remaining

- The 12g, 20g, and 23x75 handling ranges still need further tuning based on real generated results.

## v1.0

### Added

- Reworked the Item Exceptions management window into a workflow based on searching output items by Name.
- Added allowed-field insertion based on the current item category in the Item Exceptions editor.
- Added rule-driven suggested ranges and safe clamping when saving newly edited fields.
- Added an explicit "Save Item" flow in the Item Exceptions window.
- Reorganized the main documentation set into the current C# project's v1.0 docs.

### Changes

- Updated the "Add Field" button label to "Add/Modify Field" so it matches actual behavior.
- Changed the Item Exceptions window to search only output results instead of input as older docs described.
- Unified output and documentation wording to clarify that target files are overwritten without clearing the whole output directory.
- Removed duplicate alias docs from `docs/` and kept only the primary documentation set.
- Updated README to v1.0 and added notes about Item Exceptions and the current checking status.

### Fixes

- Fixed layout and visibility issues around the remarks box, field buttons, and save button in the Item Exceptions window.
- Fixed multiple usability issues around gear-specific field candidates and the Item Exceptions editing flow.

### Remaining

- GUI automation interaction tests are still missing.
- More detailed numeric regression and UI polish are still needed.

## v0.9

### Added

- Added a structured GUI for editing rule ranges.
- Added a left-side rule category tree and a center range-editing table.
- Added a field description panel and a runtime log panel.
- Added user-friendly Chinese mappings for profile names.
- Added workflows for save, reload, generate patches, and checking items that do not follow rules.
- Added the user guide and rule guide.
- Added an Item Exceptions management window that stores final field overrides by ItemID.
- Added the `rules/item_exceptions.json` configuration file and integrated it into generation and checking.
- Added import support for Item Exception fields from existing top-level fields in output or input items.
- Added a table-based Item Exception field editor to replace raw JSON block editing.
- Added a read-only Item Exceptions overview page on the main window for quick inspection of current configured hits.

### Changes

- Updated the GUI name to "SPT Realism Value Range Editor Generator v0.9".
- Set the Chinese UI as the current primary optimization direction.
- Unified Chinese wording around checking items that do not follow rules.
- Adjusted the width of the left rule-category pane for the current Chinese reading experience.
- Removed the redundant level column from the rule range table and gave that space back to core editing fields.

### Remaining Issues / Next Steps

- The English UI still has basic support, but has not received further polish.
- GUI automation interaction tests are still missing.
- Some Chinese profile names can still be improved to better match project terminology.
