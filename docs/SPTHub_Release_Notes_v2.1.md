# v2.1 Release Notes / v2.1 更新说明

## Highlights / 主要更新

- The project version is now v2.1.
- 项目版本已提升到 v2.1。
- WTT input handling now uses explicit subclass routing and includes SptBattlepass_templates.
- WTT 输入识别已完成子类化路由，并新增 SptBattlepass_templates 支持。

## GUI / 图形界面

- GUI title and localized App.Title text are aligned to v2.1.
- GUI 标题和多语言 App.Title 文本已统一到 v2.1。

## Packaging / 打包说明

- The release script default version is updated to 2.1.
- 发布脚本默认版本已更新为 2.1。
- Build both packages in one run: powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 2.1 -BuildBoth
- 一次同时打完整包和轻量包：powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1 -Version 2.1 -BuildBoth
- The release package remains GUI-first and bundles docs, rules, templates, and input sample directories.
- 发布包仍为 GUI 优先，附带 docs、规则、模板和 input 示例目录。

## Validation / 验证情况

- Core build succeeds and all tests pass.
- 核心编译通过，全部测试通过。
- Battlepass input source now produces patches successfully.
- Battlepass 输入源已可稳定识别并产出补丁。
