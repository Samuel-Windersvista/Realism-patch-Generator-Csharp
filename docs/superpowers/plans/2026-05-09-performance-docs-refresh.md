# Performance Docs Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite the performance suggestion and planning documents so they reflect the current codebase reality, preserve the execution record as history, and remove outdated stage-split docs from `docs`.

**Architecture:** Treat the performance docs as a three-document set with distinct responsibilities: current evaluation, current action plan, and historical execution record. Update each file to match that role, then remove the obsolete stage-specific route documents and verify no remaining references or wording conflicts exist.

**Tech Stack:** Markdown, git repository docs, PowerShell 5.1 shell, ripgrep/search tools

---

### Task 1: Rewrite `docs/生成效率改进建议.md` as a technical evaluation

**Files:**
- Modify: `docs/生成效率改进建议.md`
- Reference: `docs/superpowers/specs/2026-05-09-performance-docs-refresh-design.md`
- Reference: `docs/RealismPatchGenerator_性能优化执行记录.md`

- [ ] **Step 1: Re-read the approved design and current suggestion document**

Read these files before editing:

```text
docs/superpowers/specs/2026-05-09-performance-docs-refresh-design.md
docs/生成效率改进建议.md
docs/RealismPatchGenerator_性能优化执行记录.md
```

Expected: the editor has the current suggestion wording, the approved design constraints, and the historical optimization context in view.

- [ ] **Step 2: Replace the document structure with an evaluation-oriented outline**

Use this section skeleton:

```markdown
# 生成效率改进建议

## 背景与评估范围
## 总体结论
## 逐项评估
## 调整后的优先级
## 建议的推进顺序
## 暂不建议优先投入的事项
```

Expected: the old “10 条建议直接罗列并默认可做” framing is removed.

- [ ] **Step 3: Rewrite the overall conclusion section with explicit verdict buckets**

Include a concise verdict summary covering these buckets:

```markdown
- 完全认同：文件级并行化、模板元数据缓存、关键词匹配优化、模板 I/O 并行化、输出写入并行化
- 部分认同：消除冗余 DeepClone、JsonDocument 替代 JsonObject、模板懒加载
- 不建议按原表述直接执行：StaticData 默认模板改单例
- 价值较低：日志列表预分配容量
```

Expected: the reader can understand the revised stance without scanning the full document.

- [ ] **Step 4: Rewrite the per-item analysis with code-reality constraints**

For all 10 items, rewrite the explanations so they explicitly capture:

```markdown
- 文件级并行化：成立，但必须先解决 `patchOutputBuffer`、`generatedItemInfoById`、`logs` 的并发安全问题。
- DeepClone：模板加载阶段存在明确冗余；生成链路中的多个克隆是语义性复制，不能一概移除。
- JsonDocument：适合只读模板/规则数据，不适合需要继续变更的输入对象路径。
- 模板懒加载：方向成立，但收益依赖真实命中模式，需防止频繁 miss 带来的复杂度反噬。
- 模板类型/字段集合缓存：属于低风险高收益，应前置。
- Profile 推断匹配优化：可优先考虑 `SearchValues` 或等价轻量方案，不强绑定 Aho-Corasick。
- StaticData 单例：直接共享 `JsonObject` 会产生可变对象污染风险。
- 模板 I/O 并行化：成立，但模板索引构建仍需并发安全方案。
- 输出写入并行化：成立，且比文件级并行化更容易安全落地。
- 日志容量预分配：可做，但不应占据高优先级。
```

Expected: each suggestion now states both benefit and constraint, not just benefit.

- [ ] **Step 5: Replace the priority table with a revised implementation order**

Insert a new priority table similar to the following:

```markdown
| 优先级 | 改进项 | 理由 |
|--------|--------|------|
| 1 | 文件级并行化（先解决共享状态） | 潜在收益最高，但有明确前置条件 |
| 2 | TemplateCatalog 去重克隆/双份存储 | 低风险、立刻降低分配 |
| 3 | 模板元数据缓存 | 高频调用、实现直接 |
| 4 | Profile 推断匹配优化 | 热点明确、收益稳定 |
| 5 | 输出写入并行化 | 独立性高、风险较低 |
| 6 | 模板 I/O 并行化 | 可做，但收益通常次于前几项 |
| 7 | 只读路径的 JsonDocument 优化 | 需限定作用域 |
| 8 | 模板懒加载 | 设计复杂度更高 |
| 9 | StaticData 默认模板复用优化 | 需规避可变对象污染 |
| 10 | 日志容量预分配 | 微优化 |
```

