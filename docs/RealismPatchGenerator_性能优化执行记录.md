# RealismPatchGenerator 性能优化执行记录

> 注：本文仅保留历史性能优化结果、实测数据与执行结论；当前候选优化建议请参考 `docs/生成效率改进建议.md`，当前执行计划与顺序请参考 `docs/RealismPatchGenerator_性能优化计划表.md`。

## 2026-05-09：当前 P0 串行热点治理推进记录

### P0 / 任务 1：TemplateCatalog 去重克隆与双份存储收敛

- 实施内容：`TemplateCatalog.Load()` 中 `templateById[pair.Key]` 不再对同一模板执行第二次 `DeepClone()`，改为复用 `byId[pair.Key]` 已加载实例。
- 回归补充：新增 `TemplateCatalogTests.Load_ReusesSingleLoadedTemplateInstanceAcrossIndexes`，约束模板索引复用行为。
- 结论：当前切片已完成，后续继续以“模板源只读、patch 副本可变”为边界推进。

### P0 / 任务 2：模板元数据缓存

- 实施内容：在 `RealismPatchGenerator` 中新增 `isWeaponCache` 与 `templateAllowedFieldMapCache`，缓存 `IsWeapon(string? parentId)` 和 `GetAllowedOutputFieldMap(...)` 的高频只读结果。
- 回归补充：新增 `RuleDataSynchronizationTests.IsWeapon_CachesParentIdResult` 与 `RuleDataSynchronizationTests.IsWeapon_CachesMissingParentIdAsFalse`。
- 结论：当前切片已完成，缓存命中与 miss 路径都已有自动化回归约束。

### P0 / 任务 3：Profile 推断匹配优化（第一子切片）

- 实施内容：`ProfileInferenceService.InferWeaponProfile()` 将 `pistol` / `handgun` 的 `context.Name.Contains(...)` 收敛为 `context.NameTokens.Contains(...)`，消除 `Pistolero Rifle` 这类 substring 误判。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlyKeyword_DoesNotOverrideRealTokenProfile`，确认旧逻辑会把 `Test Pistolero Rifle` 错判为 `pistol`；随后以最小改动切换到 token 命中并重新验证通过。
- 代码评审：已完成针对该子切片的独立 review，结论为“可继续推进”；review 同时建议后续评估 `InferWeaponCaliberProfile()` 与 `InferWeaponStockProfile()` 中的同类 substring 热点是否也值得按 token 方式收敛。
- 结论：当前只确认了语义正确性与无回归，不在没有新基线数据的前提下提前宣称“已有可测性能收益”。

### P0 / 任务 3：Profile 推断匹配优化（第二子切片）

- 实施内容：`ProfileInferenceService.InferWeaponStockProfile()` 将 `pistol` / `machine pistol` / `stockless` 的名称 substring 判断收敛为 token 命中，避免 `Stocklesser Rifle` 这类名称被错误识别为 `stockless`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlyStockKeyword_DoesNotForceStocklessProfile`，确认旧逻辑下输出缺少 `BaseReloadSpeedMulti`（说明落入 `stockless` 而非 `fixed_stock`）；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 stock profile 子切片已与第一子切片一起纳入自动化回归，但仍未进行新的 Release CLI 三档性能复测。

### P0 / 任务 3：Profile 推断匹配优化（第三子切片）

- 实施内容：`ProfileInferenceService.InferWeaponCaliberProfile()` 将 `pistol` / `handgun` 的名称 substring fallback 判断收敛为 token 命中，避免 `Pistolero Rifle` 这类名称被错误识别为 `pistol_caliber`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlyCaliberKeyword_DoesNotForcePistolCaliberProfile`，确认旧逻辑会把 `Velocity` 错误补成 `0`；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 caliber profile 子切片已纳入自动化回归，P0 / 任务 3 仍在继续，但尚未做新的 Release CLI 三档性能复测。

### P0 / 任务 3：Profile 推断匹配优化（第四子切片）

- 实施内容：`ProfileInferenceService.InferWeaponProfile()` 将 `shotgun` 的名称 substring 判断收敛为 token 命中，避免 `Shotgunner Rifle` 这类名称被错误识别为 `shotgun`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlyShotgunKeyword_DoesNotOverrideAssaultProfile`，确认旧逻辑会把 `WeapType` 错判为 `shotgun`；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 shotgun 子切片已纳入自动化回归，P0 / 任务 3 仍在继续，但尚未做新的 Release CLI 三档性能复测。

