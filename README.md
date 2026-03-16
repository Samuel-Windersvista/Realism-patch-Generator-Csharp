# SPT现实主义数值范围编辑生成器 v0.9

这是可独立运行的现实主义数值生成器 C# 工程。当前核心迁移已经完成，CLI 与 GUI 都可直接运行，生成与检查未遵循规则物品流程都只依赖当前工程目录。

当前版本定位：v0.9

当前推荐先阅读以下文档：

- docs/使用说明.md：GUI 与 CLI 的日常使用方式
- docs/规则说明.md：当前规则文件结构、分类含义与编辑建议
- CHANGELOG.md：当前版本变更记录

当前已完成内容：

- 共享核心库，供 CLI 与 GUI 共用
- 模板加载与模板 ID 索引
- CURRENT_PATCH、STANDARD、CLONE、ITEMTOCLONE、VIR、TEMPLATE_ID 六类格式识别
- 最小补丁重建、输入属性合并、按源文件导出
- 独立数据根目录解析，仅使用 C# 工程自身目录下的 input、现实主义物品模板、output
- 武器规则迁移
	- 基础规则范围
	- 口径细分修正
	- 枪托形态修正
	- 最终落盘前夹紧与二次夹紧
- 附件规则迁移
	- 档位推断
	- 预处理启发式
	- clamp 规则
	- 必填字段补齐
	- 抑制器等特殊字段处理
- 装备规则迁移
	- gear profile 推断
	- 装备范围采样与安全夹紧
- 弹药规则迁移
	- ammo profile 推断
	- special profile 修正
	- penetration tier 推断与修正
- C# 原生未遵循规则物品检查
	- 可直接扫描 output 中的补丁结果
	- 可输出 JSON 检查报告到 audit_reports
	- 检查逻辑与 C# 生成器共用同一套规则推断能力

当前仍保留为后续阶段的内容：

- 更细粒度的数值一致性回归
- GUI 进一步打磨与交互细节优化
- 名称回归测试与更完整的自动化测试补齐

运行 CLI：

dotnet run --project RealismPatchGenerator.Cli

执行未遵循规则物品检查：

dotnet run --project RealismPatchGenerator.Cli -- audit

可选参数：

- --output-dir 自定义待检查输出目录
- --report-file 自定义报告输出路径
- --include-ok 将正常项也写入报告
- --include-template-exports 将 output 下所有 json 一起检查
- --fail-on-violations 存在违规字段时返回非 0 退出码

运行 GUI：

dotnet run --project RealismPatchGenerator.Gui

当前 C# 版默认使用自身目录下的数据：

- input
- 现实主义物品模板
- output
- audit_reports
- rules

因此它可以作为独立程序分发和运行，不再依赖外部工程的数据路径或外部审计脚本。
