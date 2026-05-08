# RealismPatchGenerator 第二阶段拆分落地顺序表

## 目标

第二阶段不再优先拆“输入/模板/输出外壳”，而是开始拆真正控制补丁数值行为的核心规则层。

这一阶段的目标不是把所有规则都抽成完美架构，而是先把最容易反复修改、最影响性能定位、最需要独立验证的几块逻辑拆出来：

- 分析上下文与推断缓存
- profile 推断逻辑
- weapon / attachment / gear / ammo 四类规则执行逻辑
- 最后再把主类中的规则分发收口为一个统一入口

完成后，RealismPatchGenerator 应主要保留：

- 生成流程编排
- 模板与输入结果衔接
- 调用规则执行器
- 统一收集输出

而不再继续承担“大量 profile 推断 + 大量字段范围修正 + 大量价格规则计算”的实现细节。

---

## 拆分原则

第二阶段建议继续遵守以下原则：

1. 先拆“可独立验证的规则块”，不要先做全局抽象框架
2. 先拆“复用高、重复计算多”的逻辑，再拆单一品类规则
3. 先建立分析上下文，再迁移推断方法，否则会把重复字符串处理原样复制出去
4. 每次只迁移一类规则，不在同一步同时改算法和改结构
5. 主类对外行为保持不变，Generate 调用路径不变

第二阶段最重要的指导思想是：

不是为了“把方法搬出去”而拆分，而是为了让规则逻辑以后可以单测、缓存、局部优化、局部替换。

---

## 第二阶段建议顺序

建议顺序如下：

1. PatchAnalysisContextFactory
2. ProfileInferenceService
3. WeaponRuleEngine
4. AttachmentRuleEngine
5. GearRuleEngine
6. AmmoRuleEngine
7. PatchRuleApplier

这个顺序的原因是：

- 先把反复使用的分析上下文抽出来，后面的规则类才能共享同一份预处理结果
- 再把 profile 推断独立出去，四类规则引擎就可以复用统一入口
- 再按武器、附件、装备、弹药逐块迁移规则应用逻辑
- 最后才把 ApplyRealismSanityCheck 收口为统一规则分发器，避免一开始就改主流程

### 当前落地状态

- 第二阶段已开始进入代码落地，不再停留在文档规划阶段。
- 已新增 `PatchAnalysisContext` 与 `PatchAnalysisContextFactory` 首版实现。
- 当前 `InferWeaponProfile`、`InferGearProfile`、`InferAmmoProfile`、`InferAmmoSpecialProfile`、`InferModProfile` 已先接入统一分析上下文读取。
- 已新增 `ProfileInferenceService` 首版实现，并先接管 weapon / ammo 相关的 5 个 profile 推断入口。
- `ProfileInferenceService` 已继续扩展到 gear / mod 推断入口，Step 2 已完成首轮落地。
- 已新增 `WeaponRuleEngine` 首版实现，并已接管武器价格区间、评分链、默认 WeapType 以及 `ApplyWeaponSanityCheck` 主入口中的 refinement/主流程编排。
- 已新增 `AttachmentRuleEngine` 首版实现，并已接管 `ApplyAttachmentSanityCheck` 主入口、附件价格区间/评分链、源字段保留与按 profile 字段裁剪逻辑。
- 已新增 `GearRuleEngine` 首版实现，并已接管 `ApplyGearSanityCheck` 主入口、装备价格区间/评分链，以及 armor / carry / utility / mobility 四组评分逻辑。
- 已新增 `AmmoRuleEngine` 首版实现，并已接管 `ApplyAmmoProfileRanges` 主入口、穿深档位解析，以及 penetration tier / special profile 的叠加范围修正逻辑。
- 已新增 `PatchRuleApplier` 首版实现，并已接管 `ApplyRealismSanityCheck` 总入口、必需字段补全与预规则 heuristics。
- 本轮仍保持“只做数据准备边界，不改推断结论”的原则，现有 16 项测试持续通过。

---

## Step 1: PatchAnalysisContextFactory

### 目标

先把“每个物品在规则应用前都要重复提取的信息”收敛成一个分析上下文对象。

当前代码里大量方法会反复做这些事：

- 读取 Name / ShortName / ArmorClass / Caliber / AmmoCaliber
- 读取 TemplateFile / SourceFile / ParentId
- 把文本转小写
- 做 token 切分
- 拼接 armor / caliber / ammo variant 文本

这些重复工作是后续规则层性能优化最容易落地的切入点。

### 建议新增类