### P0 / 任务 3：Profile 推断匹配优化（第五子切片）

- 实施内容：`ProfileInferenceService.InferWeaponProfile()` 将 `launcher` / `grenade launcher` / `m203` / `gp25` / `ubgl` 的名称 substring 判断收敛为 token 命中，避免 `Relauncher Tool` 这类名称被错误识别为 `launcher`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlyLauncherKeyword_DoesNotOverrideProfile`，确认旧逻辑会把 `WeapType` 错判为 `launcher`；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 launcher 子切片已纳入自动化回归，P0 / 任务 3 仍在继续，但尚未做新的 Release CLI 三档性能复测。

### P0 / 任务 3：Profile 推断匹配优化（第六子切片）

- 实施内容：`ProfileInferenceService.InferWeaponProfile()` 将 `sniper` / `marksman` / `dmr` / `anti-materiel` / `anti materiel` 的名称 substring 判断收敛为 token 命中，同时保留 `狙击` 的 substring 判断，避免 `Gunsniper Rifle` 这类名称被错误识别为 `sniper`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlySniperKeyword_DoesNotOverrideAssaultProfile`，确认旧逻辑会把 `WeapType` 错判为 `sniper`；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 sniper 子切片已纳入自动化回归，P0 / 任务 3 已只剩高风险的 assault/rifle catch-all 是否继续收敛这一决策点。

### P0 / 任务 3：Profile 推断匹配优化（第七子切片）

- 实施内容：`ProfileInferenceService.InferWeaponProfile()` 将 `smg` 的名称 substring 判断收敛为 token 命中，避免 `Cosmg Rifle` 这类名称被错误识别为 `smg`。
- TDD 过程：先新增失败用例 `RuleDataSynchronizationTests.WeaponName_SubstringOnlySmgKeyword_DoesNotOverrideAssaultProfile`，确认旧逻辑会把 `WeapType` 错判为 `smg`；随后以最小改动改为 token 命中并重新验证通过。
- 结论：当前 smg 子切片已纳入自动化回归；`InferWeaponProfile()` 中剩余未收敛的主要名称分支只剩高风险的 assault/rifle catch-all，当前保持不动。

### 当前验证状态

- 聚焦 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlyKeyword_DoesNotOverrideRealTokenProfile"`
  - 结果：失败，实际 `WeapType` 为 `pistol`，证明旧逻辑存在 substring 误判。
- 聚焦 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第二子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlyStockKeyword_DoesNotForceStocklessProfile"`
  - 结果：失败，`BaseReloadSpeedMulti` 为空，证明旧逻辑把 `Test Stocklesser Rifle` 错归到了 `stockless`。
- 第二子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第三子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlyCaliberKeyword_DoesNotForcePistolCaliberProfile"`
  - 结果：失败，`Velocity` 被错误写成 `0`，证明旧逻辑把 `Test Pistolero Rifle` 错归到了 `pistol_caliber`。
- 第三子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第四子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlyShotgunKeyword_DoesNotOverrideAssaultProfile"`
  - 结果：失败，`WeapType` 被错误写成 `shotgun`，证明旧逻辑把 `Test Shotgunner Rifle` 错归到了 `shotgun`。
- 第四子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第五子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlyLauncherKeyword_DoesNotOverrideProfile"`
  - 结果：失败，`WeapType` 被错误写成 `launcher`，证明旧逻辑把 `Test Relauncher Tool` 错归到了 `launcher`。
- 第五子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第六子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlySniperKeyword_DoesNotOverrideAssaultProfile"`
  - 结果：失败，`WeapType` 被错误写成 `sniper`，证明旧逻辑把 `Test Gunsniper Rifle` 错归到了 `sniper`。
