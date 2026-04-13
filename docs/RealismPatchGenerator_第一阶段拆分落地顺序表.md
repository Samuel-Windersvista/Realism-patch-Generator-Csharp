# RealismPatchGenerator 第一阶段拆分落地顺序表

## 目标

第一阶段不追求一次性彻底重构，而是先把生成主链里最重、最独立、最适合后续性能优化的部分抽出来。

阶段目标：

- 保持现有 Generate 对外行为不变
- 降低 RealismPatchGenerator.cs 的职责密度
- 为后续性能优化建立清晰边界
- 优先拆分对性能收益最直接的模块

---

## 拆分原则

1. 先拆主链边界清晰的部分，不先碰规则推断大核心
2. 先搬方法，不急着大改算法
3. 每一步都要保证行为不变
4. 拆分顺序要服务后续性能优化
5. 优先减少未来做缓存、索引、流式输出时的改动阻力

---

## 第一阶段建议顺序

1. TemplateCatalog
2. PatchOutputPipeline
3. InputFormatRouter
4. ItemInfoFactory

完成以上四步后，再进入第二阶段拆规则层。

### 当前落地状态

- 已完成 `TemplateCatalog` 的首版接入，模板加载与模板索引构建已从主生成器主链中抽出。
- 已完成 `PatchOutputPipeline` 的首版接入，输出收集状态与落盘逻辑已形成独立边界。
- 已完成 `InputFormatRouter` 与 `PatchBuildRouter` 的首版接入，输入格式识别与格式分发已从主生成器主链中抽出。
- 已完成 `ItemInfoFactory` 的首版接入，标准模板、Moxo、WTT、RaidOverhaul、Mixed 及 fallback/bootstrap 的 `ItemInfo` 构造已迁出。
- 当前第一阶段的目标已达到“初版落地完成态”：主类已明显向流程编排器收缩，且现有 16 项测试持续通过。

---

## Step 1: TemplateCatalog

### 目标

把模板加载、模板索引、模板别名解析、模板选择从主类中抽离。

### 建议新增类

- TemplateCatalog

### 建议先接管的方法

- LoadAllTemplates
- TryResolveTemplateCloneByIdOrAlias
- TryResolveTemplateAlias
- ScoreCloneAliasMatch
- TokenizeCloneReference
- NormalizeCloneReferenceToken
- SelectTemplateData
- InferParentIdFromTemplateFile
- GetTemplateForParentId

### 建议保留在主类中的职责

- 仅保留调用 TemplateCatalog 的入口
- 不再直接持有模板细节处理逻辑

### 拆分收益

- 后续可以在这里做模板缓存
- 后续可以在这里减少重复 DeepClone
- 后续可以在这里做 alias 预分词索引
- 主类会立刻缩短一大段

### 注意事项

- 第一版不要改模板选择逻辑
- 第一版不要改 alias 匹配阈值和评分规则
- 第一版不要碰外部接口，只做内部搬运

---

## Step 2: PatchOutputPipeline

### 目标

把补丁收尾、分组、写出统一抽到输出管线中。

### 建议新增类

- PatchOutputPipeline

### 建议先接管的方法

- FinalizePatch
- ApplyItemException
- EnsureBasicFields
- MergeInputProperties
- PruneDisallowedOutputFields
- NormalizeStructuredOutput
- NormalizeAmmoOutputStructure
- AddToFilePatches
- StorePatchByPatchType
- SavePatches
- OrderedPatchGroup

### 建议保留在主类中的职责

- 主类只在单项 patch 构建完成后调用输出管线
- 不再直接管理 fileBasedPatches 和输出细节

### 拆分收益

- 后续可以直接在这里减少输出阶段重复拷贝
- 后续可以在这里改成流式 JSON 写出
- 后续可以更容易做 dry-run 和输出统计扩展

### 注意事项

- 第一版不要改输出文件命名规则
- 第一版不要改输出字段顺序
- 第一版只做职责转移，不做行为变化

---

## Step 3: InputFormatRouter

### 目标

把输入文件格式识别和按格式分发构建逻辑拆开。

### 建议新增类

- InputFormatRouter

### 建议先接管的方法

- DetectSupportedFileFormat
- TryBuildPatchForSupportedFormat
- IsRealismStandardTemplateFormat
- IsMoxoTemplateFormat
- IsWTTTemplateFormat
- IsMixedTemplateFormat
- IsRaidOverhaulTemplateFormat
- 所有 IsXxxTemplatesSourceFile 方法

