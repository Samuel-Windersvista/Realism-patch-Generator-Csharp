# v1.2 Release Notes / v1.2 更新说明

## Highlights / 主要更新

- Generation now uses a fresh runtime seed by default, so repeated runs can resample values within the configured ranges
- 生成器现在默认使用新的运行时随机 seed，因此重复生成时会在配置范围内重新采样
- CLI generation now supports a --seed parameter so the same run can be reproduced when needed
- CLI 生成命令现已支持 --seed 参数，便于在需要时复现同一轮生成结果
- GUI generation now supports an optional seed input, including quick clear and reuse of the most recently used generation seed
- GUI 现在也支持可选 seed 输入，并提供快速清空以及回填最近一次实际使用 seed 的能力
- Updated README and bilingual user-facing docs to explain resampling behavior, fixed-seed generation, and the GUI seed workflow
- README 与双语用户文档已同步更新，补充说明重新采样行为、固定 seed 生成方式以及 GUI 的 seed 工作流

## Validation / 验证情况

- Added an integration test to verify identical output when the same explicit seed is used
- 新增集成测试，用于验证在相同显式 seed 下生成结果完全一致
- Re-ran the existing test suite after the seed and documentation changes
- 在 seed 功能与文档更新后，现有测试套件已重新执行并通过

## Current Follow-Up / 当前状态

- GUI and CLI both support fixed-seed reproduction; future polishing can focus on ergonomics rather than missing capability
- GUI 与 CLI 均已具备固定 seed 复现能力，后续工作可以聚焦在易用性优化，而不是功能补缺