- 第六子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 第七子切片 RED：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "WeaponName_SubstringOnlySmgKeyword_DoesNotOverrideAssaultProfile"`
  - 结果：失败，`WeapType` 被错误写成 `smg`，证明旧逻辑把 `Test Cosmg Rifle` 错归到了 `smg`。
- 第七子切片 GREEN：同一命令在修复后重新执行。
  - 结果：通过。
- 当前全量回归：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
  - 结果：26/26 通过。
- 未完成项：Release CLI 小/中/大三档性能复测尚未重新执行，因此本轮记录暂不补写新的耗时与分配量对比表。
## 说明

本次记录对应第二阶段拆分完成后的首轮性能优化落地。

执行原则：

- 先补基线计时与固定样本
- 再做低风险高收益优化
- 最后验证输出完全一致，而不是只看耗时数字

固定参数：

- 运行方式：Release CLI
- Seed：123456
- 一致性验证：基线输出与优化后输出逐文件 SHA256 对比，三档样本均为 NO_DIFF

## 基线与优化后结果

### 2026-05-09 本轮 P0 收口复测（当前工作区 vs HEAD 基线快照）

- 运行方式：Release CLI
- Seed：123456
- 基线来源：`HEAD` 干净快照（`8d7603756591513b4e342f7d3647966e640cef1d`）
- 一致性验证：small / medium / large 三档输出逐文件 SHA256 对比均为 `NO_DIFF`
- 自动化回归：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"` => `26/26` 通过

#### 小样本

- 样本：`user_templates/[2]新物品-竞技场赛季奖励-SPT Battlepass.json`
- 总耗时：196.31 ms -> 196.04 ms，下降 0.14%
- 模板加载：125.54 ms -> 129.08 ms，上升 2.82%
- 文件处理：62.18 ms -> 58.85 ms，下降 5.36%
- 规则执行：28.86 ms -> 27.45 ms，下降 4.89%
- 输出写出：5.74 ms -> 5.03 ms，下降 12.37%
- 分配量：34.24 MB -> 34.16 MB，下降 0.23%

#### 中样本

- 样本：`input/user_templates` 整个子目录
- 总耗时：1632.09 ms -> 1019.75 ms，下降 37.52%
- 模板加载：125.46 ms -> 125.76 ms，上升 0.24%
- 文件处理：1368.64 ms -> 764.37 ms，下降 44.15%
- 规则执行：217.19 ms -> 211.39 ms，下降 2.67%
- 输出写出：134.58 ms -> 126.99 ms，下降 5.64%
- 分配量：193.09 MB -> 141.46 MB，下降 26.74%

#### 大样本

- 样本：完整 `input` 目录
- 总耗时：3180.82 ms -> 1645.59 ms，下降 48.27%
- 模板加载：128.67 ms -> 127.83 ms，下降 0.65%
- 文件处理：2897.63 ms -> 1356.60 ms，下降 53.18%
- 规则执行：438.70 ms -> 419.70 ms，下降 4.33%
- 输出写出：151.82 ms -> 159.03 ms，上升 4.75%
- 分配量：399.58 MB -> 261.88 MB，下降 34.46%

#### 收口结论

- P0 / 任务 1~3 的当前实施集合在三档样本上都保持了输出一致性（`NO_DIFF`）。
- P0 / 任务 3 的 token 收敛主要带来“去误判 + 小幅规则阶段下降”的收益，未引入可见行为漂移。
- 输出阶段在最新大样本复测里仅为 `159.03 ms / 1645.59 ms ≈ 9.66%`，占比仍然不高；因此 **暂不建议立刻进入 P1 / 任务 4（输出写入并行化）**。
- 更合理的下一步是先评估 `P1 / 任务 5（模板 I/O 并行化）` 或转入 `P2` 前置边界梳理，而不是为占比不足 10% 的输出阶段先引入并行复杂度。

### P2 / 任务 6：第一子切片（单文件处理上下文边界整理）