Expected: priority now reflects both engineering value and delivery risk.

- [ ] **Step 6: Add a closing section that distinguishes current guidance from historical records**

Add a short closing note like this:

```markdown
## 与历史文档的关系

本文件描述的是当前基于代码核查后的性能建议口径。
历史性能收益与已执行结果以 `docs/RealismPatchGenerator_性能优化执行记录.md` 为准；具体落地顺序以当前版 `docs/RealismPatchGenerator_性能优化计划表.md` 为准。
```

Expected: readers know which document answers which question.

- [ ] **Step 7: Verify the rewritten suggestion document for consistency**

Run:

```powershell
rg -n "已完成|第 0 轮|第 1 轮|第 2 轮|第 3 轮|第 4 轮" "docs/生成效率改进建议.md"
```

Expected: no lines remain that present the suggestion doc as a historical execution log.


### Task 2: Rewrite `docs/RealismPatchGenerator_性能优化计划表.md` as the current executable plan

**Files:**
- Modify: `docs/RealismPatchGenerator_性能优化计划表.md`
- Reference: `docs/superpowers/specs/2026-05-09-performance-docs-refresh-design.md`
- Reference: `docs/生成效率改进建议.md`
- Reference: `docs/RealismPatchGenerator_性能优化执行记录.md`

- [ ] **Step 1: Re-read the current plan table and identify completed-history language to remove**

Inspect the existing plan file and mark these patterns for removal or rewrite:

```text
当前执行状态
已完成
第 0 轮已完成
第 1 轮已完成
第 2 轮已完成
第 3 轮已完成
第 4 轮已完成
一致性验证已完成
```

Expected: the editor has a clear list of historical phrases that do not belong in the new plan.

- [ ] **Step 2: Replace the document structure with a current-plan outline**

Use this skeleton:

```markdown
# RealismPatchGenerator 性能优化计划表

## 目标与适用范围
## 当前约束与前置条件
## 优先级分层
## 可执行任务表
## 建议执行顺序
## 暂不建议当前投入的事项
## 验收方式
```

Expected: the file reads like an action plan, not a mixed planning/history record.

- [ ] **Step 3: Define the priority layers around risk and payoff**

Add a section that groups work like this:

```markdown
### P0：低风险高收益
- TemplateCatalog 去重克隆与双份存储收敛
- 模板元数据缓存（weapon type / allowed field set）
- Profile 推断关键词匹配优化
- 输出写入并行化评估与落地

### P1：中风险中高收益
- 模板 I/O 并行化
- 只读模板路径的 JsonDocument 优化
- 文件级并行化的并发安全改造准备

### P2：高复杂度，需进一步证据
- 模板懒加载
- StaticData 默认模板复用方案重设计
```

Expected: the plan clearly separates “worth doing now” from “needs more proof”.

- [ ] **Step 4: Create an executable task table with files, validation, and completion criteria**

Include a table or repeated subsections that cover at least these tasks:

```markdown
1. TemplateCatalog 去重克隆与双份存储收敛
2. 模板元数据缓存
3. Profile 推断匹配优化
4. 输出写入并行化
5. 模板 I/O 并行化
6. 文件级并行化前置改造
```

Each task entry must state:

```markdown
- 涉及文件
- 实施内容
- 验证方式
- 风险点
- 完成标准
```

Expected: an engineer can pick up the file and know what to do next without reading deleted historical docs.

- [ ] **Step 5: Replace old round-based completion claims with a forward-looking execution order**

Add a sequence similar to this:

```markdown
1. 先做 TemplateCatalog 去重克隆与双份存储收敛
2. 再做模板元数据缓存
3. 再做 Profile 推断关键词匹配优化
4. 然后评估输出写入并行化
5. 最后准备文件级并行化所需的共享状态隔离
```

Expected: the plan shows a realistic order instead of retroactively describing completed rounds.

- [ ] **Step 6: Add an explicit “not now” section**

Insert a section like this:

```markdown
## 暂不建议当前投入的事项

- 不把所有 JsonObject 入口直接替换为 JsonDocument
- 不直接把 StaticData 默认模板改成共享单例
- 不在缺少 profile 数据前贸然实现模板懒加载
- 不优先投入日志列表容量预分配这类微优化
```

Expected: the file prevents over-engineering and protects the next executor from chasing weak wins.

- [ ] **Step 7: Add a short note linking this plan to the history record**