- PatchAnalysisContext
- PatchAnalysisContextFactory

PatchAnalysisContext 建议承载：

- 归一化后的名称文本
- 名称 tokens
- armor 文本
- ammo caliber 文本
- ammo variant 文本
- 归一化 parentId
- templateFile 文件名
- sourceFile 文件名
- 常用布尔判断结果或轻量派生字段

### 建议先接管的方法

优先把这些“信息准备型方法”所依赖的数据搬进上下文，而不是先大改规则算法：

- ExtractGearArmorClassText
- ExtractAmmoCaliberText
- ExtractAmmoVariantText
- NormalizeParentId 的调用结果
- 各种 GetLowerText + AlphaNumericTokenRegex 的重复结果

### 拆分收益

- 消除 profile 推断阶段的大量重复字符串处理
- 为后续缓存提供稳定载体
- 让推断逻辑从“直接操作 patch + itemInfo”转为“读取统一分析结果”
- 让后续单测可以直接构造分析上下文，减少测试样板

### 注意事项

- 这个步骤不要改推断结论，只做数据准备收拢
- 上下文是一次生成流程内的瞬时对象，不要把可变 patch 状态塞进去
- 若上下文要引用源字段，必须保留与 SourceProperties 一致的读取语义，不能破坏现有保留/夹紧逻辑

### 当前完成度判断

- Step 1 已完成首个可运行切片。
- Step 2 已完成首轮落地，weapon / ammo / gear / mod 的推断入口都已进入 `ProfileInferenceService`。
- Step 3 已完成第二个可运行切片，WeaponRuleEngine 已不只覆盖价格链，也已接住武器规则主入口。
- Step 4 已完成首个可运行切片，AttachmentRuleEngine 已接住附件规则主入口与价格链。
- Step 5 已完成首个可运行切片，GearRuleEngine 已接住装备规则主入口与价格链。
- Step 6 已完成首个可运行切片，AmmoRuleEngine 已接住弹药规则主入口与 tier/special 修正链。
- Step 7 已完成首个可运行切片，PatchRuleApplier 已接住总规则入口与规则前预处理。
- 第二阶段既定七个边界现已全部完成首轮落地，下一步可转入性能优化计划表中的基线测试、热点定位与定向优化。

---

## Step 2: ProfileInferenceService

### 目标

把各种 profile 推断从主类中完整抽离，形成统一的“分类/档位推断服务”。

这是第二阶段最关键的一步，因为 weapon / attachment / gear / ammo 四类规则都依赖它。

### 建议新增类

- ProfileInferenceService

如果你希望更细一点，也可以内部再分为：

- WeaponProfileInference
- AttachmentProfileInference
- GearProfileInference
- AmmoProfileInference

但第二阶段初期不建议先拆成四个公开服务，先保留一个统一入口更容易落地。

### 建议先接管的方法

- InferWeaponProfile
- InferWeaponCaliberProfile
- InferWeaponStockProfile
- InferGearProfile
- InferArmorPlateProfile
- InferBodyArmorProfile
- InferHelmetProfile
- InferFaceProtectionProfile
- InferBackpackProfile
- InferEyewearProfile
- InferChestRigProfile
- InferCosmeticGearProfile
- InferAmmoProfile
- InferAmmoSpecialProfile
- InferAmmoPenetrationTier
- InferModProfile
- InferModProfileFromTemplateFile
- InferModProfileFromNameFallback
- 各类 InferBarrelProfileFromName / InferHandguardProfileFromName / InferMagazineProfile / InferSightProfileFromName 等辅助方法

### 拆分收益

- 主类中最难读的一大块逻辑被整体移走
- 四类规则引擎以后都只依赖一个推断服务，不再直接散落调用几十个私有方法
- 以后如果要加缓存、加统计、加命中日志，入口会非常明确
- profile 推断错误可以独立做回归测试，不需要整条 Generate 链路都跑一遍

### 注意事项

- 先保留现有判断顺序，不要重排 if 链，否则很容易改变 profile 命中优先级
- 先保持规则数据读取方式不变，仍然依赖现有 rules 对象
- 迁移后要重点回归 attachment 与 gear，因为这两块的名称兜底判断最多，最容易出现行为漂移

---

## Step 3: WeaponRuleEngine

### 目标

把武器规则应用逻辑从主类中独立成第一个规则引擎。

之所以先拆武器，是因为它的结构最完整：

- 有明确的 profile 推断
- 有基础范围与 refinement range
- 有价格计算
- 有全局安全夹紧