- 实施内容：新增 `FileProcessingContext`，将 `ProcessItemFile` 内的 `processed`、`processedCount`、`sourceKey`、`inputFormat` 归并为显式上下文，并让 `ProcessSingleItem` 经由该上下文执行业务。
- 回归补充：新增多 item 文件输出顺序与 disabled item 共存场景测试。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~MultiItemFile_GenerationPreservesPerFileOutputAndItemOrder"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~DisabledItem_IsSkippedWithoutBreakingSiblingOutputInSameFile"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~MultiItemFile_GenerationPreservesPerFileOutputAndItemOrder|FullyQualifiedName~DisabledItem_IsSkippedWithoutBreakingSiblingOutputInSameFile"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 说明：本子切片属于边界整理与可回归性增强，未重新执行新的 Release CLI 小 / 中 / 大三档性能复测；其验证依据是串行行为保持不变的自动化测试，而不是新的性能对比表。
- 结论：当前仍保持串行执行，但单文件处理路径的可变边界比之前更清晰，为后续 RNG 隔离与输出合并策略提供切入点。

### P2 / 任务 6：第二子切片（RNG 显式依赖边界整理）

- 实施内容：将现有 `CompatibleRandom` 从 `RealismPatchGenerator` 的隐式全局访问，改为经由 `FileProcessingContext`、`FinalizePatch`、`PatchRuleApplier` 与 `PatchRuleContext` 显式传递；`SampleRangeValue` 与 `ApplyNumericRanges` 新增 RNG 显式重载，但当前仍复用同一串行 RNG 实例，不改变既有输出序列。
- 回归补充：新增 RNG 显式采样 seam 测试，以及固定 seed 下重复生成输出一致性测试。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~SampleRangeValue_UsesProvidedRandomInstance|FullyQualifiedName~SameSeed_GenerateTwice_ProducesIdenticalRangeDrivenOutput"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片只做依赖边界显式化，不引入新的 per-file / per-item seed 策略，为后续真正的 RNG ownership 替换保留稳定切入点。

### P2 / 任务 6：第三子切片（单文件输出累积与显式合并）

- 实施内容：将 `patchOutputBuffer` 的写入从 `FinalizePatch(...)` 期间的共享增量写入，调整为 `FileProcessingContext` 内的单文件局部累积，并在 `ProcessItemFile(...)` 结束后一次性按原顺序 flush 回全局输出缓冲。
- 回归补充：新增 cross-file clone 场景与 multi-item 文件输出顺序场景测试。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~CrossFileCloneResolution_RemainsValidWhenOutputFlushIsDeferredPerFile|FullyQualifiedName~MultiItemFile_ItemOrderRemainsStableAfterPerFileBatchFlush"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片仅把输出累积边界显式化，不改变串行顺序与 clone 解析口径，为后续输出合并策略与文件级并行预演提供稳定接缝。

### P2 / 任务 6：第四子切片（PatchStore 共享注册表边界整理）

- 实施内容：将 `weaponPatches`、`attachmentPatches`、`ammoPatches`、`gearPatches`、`consumablePatches` 与 `generatedItemInfoById` 收敛到新的 `PatchStore` 中，并由其统一承担 patch 存储、item info 存储、clone 读取与统计快照职责。
- 回归补充：新增 mixed-type patch lookup 场景测试，并继续保持 cross-file clone regression 绿灯。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~StoredPatches_FromMultipleTypes_RemainResolvableByItemId|FullyQualifiedName~CrossFileCloneResolution_RemainsValidWhenOutputFlushIsDeferredPerFile"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片仅把跨文件 patch / info 共享依赖显式化，不改变串行查找顺序与 clone 语义，为后续并行化前的共享状态替换或同步策略提供稳定边界。

### P2 / 任务 6：第五子切片（TemplateMetadataCache 边界整理）

- 实施内容：将 alias hit/miss 缓存、allowed field map 缓存、`IsWeapon` 缓存以及 alias token index 收敛到新的 `TemplateMetadataCache` 中；`RealismPatchGenerator` 保留模板源与评分逻辑，但不再直接拥有这些可变缓存容器。
- 回归补充：新增 alias cache hit regression，并继续保持 `IsWeapon` cache regressions 绿灯。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~TryResolveTemplateCloneByIdOrAlias_CachesResolvedAliasResult|FullyQualifiedName~IsWeapon_CachesParentIdResult|FullyQualifiedName~IsWeapon_CachesMissingParentIdAsFalse"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片仅把跨 item 的模板元数据缓存显式化，不改变串行 cache-warming 语义，为后续缓存同步策略或更细粒度上下文拆分提供稳定边界。

