# v1.30.0 Release Notes / v1.30.0 更新说明

## Highlights / 主要更新

- The project version is now v1.30.0, and the main docs set has been re-synchronized to the current GUI-only workflow.
- 项目版本已提升到 v1.30.0，主文档集也已重新同步到当前 GUI-only 工作流。
- Audit now checks both numeric ranges and template-driven output structure for weapons, attachments, ammo, and gear.
- 检修现在会同时检查数值范围与模板驱动结构，覆盖武器、附件、弹药、装备四大类。
- Weapon fallback generation has been expanded, and more partially covered attachment families now participate in structure audit.
- 武器兜底生成骨架已补齐，更多此前只做部分覆盖的附件子类也纳入了结构检修。
- The repository has been cleaned up to remove historical output samples and temporary build directories that are unrelated to the current delivery flow.
- 仓库已清理掉与当前交付流程无关的历史输出样本和临时构建目录。

## Packaging / 打包说明

- The release script now defaults to v1.30.0.
- 发布脚本默认版本已更新为 v1.30.0。
- Full package: larger, but no preinstalled runtime is required on the target machine.
- 完整包：体积更大，但目标机器不需要预装运行时。
- Lightweight package: does not bundle the runtime, so it is smaller but requires the matching .NET Desktop Runtime.
- 轻量包：不再携带运行时，因此体积更小，但要求目标机器已安装匹配的 .NET Desktop Runtime。

## Validation / 验证情况

- Existing generation and audit regression tests were re-run after the structure-audit and fallback updates.
- 结构检修与兜底生成更新后，相关生成与审计回归测试已重新执行。
- The repository was then repackaged using the framework-dependent release mode for lightweight distribution.
- 随后又按 framework-dependent 方式重新打包，生成了轻量发行包。