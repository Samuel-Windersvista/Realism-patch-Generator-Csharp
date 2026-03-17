# SPT Realism Value Range Generator v1.2

SPT Realism Value Range Generator v1.2 is a toolset designed specifically for SPT Realism Mod 1.6.4. It is used to edit realism value rules, generate realism patches, check generated results, and apply field-level exceptions to specific items.

SPT 现实主义数值范围编辑生成器 v1.2 是专门为SPT Realism Mod 1.6.4设计的工具集，用于编辑现实主义数值规则、生成现实主义补丁、检查生成结果，并能对特定物品应用字段级例外覆盖。

This tool brings the GUI editor, CLI generator, rule files, item exception management, and output checking together into one program. It is intended for users who want to maintain realism value rules in a more structured way, generate stable patch results, and continue verifying whether the generated output still follows the expected ranges.

本工具将 GUI 编辑器、CLI 生成器、规则文件、例外物品管理和输出检查统一收拢到同一个程序中。它适合希望以更结构化方式维护现实主义数值规则、生成稳定补丁结果，并在生成后继续验证输出是否遵循预期范围的用户。


## Project Overview / 项目概览

SPT Realism Value Range Generator focuses on the full rule-to-output workflow for realism values:

SPT 现实主义数值范围编辑生成器聚焦于“规则到输出”的完整现实主义数值工作流：

- Edit rule ranges for weapons, attachments, ammo, and gear in the GUI.
- 在 GUI 中编辑武器、附件、弹药、装备四大类规则范围。
- Generate patch output from either the GUI or the CLI.
- 通过 GUI 或 CLI 生成补丁输出。
- Check the generated output against the current rule files.
- 使用当前规则文件对生成结果做规则检查。
- Apply field-level exceptions to individual items through the Item Exceptions workflow.
- 通过例外物品功能对单个物品应用字段级例外覆盖。

## Main Capabilities / 主要能力

- GUI-based editing for weapon, attachment, ammo, and gear rules.
- 图形化编辑武器、附件、弹药、装备四大类规则。
- CLI generation and checking workflow for batch use.
- 支持命令行批量生成与检查工作流。
- Fixed-random-seed generation in both GUI and CLI for reproducible runs.
- GUI 与 CLI 均支持固定随机种子生成，可用于复现同一轮结果。
- GUI controls for clearing the current seed, returning to random mode, and quickly reusing the most recently used seed.
- GUI 提供对种子的清空、回到随机模式以及回填最近一次种子的交互功能。
- Item-specific exception overrides through rules/item_exceptions.json.
- 支持通过 rules/item_exceptions.json 对个别物品做字段级例外覆盖。
- Support input structures commonly used by mods built around six code-style patterns: CURRENT_PATCH, STANDARD, CLONE, ITEMTOCLONE, VIR, and TEMPLATE_ID.
- 支持 CURRENT_PATCH、STANDARD、CLONE、ITEMTOCLONE、VIR、TEMPLATE_ID 6种代码习惯的MOD的输入结构。

## Typical Workflow / 典型使用流程

1. Put the JSON files you want to turn into patches into the input folder.
1. 将需要生成的补丁的JSON 文件放入 input文件夹。
1. Open the GUI to review or edit rule ranges.
1. 打开 GUI 检查或调整规则范围。
1. Save changes back into the rules directory.
1. 将修改保存回 rules 目录。
1. Run generation from the GUI or CLI.
1. 通过 GUI 或 CLI 执行生成。
1. Review generated files under output.
1. 在 output 中检查生成结果。
1. If you need a rule-violation report, run the checking step.
1. 如果需要规则违规报告，可以执行检查。
1. For a small number of structurally unusual items, use the Item Exceptions feature for targeted handling.
1. 对少量结构特殊的物品，再使用 例外物品功能做定向处理。

## Seed and Reproducibility / 随机 Seed 与结果复现

By default, the generator uses a fresh runtime seed, so the same item will show different numeric values across repeated runs, while those values still remain inside the configured ranges.

默认情况下，生成器会使用新的运行时种子，因此同一物品在多次生成之间会出现不同的数值，只是这些数值仍然保持在配置好的数值范围内。

When you need to reproduce a specific run:

当你需要复现某一轮生成结果时：

- CLI supports --seed for fixed-seed generation.
- CLI 可通过 --seed 指定固定 seed。
- GUI provides an optional numeric seed input box.
- GUI 提供可选的种子数值输入框。
- GUI can clear the current seed to return to random generation.
- GUI 可清空当前种子数值，回到随机生成模式。
- GUI can also restore the most recently used seed value with one click.
- GUI 还可以一键回填最近一次实际使用的种子数值。