### P2 / 任务 6：第六子切片（TemplateRepository 边界整理）

- 实施内容：新增 `TemplateRepository`，将模板快照字典（`templates`、`templateById`、`templateFileByItemId`、`templateParentIndex`）、alias 索引构建 / 重置生命周期（`BuildTemplateAliasIndex` / `Reload`）、模板查找与 clone 解析（`SelectTemplateData` / `TryResolveTemplateCloneByIdOrAlias`）、父模板推断（`InferParentIdFromTemplateFile` / `GetTemplateForParentId`）以及 `IsWeapon` / 允许字段表缓存访问全部收敛到该仓库中；`TemplateRepository` 拥有 `TemplateMetadataCache` 实例并统一暴露其缓存语义；`RealismPatchGenerator` 不再直接拥有模板快照字典（`templates` / `templateById` / `templateFileByItemId` / `templateParentIndex`）与 alias 索引，模板侧缓存语义交由 `TemplateRepository` / `TemplateMetadataCache` 承载并经边界访问；生成器内部仍保留一个兼容性私有字段引用 `TemplateMetadataCache`，但不再作为这些缓存的直接持有者。
- 回归补充：新增 `TemplateRepository_Reload_ClearsResolvedAliasCacheAndRebuildsAliasIndex`，覆盖 `Reload()` 后 alias 解析仍返回相同 ID 的 reload-after-alias-resolution 行为；新增 `TryResolveTemplateCloneByIdOrAlias_CachedAliasAndDirectIdReturnSameTemplateInstance`，验证 alias 缓存路径与直接 ID 查找返回同一模板实例；同时继续保持第五子切片的 alias cache hit 回归与 `IsWeapon` cache 回归绿灯。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~TryResolveTemplateCloneByIdOrAlias_CachesResolvedAliasResult|FullyQualifiedName~TemplateRepository_Reload_ClearsResolvedAliasCacheAndRebuildsAliasIndex|FullyQualifiedName~TryResolveTemplateCloneByIdOrAlias_CachedAliasAndDirectIdReturnSameTemplateInstance|FullyQualifiedName~IsWeapon_CachesParentIdResult|FullyQualifiedName~IsWeapon_CachesMissingParentIdAsFalse"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片仅把模板快照存储与模板侧缓存收敛到独立仓库中，不改变串行 alias 语义、cache-warming 策略与输出行为，为后续并行化或缓存替换策略提供更清晰的模板数据边界。

### P2 / 任务 6：第七子切片（静态文本与推断 helper 边界整理）

- 实施内容：新增 `PatchTextInferenceHelpers`，将 `RealismPatchGenerator` 中被 `ProfileInferenceService` / `ItemInfoFactory` 反向依赖的纯静态 helper（本地化名称提取、显示名选择、token 提取、关键词判断、名称驱动的 profile 推断等）迁移到独立边界；相关调用方改为直接依赖该 helper，并删除生成器中已被替代的重复/死代码方法。
- 回归补充：新增针对 `ProfileInferenceService` 与 `ItemInfoFactory` 的解耦 regression，约束它们不再依赖 `RealismPatchGenerator` 的对应静态 helper 宿主；同时继续保持 TemplateRepository / alias / IsWeapon 等既有 seam regressions 绿灯。
- 验证命令：`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj" --filter "FullyQualifiedName~ProfileInferenceService_DoesNotDependOn_RealismPatchGenerator_NameInferenceHelpers|FullyQualifiedName~ItemInfoFactory_DoesNotDependOn_RealismPatchGenerator_TextHelpers|FullyQualifiedName~TryResolveTemplateCloneByIdOrAlias_CachesResolvedAliasResult|FullyQualifiedName~TemplateRepository_Reload_ClearsResolvedAliasCacheAndRebuildsAliasIndex|FullyQualifiedName~TryResolveTemplateCloneByIdOrAlias_CachedAliasAndDirectIdReturnSameTemplateInstance"`；`dotnet test "RealismPatchGenerator.Tests\RealismPatchGenerator.Tests.csproj"`
- 结论：当前切片仅把纯静态文本 / 名称 / 推断辅助逻辑迁移到独立 helper 边界，不改变推断顺序、fallback 语义与输出行为，为后续进一步收缩 `RealismPatchGenerator` 的 orchestration 边界提供基础。

