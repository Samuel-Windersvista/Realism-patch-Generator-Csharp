# SPT现实主义数值范围编辑生成器 v1.0

这是当前可独立运行的 C# 版现实主义数值生成器工程。GUI、CLI、规则编辑、例外物品管理和输出审计现在都集中在同一个 .NET 解决方案里。

当前版本定位：v1.0

## 当前推荐先读

- docs/使用说明.md：日常 GUI 与 CLI 用法
- docs/规则说明.md：rules 目录结构与 GUI 分类映射
- docs/现实主义数值生成器快速入门.md：最短执行流程
- CHANGELOG.md：版本变更记录

## 当前已经具备的能力

- 共享 Core 库，供 GUI 与 CLI 共用
- 六类输入格式识别：CURRENT_PATCH、STANDARD、CLONE、ITEMTOCLONE、VIR、TEMPLATE_ID
- 模板加载、模板 ID 索引、最小补丁重建、输入属性合并
- 武器、附件、弹药、装备四大类规则加载与应用
- 生成后按当前目标文件覆盖写回 output，而不是整目录清空
- 原生 C# 审计链路，可直接检查 output 中的违规字段并输出到 audit_reports
- 例外物品管理正式接入生成与审计流程

## 例外物品功能现状

当前“例外物品”窗口支持：

- 按 Name 只搜索 output 中已经生成出的物品
- 载入当前物品的真实顶层字段
- 在同一编辑区内新增、修改、删除字段
- 用“新增/修改字段”把当前编辑器里的字段和值写回字段列表
- 用“保存物品”把整条例外配置写入 rules/item_exceptions.json
- 对装备类字段提供基于当前规则的建议范围与安全夹紧
- 生成时在最终落盘前应用覆盖
- 审计时只对明确覆盖的字段做定向豁免

## 运行方式

运行 GUI：

```powershell
dotnet run --project RealismPatchGenerator.Gui
```

运行生成：

```powershell
dotnet run --project RealismPatchGenerator.Cli
```

执行审计：

```powershell
dotnet run --project RealismPatchGenerator.Cli -- audit
```

常用审计参数：

- --output-dir：指定待检查输出目录
- --report-file：指定报告输出路径
- --include-ok：把正常项也写入报告
- --include-template-exports：把 output 下所有 json 一起纳入检查
- --fail-on-violations：存在违规字段时返回非 0 退出码

## 默认目录

程序默认使用仓库根目录下这些数据目录：

- input
- 现实主义物品模板
- output
- audit_reports
- rules

## 当前约定

- consumable 与普通 cosmetic 不作为规则审计主范围
- attachment 中的 mod_profile_unresolved 不计入附件审计噪音
- 例外物品优先以 output 中真实生成出的对象为基线编辑

## 当前仍保留的后续项

- 更细粒度的数值一致性回归
- GUI 交互细节继续打磨
- 更完整的自动化测试补齐