它最适合作为规则引擎模板，给后续 attachment / gear / ammo 提供拆分范式。

### 建议新增类

- WeaponRuleEngine

### 建议先接管的方法

- ApplyWeaponSanityCheck
- ApplyWeaponPriceRule
- ResolveWeaponPriceRange
- CalculateWeaponPriceScore
- CalculateWeaponRecoilScore
- CalculateWeaponHandlingScore
- CalculateWeaponAccuracyScore
- CalculateWeaponRateScore
- CalculateWeaponCaliberPremium
- CalculateWeaponStockPremium
- CalculateWeaponWeightScore
- ApplyWeaponRefinementRanges
- GetDefaultWeaponTypeForProfile

### 拆分收益

- 最复杂的一类规则先形成稳定模板
- 价格计算和范围修正不再堆在主类里
- 后续可以独立优化 weapon profile 与 refinement 组合逻辑
- 更方便对 weapon 行为补测试

### 注意事项

- ApplyFieldClamps、ApplyNumericRanges、ApplyGlobalSafetyClamps 这类通用工具先不要急着搬散，可先继续由主类或共享 helper 提供
- preserveExistingValues 的语义必须保持不变
- 这一阶段先不改价格算法权重，只迁移位置

---

## Step 4: AttachmentRuleEngine

### 目标

把附件规则从主类中拆出来，并明确它与 profile 推断、源字段保留、结构字段移除之间的边界。

附件是第二阶段最需要谨慎拆分的一块，因为它的规则分支最多，也最容易和源字段保留逻辑互相影响。

### 建议新增类

- AttachmentRuleEngine

### 建议先接管的方法

- ApplyAttachmentSanityCheck
- ApplyAttachmentPriceRule
- ResolveAttachmentPriceRange
- CalculateAttachmentPriceScore
- RemoveAttachmentFieldsByProfile
- ApplyAttachmentPreservedSourceFields
- ResolveEffectiveModType
- 以及 attachment price score 相关辅助计算方法

### 拆分收益

- 把最复杂的 mod 规则分支从主类中剥离出去
- 以后新增 mod profile 时，只需要在 attachment 引擎局部修改
- 能更清楚地定位“结构字段补全”和“范围采样/夹紧”的责任边界
- 为后续 attachment profile 缓存、alias 缓存、源值保留优化提供稳定边界

### 注意事项

- 这一块必须特别关注 SourceProperties 的保留语义
- 像 gasblock、handguard 这类已验证过的保留字段逻辑，迁移时不能退化成重新采样
- 若某些辅助方法同时被 weapon / attachment 用到，先提成共享 helper，不要复制两份

---

## Step 5: GearRuleEngine

### 目标

把装备规则拆成独立引擎，隔离 armor、背包、胸挂、面罩、耳机等多种 gear profile 的范围和价格逻辑。

### 建议新增类

- GearRuleEngine

### 建议先接管的方法

- ApplyGearSanityCheck
- ApplyGearPriceRule
- ResolveGearPriceRange
- CalculateGearPriceScore
- CalculateArmorProtectionScore
- CalculateCarryCapacityScore
- CalculateGearUtilityScore
- CalculateMobilityBurdenScore
- GetArmorClassScore
- GetGridCellCapacity
- GetSlotCount

### 拆分收益

- gear 价格和范围逻辑可独立测试，不再被 attachment / weapon 噪音干扰
- 背包、护甲、胸挂等子类逻辑可以逐步细化，而不会继续膨胀主类
- 以后如果要单独优化 grid/slot 读取或 armor 文本判断，会有明确落点

### 注意事项

- gear profile 依赖的名称判断很多，迁移后要重点回归 armor vest、armor rig、backpack、eyewear 边界
- 不要在这一阶段顺手重做 gear 分类体系，先保证行为一致

---

## Step 6: AmmoRuleEngine

### 目标

把弹药规则抽离成独立引擎，尤其是 profile、penetration tier、special profile 叠加修正这一整块。

### 建议新增类

- AmmoRuleEngine

### 建议先接管的方法

- ApplyAmmoProfileRanges
- ExtractPenetrationValue
- 以及 ammo 规则相关的辅助范围修正逻辑

如果 Step 2 已经完成，那么 ammo profile / penetration tier / special profile 的推断应直接复用 ProfileInferenceService，而不是继续保留在引擎内部。

### 拆分收益