## 更早历史性能对比（非 P2 / 任务 6 第一子切片结果）

以下小 / 中 / 大样本耗时与分配量对比，属于更早轮次的累计性能优化记录，用于保留历史基线，不对应本次 `FileProcessingContext` 边界整理。

### 小样本

- 样本：user_templates/[2]新物品-竞技场赛季奖励-SPT Battlepass.json
- 总耗时：201.09 ms -> 170.90 ms，下降 15.02%
- 文件处理：146.77 ms -> 54.89 ms，下降 62.60%
- 规则执行：35.01 ms -> 24.54 ms，下降 29.91%
- 输出写出：5.49 ms -> 5.15 ms，下降 6.19%
- 分配量：51.55 MB -> 34.50 MB，下降 33.07%

### 中样本

- 样本：input/user_templates 整个子目录
- 总耗时：3102.45 ms -> 1487.02 ms，下降 52.07%
- 文件处理：2943.03 ms -> 1262.68 ms，下降 57.09%
- 规则执行：197.90 ms -> 211.16 ms，上升 6.70%
- 输出写出：109.40 ms -> 114.53 ms，上升 4.69%
- 分配量：1165.41 MB -> 202.19 MB，下降 82.65%

### 大样本

- 样本：完整 input 目录
- 总耗时：4134.72 ms -> 2969.75 ms，下降 28.18%
- 文件处理：3927.23 ms -> 2661.51 ms，下降 32.23%
- 规则执行：435.49 ms -> 436.19 ms，基本持平
- 输出写出：158.57 ms -> 194.98 ms，上升 22.96%
- 分配量：1390.50 MB -> 408.61 MB，下降 70.61%

## 已落地优化

### 第 0 轮：性能基线与计时点

- 为 Generate 主流程补充总耗时、模板加载、输入发现、文件处理、规则执行、输出写出、分配量与 GC 次数统计
- CLI 增加性能指标输出
- CLI 增加 include 过滤，便于稳定复现小/中/大样本

### 第 1 轮：分析上下文与推断缓存

- 新增 PatchRuleContext
- 在单个 item 的规则执行链内缓存 PatchAnalysisContext
- 缓存 weapon / attachment / gear / ammo 相关 profile 推断与 ammo penetration tier
- 仅在 ModType、WeapType 等会影响上下文的字段被修正后失效缓存，避免行为漂移

### 第 2 轮：模板/别名/规则热点索引化

- 为 template alias 匹配建立预分词索引
- alias 解析改为只扫描共享 token 的候选集，而不是每次线性扫描整个 templateById
- 增加 unresolved alias 缓存，避免重复失败扫描

### 第 3 轮：DeepClone 与 JSON 处理减量

- 输入文件解析改为 FileStream + JsonNode.Parse(stream)，去掉整文件字符串中转
- PatchOutputBuffer 改为保存最终 patch 引用，减少一层重复 DeepClone

### 第 4 轮：输出流水线与文件写出优化

- 输出阶段改为 Utf8JsonWriter 流式写出
- 移除输出前为整文件再次组装 JsonObject 的中间态

## 结果解读

- 当前最明显的收益来自文件处理阶段，而不是规则执行阶段本身
- 模板加载阶段因 alias 预索引预处理有所上升，但总体收益远大于这部分新增成本
- 输出阶段在大样本上没有得到耗时改善，但中间分配明显下降；考虑到输出段在总耗时中的占比仍较低，本轮不继续追加更重的输出复杂度
- 首轮优化后，最主要的收益已经来自减少重复扫描、减少整文件字符串中转，以及减少重复克隆

## 验证结论

- RuleDataSynchronizationTests：16/16 通过
- 三档样本输出：NO_DIFF
- 第二阶段之后的首轮性能优化已完成，可进入后续按需微调，而不必继续做高风险大改