### 可选内部结构

- 路由器只负责识别和分发
- 各格式 patch 构建方法先暂时仍留在主类
- 第二步再考虑继续抽成各格式 builder

### 拆分收益

- 主类中最长的格式判断和 switch 会明显收缩
- 后续并行化时更容易按文件或格式做边界控制
- 后续可以把各输入格式逐步演进为独立 builder

### 注意事项

- 第一版不要同时拆分所有 TryBuildXxxPatch 方法
- 先拆检测和路由，避免改动面过大
- 路由接口保持最小化

---

## Step 4: ItemInfoFactory

### 目标

把 ItemInfo 构建、字段提取、源字段过滤逻辑集中。

### 建议新增类

- ItemInfoFactory

### 建议先接管的方法

- ExtractItemInfo
- 所有 ExtractXxxItemInfo 重载
- ExtractProperties
- ExtractEffectiveInputFields
- TryGetRaidOverhaulOverrideProperties
- GetAllowedLegacySourceFields
- AddAllowedLegacySourceFields
- ResolveEffectiveModType
- EnrichItemInfoWithSourceContext
- CreateMixedBootstrapItemInfo
- CreateWTTBootstrapItemInfo
- CreateRaidOverhaulBootstrapItemInfo

### 拆分收益

- 后续做单项分析缓存时有天然入口
- 后续可以避免同一输入被多次重复解析
- 便于单测各种输入结构到 ItemInfo 的映射

### 注意事项

- 第一版不要改字段筛选规则
- 第一版不要改 SourceProperties 的生成语义
- 先确保行为完全等价

---

## 第一阶段不建议先拆的部分

以下部分暂时不作为第一阶段优先目标：

- ApplyRealismSanityCheck
- ApplyWeaponSanityCheck
- ApplyAttachmentSanityCheck
- ApplyGearSanityCheck
- ApplyAmmoProfileRanges
- InferWeaponProfile
- InferGearProfile
- InferModProfile
- 以及各类价格计算、档位推断、启发式判断

### 原因

- 规则层分支过多，单次改动风险高
- 和现有行为绑定太深
- 更适合作为第二阶段拆分目标
- 第一阶段先把主链边界理顺，再拆规则层更稳

---

## 第一阶段完成后的理想状态

完成四步后，RealismPatchGenerator 应尽量只保留这些职责：

- 初始化上下文
- 枚举输入文件
- 调用格式路由
- 调用 ItemInfoFactory
- 调用规则应用
- 调用输出管线
- 汇总 GenerationResult

换句话说，主类应从“全能实现类”变成“流程编排器”。

### 当前完成度判断

- 从既定四个拆分边界来看，第一阶段已完成首轮落地。
- 仍有少量辅助方法暂留在主类，但它们已不再构成第一阶段的主要阻塞。
- 后续若继续细抠第一阶段，也应只做小范围清理，不应再拖延第二阶段开始。

---

## 第二阶段预告

第一阶段完成后，再开始拆规则层，建议方向如下：

- PatchRuleApplier
- WeaponRuleEngine
- AttachmentRuleEngine
- GearRuleEngine
- AmmoRuleEngine
- ProfileInferenceService

第二阶段目标：

- 收敛重复推断
- 建立 PatchAnalysis 缓存
- 为性能优化准备 profile 缓存和字符串分析缓存

---

## 第一阶段完成后的性能优化切入点

完成第一阶段后，建议优先做以下优化：

1. 减少模板加载和输出阶段的重复 DeepClone
2. 为 clone alias 匹配建立预分词缓存
3. 为 profile 推断建立单项缓存
4. 视需要将输出改为流式 JSON 写出

---

## 验收标准

第一阶段完成后，应满足：

- Generate 对外调用方式不变
- 生成结果不变
- 现有测试继续通过
- 主类长度明显下降
- 模板、输入路由、ItemInfo、输出四块职责边界清晰
- 第二阶段性能优化具备明确落点

---

## 推荐执行策略

每一步按以下顺序推进：

1. 新建类，复制方法
2. 通过最小接口接回主类
3. 不做逻辑修改，只做职责迁移
4. 跑现有测试或最小生成验证
5. 确认行为一致后再进入下一步

避免在同一步里同时做：

- 拆分类
- 改算法
- 做性能优化
- 改命名和风格清理

应坚持“小步迁移，逐步验证”。

---

## 备注

本表用于第一阶段拆分备忘，不等同于最终架构设计。实际执行时，应以行为稳定和可验证性优先。