- ammo 规则会变成一个非常清晰的“基础范围 + tier 修正 + special 修正”的执行模块
- 后续若要针对弹药单独做批量性能分析，会很容易加统计点
- 以后要扩展新口径/新特种弹规则，也不会污染其他物品类别

### 注意事项

- AmmoRuleEngine 不要反向持有太多通用工具，避免把共享逻辑又吸回去
- 弹药字段边界值较多，迁移后应重点确认 ArmorDamage 和 malfunction 相关字段的夹紧语义没有变化

---

## Step 7: PatchRuleApplier

### 目标

当前面的上下文、推断服务、四类规则引擎都拆好之后，最后再把总入口 ApplyRealismSanityCheck 收口为统一规则分发器。

### 建议新增类

- PatchRuleApplier

### 建议先接管的方法

- ApplyRealismSanityCheck
- EnsureRequiredFields
- ApplyPreRuleHeuristics

PatchRuleApplier 的职责应尽量简单：

- 先做前置结构修复
- 根据 $type 判断 item 大类
- 调用对应规则引擎
- 最后做通用安全收尾

### 拆分收益

- 主类的规则控制流被正式抽空
- RealismPatchGenerator 会更像编排器，而不是规则实现体
- 后续若要接入统计、调试日志、性能计时点，PatchRuleApplier 会是最佳挂点

### 注意事项

- 不要在这个步骤里重新设计完整策略模式或 DI 容器
- 第二阶段的目标是把边界清出来，不是一次性做最终架构

---

## 第二阶段不建议先拆的部分

以下内容第二阶段不建议抢先处理：

- SampleRangeValue / ClampRangeValue / CreateNumericNode 这一类底层数值工具的全面抽象
- 所有 helper 方法一次性工具类化
- rules 数据结构本身的大重命名
- CLI / GUI 的依赖注入改造
- 全量性能算法重写

原因是这些改动都很容易和“规则职责迁移”缠在一起，导致验证面迅速扩大。

第二阶段应该先把规则边界切清楚，再决定哪些共享工具值得下沉，哪些热点需要重写。

---

## 第二阶段完成后的理想状态

第二阶段完成后，类职责大致应接近：

- RealismPatchGenerator：流程编排
- TemplateCatalog：模板读取与查找
- InputFormatRouter：输入识别与路由
- ItemInfoFactory：ItemInfo 构造
- PatchOutputPipeline：输出组织与落盘
- PatchAnalysisContextFactory：规则分析上下文准备
- ProfileInferenceService：profile 推断
- WeaponRuleEngine：武器规则执行
- AttachmentRuleEngine：附件规则执行
- GearRuleEngine：装备规则执行
- AmmoRuleEngine：弹药规则执行
- PatchRuleApplier：统一规则分发

这时主类长度应明显下降，而且“性能问题属于哪一层”“生成行为属于哪一层”会比现在清楚很多。

---

## 与性能优化的衔接

第二阶段做完后，最值得优先推进的性能优化通常会变成：

1. 对 PatchAnalysisContext 做一次生成链内缓存，避免重复构造
2. 对 ProfileInferenceService 的 token / keyword 判断建立轻量缓存或预分词索引
3. 对 attachment alias / profile 推断链做热点统计，确认是否需要字典化或预编译匹配
4. 评估规则引擎内部的 DeepClone、重复字段扫描、重复范围采样是否还能继续下沉优化

也就是说，第二阶段不是性能优化的对立面，而是把性能优化的落点提前准备好。

---

## 验收标准

第二阶段完成后，应满足：

- Generate 对外调用方式不变
- 生成结果不变
- 现有测试继续通过
- 主类中的 profile 推断与规则应用方法显著减少
- 规则错误可以更容易定位到具体引擎
- 后续性能优化可以明确落到“上下文、推断、某类规则引擎”之一

---

## 推荐执行策略

每一步按以下顺序推进：

1. 先新建类与最小接口
2. 把原方法原样迁移过去
3. 由主类调用新类，但不立即改算法
4. 跑现有测试或最小生成验证
5. 确认行为一致后，再进入下一步

应继续避免在同一步里同时做：

- 拆分类
- 改推断规则
- 改价格算法
- 改性能实现
- 改命名与风格清理

第二阶段尤其要坚持“迁移完成一个规则块，就验证一个规则块”。

---

## 备注

本表用于第二阶段拆分备忘，不等同于最终架构设计。实际执行时，优先级应始终服从两件事：

- 行为稳定
- 可验证性

如果第二阶段执行过程中发现某一块规则迁移后验证成本过高，应优先缩小拆分粒度，而不是强行一次迁完。