Add a section or closing note like this:

```markdown
## 历史记录说明

本文件描述的是当前建议执行的优化任务顺序。
历史性能收益、样本数据与既有优化结果请查看 `docs/RealismPatchGenerator_性能优化执行记录.md`。
```

Expected: the current plan and the history record no longer compete for the same role.

- [ ] **Step 8: Verify the rewritten plan no longer presents itself as fully completed**

Run:

```powershell
rg -n "已完成|当前执行状态|一致性验证已完成|第 0 轮|第 1 轮|第 2 轮|第 3 轮|第 4 轮" "docs/RealismPatchGenerator_性能优化计划表.md"
```

Expected: no stale completion phrasing remains unless it is clearly inside a historical-reference note.


### Task 3: Clarify `docs/RealismPatchGenerator_性能优化执行记录.md` as a historical record

**Files:**
- Modify: `docs/RealismPatchGenerator_性能优化执行记录.md`
- Reference: `docs/生成效率改进建议.md`
- Reference: `docs/RealismPatchGenerator_性能优化计划表.md`

- [ ] **Step 1: Insert a short scope note near the top of the file**

Add this note immediately after the title and before the existing `## 说明` section:

```markdown
> 本文档用于保留已执行性能优化的历史结果与样本数据。
> 当前建议请查看 `docs/生成效率改进建议.md`，当前落地顺序请查看 `docs/RealismPatchGenerator_性能优化计划表.md`。
```

Expected: the record is preserved, but its role is no longer ambiguous.

- [ ] **Step 2: Verify no other structural rewrite is needed**

Review the rest of the file and keep the performance data sections unchanged.

Expected: only the role-clarification note is added; historical measurements remain intact.


### Task 4: Delete the outdated stage-split route documents

**Files:**
- Delete: `docs/RealismPatchGenerator_第一阶段拆分落地顺序表.md`
- Delete: `docs/RealismPatchGenerator_第二阶段拆分落地顺序表.md`

- [ ] **Step 1: Confirm the two target files exist before deletion**

Run:

```powershell
Test-Path -LiteralPath "docs/RealismPatchGenerator_第一阶段拆分落地顺序表.md"; Test-Path -LiteralPath "docs/RealismPatchGenerator_第二阶段拆分落地顺序表.md"
```

Expected: both commands return `True`.

- [ ] **Step 2: Delete the outdated files**

Delete exactly these files:

```text
docs/RealismPatchGenerator_第一阶段拆分落地顺序表.md
docs/RealismPatchGenerator_第二阶段拆分落地顺序表.md
```

Expected: the obsolete stage-split docs are removed from the working tree.

- [ ] **Step 3: Verify they are gone**

Run:

```powershell
Test-Path -LiteralPath "docs/RealismPatchGenerator_第一阶段拆分落地顺序表.md"; Test-Path -LiteralPath "docs/RealismPatchGenerator_第二阶段拆分落地顺序表.md"
```

Expected: both commands return `False`.


### Task 5: Cross-document verification for consistency and stale references

**Files:**
- Verify: `docs/生成效率改进建议.md`
- Verify: `docs/RealismPatchGenerator_性能优化计划表.md`
- Verify: `docs/RealismPatchGenerator_性能优化执行记录.md`
- Verify: `docs/superpowers/specs/2026-05-09-performance-docs-refresh-design.md`

- [ ] **Step 1: Search for references to the deleted stage documents**

Run:

```powershell
rg -n "第一阶段拆分落地顺序表|第二阶段拆分落地顺序表" docs
```

Expected: no remaining references in active docs, or only deliberate mention inside version-control history outside the workspace scan.

- [ ] **Step 2: Review the three active performance docs together**

Confirm each file now answers a distinct question:

```markdown
- `生成效率改进建议.md`：哪些建议成立、哪些有风险
- `性能优化计划表.md`：接下来按什么顺序做
- `性能优化执行记录.md`：历史上已经做过什么、效果如何
```

Expected: no two documents claim the same responsibility.

- [ ] **Step 3: Inspect the final diff for scope control**

Run:

```powershell
git diff -- docs
```

Expected: only the three performance docs plus the two deleted stage-split docs appear in the diff, along with the already-approved spec/plan files if they are uncommitted.

- [ ] **Step 4: Do not create a commit unless the user explicitly asks**

Follow this repository rule during execution:

```text
Do not run git commit unless the user explicitly requests a commit.
```

Expected: documentation work is completed and verified without an unsolicited commit.
