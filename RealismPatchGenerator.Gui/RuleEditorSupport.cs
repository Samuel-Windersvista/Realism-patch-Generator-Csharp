using System.Globalization;
using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Gui;

internal enum UiLanguage
{
    Chinese,
    English,
}

internal readonly record struct LocalizedText(string Chinese, string English)
{
    public string Get(UiLanguage language)
    {
        return language == UiLanguage.English ? English : Chinese;
    }
}

internal sealed class RuleDocument
{
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public required JsonObject Root { get; init; }
    public bool IsDirty { get; set; }
}

internal sealed class RuleEditorSectionDefinition
{
    public required string Key { get; init; }
    public required string GroupKey { get; init; }
    public required LocalizedText DisplayName { get; init; }
    public required string FileName { get; init; }
    public required string SectionProperty { get; init; }
    public required LocalizedText SourceLabel { get; init; }
    public required LocalizedText Description { get; init; }
    public bool IsFlatRangeMap { get; init; }
}

internal sealed class RuleFieldHelp
{
    public required LocalizedText DisplayName { get; init; }
    public required LocalizedText Effect { get; init; }
    public required LocalizedText DirectionHint { get; init; }
}

internal sealed class RuleRangeEntry
{
    public required RuleDocument Document { get; init; }
    public required RuleEditorSectionDefinition Section { get; init; }
    public required string ProfileKey { get; init; }
    public required string FieldKey { get; init; }
    public required JsonObject RangeNode { get; init; }
    public required bool PreferInt { get; init; }
    public double MinValue { get; private set; }
    public double MaxValue { get; private set; }
    public bool IsDirty { get; private set; }

    public void Initialize(double minValue, double maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public void Update(double minValue, double maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
        RangeNode["min"] = minValue;
        RangeNode["max"] = maxValue;
        RangeNode["preferInt"] = PreferInt;
        IsDirty = true;
        Document.IsDirty = true;
    }
}

internal static class UiTextCatalog
{
    private static readonly IReadOnlyDictionary<string, LocalizedText> Texts = new Dictionary<string, LocalizedText>(StringComparer.Ordinal)
    {
        ["App.Title"] = new("SPT现实主义数值范围编辑生成器 v2.1", "SPT Realism Range Editor Generator v2.1"),
        ["Label.DataRoot"] = new("数据目录:", "Data Root:"),
        ["Label.OutputPath"] = new("输出路径:", "Output Path:"),
        ["Label.Seed"] = new("Seed:", "Seed:"),
        ["Label.ModifiedOnly"] = new("只看已修改项", "Modified Only"),
        ["Label.Search"] = new("搜索:", "Search:"),
        ["Label.Language"] = new("语言:", "Language:"),
        ["Language.Chinese"] = new("中文", "Chinese"),
        ["Language.English"] = new("英文", "English"),
        ["Button.Browse"] = new("选择输出路径", "Browse Output"),
        ["Button.SaveAll"] = new("保存全部", "Save All"),
        ["Button.Reload"] = new("重新加载", "Reload"),
        ["Button.Exceptions"] = new("例外物品", "Exception Items"),
        ["Button.Generate"] = new("生成补丁", "Generate"),
        ["Button.ClearSeed"] = new("清空", "Clear"),
        ["Button.UseLastSeed"] = new("填回上次", "Use Last"),
        ["Group.Navigation"] = new("规则分类", "Rule Categories"),
        ["Group.Grid"] = new("规则范围", "Rule Ranges"),
        ["Group.NavigationWithCount"] = new("规则分类 ({0})", "Rule Categories ({0})"),
        ["Group.GridAll"] = new("规则范围 ({0})", "Rule Ranges ({0})"),
        ["Group.GridRoot"] = new("规则范围 - {0} ({1})", "Rule Ranges - {0} ({1})"),
        ["Group.GridSection"] = new("规则范围 - {0} ({1})", "Rule Ranges - {0} ({1})"),
        ["Group.GridProfile"] = new("规则范围 - {0} / {1} ({2})", "Rule Ranges - {0} / {1} ({2})"),
        ["Tree.GroupNode"] = new("{0} ({1})", "{0} ({1})"),
        ["Tree.SectionNode"] = new("{0} ({1})", "{0} ({1})"),
        ["TreeGroup.Weapon"] = new("武器", "Weapons"),
        ["TreeGroup.Attachment"] = new("附件", "Attachments"),
        ["TreeGroup.Ammo"] = new("弹药", "Ammo"),
        ["TreeGroup.Gear"] = new("装备", "Gear"),
        ["Tab.Explanation"] = new("字段说明", "Field Help"),
        ["Tab.ExceptionsOverview"] = new("例外总览", "Exception Items Overview"),
        ["Tab.Log"] = new("运行日志", "Run Log"),
        ["Column.Enabled"] = new("启用", "Enabled"),
        ["Column.ItemId"] = new("ItemID", "ItemID"),
        ["Column.Name"] = new("名称", "Name"),
        ["Column.OverrideCount"] = new("字段数", "Fields"),
        ["Column.SourceFile"] = new("来源文件", "Source File"),
        ["Column.Notes"] = new("备注", "Notes"),
        ["Column.Profile"] = new("档位", "Profile"),
        ["Column.Field"] = new("字段", "Field"),
        ["Column.Min"] = new("最小值", "Min"),
        ["Column.Max"] = new("最大值", "Max"),
        ["Column.PreferInt"] = new("整数优先", "Prefer Int"),
        ["Column.Source"] = new("来源", "Source"),
        ["Status.TotalItems"] = new("总范围项: {0}", "Total entries: {0}"),
        ["Status.VisibleItems"] = new("当前显示: {0}", "Visible: {0}"),
        ["Status.DirtyItems"] = new("未保存修改: {0}", "Unsaved: {0}"),
        ["State.Ready"] = new("就绪", "Ready"),
        ["State.InvalidRoot"] = new("请选择有效的数据目录", "Select a valid data root"),
        ["State.Loaded"] = new("规则已加载", "Rules loaded"),
        ["State.Saved"] = new("规则已保存", "Rules saved"),
        ["State.Reloaded"] = new("规则已重新加载", "Rules reloaded"),
        ["State.ExceptionSaved"] = new("例外物品已保存", "Exception items saved"),
        ["State.Saving"] = new("正在保存规则", "Saving rules"),
        ["State.Generating"] = new("正在生成补丁", "Generating patches"),
        ["State.GenerateDone"] = new("生成完成", "Generation complete"),
        ["State.GenerateFailed"] = new("生成失败", "Generation failed"),
        ["Message.InvalidDataRoot"] = new("请选择包含 input 和 RealismItemTemplates 的 C# 程序数据目录。", "Select a C# data directory containing input and RealismItemTemplates."),
        ["Message.InvalidDataRootTitle"] = new("目录无效", "Invalid Directory"),
        ["Message.ReloadConfirm"] = new("当前有未保存修改，重新加载会丢失这些更改。是否继续？", "You have unsaved changes. Reloading will discard them. Continue?"),
        ["Message.ReloadConfirmTitle"] = new("确认重新加载", "Confirm Reload"),
        ["Message.SaveFailedTitle"] = new("保存失败", "Save Failed"),
        ["Message.LoadFailedTitle"] = new("加载失败", "Load Failed"),
        ["Message.GenerateFailedTitle"] = new("生成失败", "Generation Failed"),
        ["Message.ParseNumber"] = new("请输入有效数字。", "Enter a valid number."),
        ["Message.MinGreaterThanMax"] = new("最小值不能大于最大值。", "Min cannot be greater than max."),
        ["Message.InvalidSeed"] = new("Seed 必须是 0 到 4294967295 之间的无符号整数。", "Seed must be an unsigned integer between 0 and 4294967295."),
        ["Message.NoFieldSelected"] = new("请选择一条规则查看字段说明。", "Select a rule entry to view field help."),
        ["Message.ExplanationHint"] = new("左侧选择规则分类，右侧编辑最小值和最大值。", "Choose a rule category on the left, then edit min and max values on the right."),
        ["Message.SeedPlaceholder"] = new("留空=每次随机", "Blank = new random seed"),
        ["Message.OutputPathHint"] = new("建议输出到 -user\\mods\\SPT-Realism\\db\\templates", "Recommended output: -user\\mods\\SPT-Realism\\db\\templates"),
        ["Message.OutputPathHintWithLastSeed"] = new("建议输出到 -user\\mods\\SPT-Realism\\db\\templates | 上次生成 seed: {0}", "Recommended output: -user\\mods\\SPT-Realism\\db\\templates | Last generation seed: {0}"),
        ["Label.ProfileGlobal"] = new("全局", "Global"),
        ["Label.Yes"] = new("是", "Yes"),
        ["Label.No"] = new("否", "No"),
        ["Explanation.Section"] = new("分类", "Category"),
        ["Explanation.Profile"] = new("档位", "Profile"),
        ["Explanation.Field"] = new("字段", "Field"),
        ["Explanation.CurrentRange"] = new("当前范围", "Current Range"),
        ["Explanation.IntegerMode"] = new("整数优先", "Prefer Int"),
        ["Explanation.Description"] = new("分类说明", "Category Description"),
        ["Explanation.FieldEffect"] = new("字段作用", "Effect"),
        ["Explanation.DirectionHint"] = new("方向提示", "Direction Hint"),
        ["Explanation.Modified"] = new("修改状态", "Modified"),
        ["Explanation.ModifiedYes"] = new("当前项尚未保存到规则文件。", "This entry has unsaved edits."),
        ["Explanation.ModifiedNo"] = new("当前项与磁盘中的规则文件一致。", "This entry matches the current rule file on disk."),
        ["Log.StartGenerate"] = new("开始生成，数据目录: {0}", "Generation started, data root: {0}"),
        ["Log.SaveSuccess"] = new("已保存规则文件: {0}", "Saved rule file: {0}"),
        ["Log.ReloadSuccess"] = new("规则已从磁盘重新加载。", "Rules reloaded from disk."),
        ["Log.ExceptionSaveSuccess"] = new("已保存例外物品配置: {0}", "Saved exception item settings: {0}"),
    };

    public static string Get(string key, UiLanguage language)
    {
        return Texts.TryGetValue(key, out var text) ? text.Get(language) : key;
    }

    public static string Format(string key, UiLanguage language, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(key, language), args);
    }
}

internal static class RuleEditorCatalog
{
    public static IReadOnlyList<RuleEditorSectionDefinition> Sections { get; } =
    [
        new()
        {
            Key = "weapon-clamp",
            GroupKey = "weapon",
            DisplayName = new("武器全局夹紧", "Weapon Global Clamps"),
            FileName = "weapon_rules.json",
            SectionProperty = "gunClampRules",
            SourceLabel = new("武器全局夹紧", "Weapon Global Clamps"),
            Description = new("限制武器核心字段的最终合法范围，避免生成值超出系统可接受区间。", "Hard limits for weapon core fields to keep generated values inside safe bounds."),
            IsFlatRangeMap = true,
        },
        new()
        {
            Key = "weapon-base",
            GroupKey = "weapon",
            DisplayName = new("武器基础规则", "Weapon Base Rules"),
            FileName = "weapon_rules.json",
            SectionProperty = "weaponProfileRanges",
            SourceLabel = new("武器基础规则", "Weapon Base Rules"),
            Description = new("定义不同武器档位自身的基础范围，是武器数值的主基准。", "Base ranges for each weapon profile. This is the primary baseline for generated weapon stats."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "weapon-caliber",
            GroupKey = "weapon",
            DisplayName = new("武器口径补修", "Weapon Caliber Modifiers"),
            FileName = "weapon_rules.json",
            SectionProperty = "weaponCaliberRuleModifiers",
            SourceLabel = new("武器口径补修", "Weapon Caliber Modifiers"),
            Description = new("基于弹种口径对武器基础范围做偏移修正，用于体现不同弹药体系的后坐与初速差异。", "Offset modifiers applied to weapon baselines based on caliber families."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "weapon-stock",
            GroupKey = "weapon",
            DisplayName = new("武器枪托补修", "Weapon Stock Modifiers"),
            FileName = "weapon_rules.json",
            SectionProperty = "weaponStockRuleModifiers",
            SourceLabel = new("武器枪托补修", "Weapon Stock Modifiers"),
            Description = new("基于枪托形态对武器基础范围做偏移修正，影响操控、后坐和肩托稳定性。", "Offset modifiers based on stock form factor, affecting control, recoil, and shoulder support."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "attachment-clamp",
            GroupKey = "attachment",
            DisplayName = new("附件全局夹紧", "Attachment Global Clamps"),
            FileName = "attachment_rules.json",
            SectionProperty = "modClampRules",
            SourceLabel = new("附件全局夹紧", "Attachment Global Clamps"),
            Description = new("限制附件字段的最终上下界，避免单个配件对枪械属性产生异常增益或惩罚。", "Hard bounds for attachment stats so a single part cannot create extreme bonuses or penalties."),
            IsFlatRangeMap = true,
        },
        new()
        {
            Key = "attachment-base",
            GroupKey = "attachment",
            DisplayName = new("附件规则", "Attachment Rules"),
            FileName = "attachment_rules.json",
            SectionProperty = "modProfileRanges",
            SourceLabel = new("附件规则", "Attachment Rules"),
            Description = new("定义配件档位的基础范围，例如消音器、弹匣、护木、瞄具等。", "Base ranges for attachment profiles such as suppressors, magazines, handguards, and optics."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "ammo-base",
            GroupKey = "ammo",
            DisplayName = new("弹药基础规则", "Ammo Base Rules"),
            FileName = "ammo_rules.json",
            SectionProperty = "ammoProfileRanges",
            SourceLabel = new("弹药基础规则", "Ammo Base Rules"),
            Description = new("定义不同口径或用途弹药的基础范围，是伤害、穿深、速度等数值的主基准。", "Base ranges for ammo families, defining damage, penetration, velocity, and related stats."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "ammo-special",
            GroupKey = "ammo",
            DisplayName = new("弹药特殊修正", "Ammo Special Modifiers"),
            FileName = "ammo_rules.json",
            SectionProperty = "ammoSpecialModifiers",
            SourceLabel = new("弹药特殊修正", "Ammo Special Modifiers"),
            Description = new("按 AP、曳光、亚音速、空尖等特性对基础弹药范围叠加修正。", "Additive modifiers for AP, tracer, subsonic, hollow point, and other special ammo traits."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "ammo-penetration",
            GroupKey = "ammo",
            DisplayName = new("弹药穿深档位修正", "Ammo Penetration Tier Modifiers"),
            FileName = "ammo_rules.json",
            SectionProperty = "ammoPenetrationModifiers",
            SourceLabel = new("弹药穿深档位修正", "Ammo Penetration Tier Modifiers"),
            Description = new("按照穿深档位对伤害、甲伤、过热、故障率等字段做整体偏移。", "Modifiers applied by penetration tier across damage, armor damage, heat, and malfunction behavior."),
            IsFlatRangeMap = false,
        },
        new()
        {
            Key = "gear-clamp",
            GroupKey = "gear",
            DisplayName = new("装备全局夹紧", "Gear Global Clamps"),
            FileName = "gear_rules.json",
            SectionProperty = "gearClampRules",
            SourceLabel = new("装备全局夹紧", "Gear Global Clamps"),
            Description = new("限制装备类字段的最终范围，避免移动速度、舒适度等出现异常值。", "Hard limits for gear-related fields such as movement penalties and comfort."),
            IsFlatRangeMap = true,
        },
        new()
        {
            Key = "gear-base",
            GroupKey = "gear",
            DisplayName = new("装备规则", "Gear Rules"),
            FileName = "gear_rules.json",
            SectionProperty = "gearProfileRanges",
            SourceLabel = new("装备规则", "Gear Rules"),
            Description = new("定义护甲、胸挂、头盔、背包、耳机等装备档位的基础数值范围。", "Base ranges for armor, rigs, helmets, backpacks, headsets, and other gear profiles."),
            IsFlatRangeMap = false,
        },
    ];

    public static IReadOnlyDictionary<string, RuleFieldHelp> FieldHelp { get; } =
        new Dictionary<string, RuleFieldHelp>(StringComparer.OrdinalIgnoreCase)
        {
            ["VerticalRecoil"] = new() { DisplayName = new("垂直后坐", "Vertical Recoil"), Effect = new("影响开火时枪口向上抬升的强度。", "Controls how strongly the muzzle climbs upward when firing."), DirectionHint = new("通常调低会更稳，调高会更难压枪。", "Lower values usually feel steadier, higher values are harder to control.") },
            ["HorizontalRecoil"] = new() { DisplayName = new("水平后坐", "Horizontal Recoil"), Effect = new("影响开火时枪口横向摆动的幅度。", "Controls side-to-side recoil movement while firing."), DirectionHint = new("通常调低会让弹着更集中，调高会让横向散布更明显。", "Lower values usually tighten groups, higher values increase lateral spread.") },
            ["Convergence"] = new() { DisplayName = new("收束能力", "Convergence"), Effect = new("影响后坐恢复和瞄准重新归位的速度。", "Affects how quickly the weapon settles back onto target after recoil."), DirectionHint = new("通常调高会更容易回正，调低会让连续射击更发散。", "Higher values usually help the weapon recover faster.") },
            ["Dispersion"] = new() { DisplayName = new("散布", "Dispersion"), Effect = new("影响武器或部件对射击离散度的贡献。", "Affects shot spread contributed by the weapon or part."), DirectionHint = new("通常调低更精准，调高更飘。", "Lower is usually more accurate, higher is more scattered.") },
            ["VisualMulti"] = new() { DisplayName = new("视觉后坐倍率", "Visual Recoil Multiplier"), Effect = new("影响屏幕视觉晃动和主观后坐感。", "Controls perceived camera and visual kick while firing."), DirectionHint = new("通常调低更平顺，调高则开火观感更震。", "Lower values feel smoother, higher values feel punchier.") },
            ["Ergonomics"] = new() { DisplayName = new("人机工效", "Ergonomics"), Effect = new("影响持枪舒适度、抬枪和维持瞄准的体验。", "Affects handling comfort, aim raise behavior, and sustained aiming feel."), DirectionHint = new("通常调高更顺手，调低更笨重。", "Higher usually feels snappier, lower feels heavier.") },
            ["RecoilIntensity"] = new() { DisplayName = new("后坐强度", "Recoil Intensity"), Effect = new("影响后坐冲击的整体强弱。", "Controls the overall punch of recoil."), DirectionHint = new("通常调低更柔和，调高更猛烈。", "Lower softens recoil, higher makes it more violent.") },
            ["BaseTorque"] = new() { DisplayName = new("基础扭矩", "Base Torque"), Effect = new("影响手枪等武器在开火时的翻转倾向。", "Affects rotational kick, often most noticeable on pistols."), DirectionHint = new("负值更大通常更利于控制翻转。", "More negative values usually reduce flip.") },
            ["CameraRecoil"] = new() { DisplayName = new("镜头后坐", "Camera Recoil"), Effect = new("影响视角抖动和镜头被后坐带动的程度。", "Controls how much the camera itself is moved by recoil."), DirectionHint = new("通常调低能减少眩晕感，调高会更晃。", "Lower values reduce camera shake, higher values increase it.") },
            ["Velocity"] = new() { DisplayName = new("速度修正", "Velocity Modifier"), Effect = new("影响部件或修正规则对弹速的加减。", "Modifies projectile velocity when used as a rule offset."), DirectionHint = new("调高通常提升初速，调低通常降低初速。", "Higher usually increases velocity, lower decreases it.") },
            ["BaseReloadSpeedMulti"] = new() { DisplayName = new("基础换弹倍率", "Base Reload Speed Multiplier"), Effect = new("影响武器层面的换弹速度倍率。", "Controls base reload speed scaling on the weapon."), DirectionHint = new("小于 1 通常更快，大于 1 通常更慢。", "Below 1 is usually faster, above 1 is slower.") },
            ["BaseChamberCheckSpeed"] = new() { DisplayName = new("验膛速度", "Chamber Check Speed"), Effect = new("影响检查弹膛动作的基础速度。", "Controls base speed for chamber check actions."), DirectionHint = new("调高通常动作更快。", "Higher usually means faster actions.") },
            ["ShotgunDispersion"] = new() { DisplayName = new("霰弹散布", "Shotgun Dispersion"), Effect = new("影响霰弹枪弹丸扩散程度。", "Controls pellet spread for shotgun ammo or weapons."), DirectionHint = new("调低通常更集中，调高通常更散。", "Lower usually tightens patterns, higher spreads them out.") },
            ["Accuracy"] = new() { DisplayName = new("精度修正", "Accuracy Modifier"), Effect = new("影响命中精度和部件对精准度的贡献。", "Affects accuracy and how much a part changes it."), DirectionHint = new("通常调高更准，调低更差。", "Higher usually improves accuracy.") },
            ["Loudness"] = new() { DisplayName = new("响度", "Loudness"), Effect = new("影响射击声响的大小。", "Controls how loud the shot is."), DirectionHint = new("负值更大通常更安静，正值更大通常更吵。", "More negative is quieter, more positive is louder.") },
            ["Flash"] = new() { DisplayName = new("枪口火焰", "Muzzle Flash"), Effect = new("影响枪口焰可见度。", "Controls visible muzzle flash."), DirectionHint = new("调低通常火光更小，调高通常更明显。", "Lower reduces flash, higher makes it more visible.") },
            ["ModMalfunctionChance"] = new() { DisplayName = new("部件故障率修正", "Part Malfunction Modifier"), Effect = new("影响安装该部件后故障概率的变化。", "Modifies malfunction risk contributed by a part."), DirectionHint = new("调低更稳定，调高更容易出故障。", "Lower is safer, higher is riskier.") },
            ["DurabilityBurnModificator"] = new() { DisplayName = new("耐久烧蚀倍率", "Durability Burn Multiplier"), Effect = new("影响武器或弹药对耐久消耗的放大程度。", "Controls how strongly durability wear is amplified."), DirectionHint = new("调低更耐用，调高磨损更快。", "Lower means less wear, higher means faster wear.") },
            ["AimSpeed"] = new() { DisplayName = new("举镜速度", "Aim Speed"), Effect = new("影响从腰射到瞄准的切换速度。", "Controls speed when moving into ADS."), DirectionHint = new("通常调高更快，调低更慢。", "Higher is usually faster.") },
            ["AimStability"] = new() { DisplayName = new("瞄准稳定", "Aim Stability"), Effect = new("影响举镜后准星晃动和保持稳定的能力。", "Controls sway and steadiness while aiming."), DirectionHint = new("通常调高更稳，调低更飘。", "Higher is steadier.") },
            ["ReloadSpeed"] = new() { DisplayName = new("换弹修正", "Reload Speed Modifier"), Effect = new("影响部件对换弹动作速度的修正。", "Modifies reload speed through parts."), DirectionHint = new("调高通常更快，调低通常更慢。", "Higher usually speeds up reloads.") },
            ["LoadUnloadModifier"] = new() { DisplayName = new("装填与卸弹修正", "Load/Unload Modifier"), Effect = new("影响压弹、退弹和管理弹匣的速度。", "Affects loading and unloading actions."), DirectionHint = new("调高通常更快。", "Higher usually improves speed.") },
            ["CheckTimeModifier"] = new() { DisplayName = new("检查时间修正", "Check Time Modifier"), Effect = new("影响查看弹药、检查弹匣等动作时长。", "Affects time spent on inspection actions."), DirectionHint = new("调低通常更快，调高通常更慢。", "Lower is usually faster.") },
            ["Handling"] = new() { DisplayName = new("操控性", "Handling"), Effect = new("影响持枪和切换动作的整体灵活度。", "Affects general handling agility."), DirectionHint = new("通常调高更灵活，调低更迟缓。", "Higher usually feels more agile.") },
            ["HeatFactor"] = new() { DisplayName = new("热量系数", "Heat Factor"), Effect = new("影响开火后热量累积速度。", "Controls how quickly heat builds while firing."), DirectionHint = new("调低更不容易过热，调高更容易升温。", "Lower resists overheating, higher heats faster.") },
            ["CoolFactor"] = new() { DisplayName = new("散热系数", "Cooling Factor"), Effect = new("影响热量散去的速度。", "Controls heat dissipation speed."), DirectionHint = new("调高通常冷却更快。", "Higher usually cools faster.") },
            ["InitialSpeed"] = new() { DisplayName = new("初速", "Initial Speed"), Effect = new("影响弹丸飞行速度和弹道平直程度。", "Controls projectile speed and trajectory flatness."), DirectionHint = new("通常调高飞得更快更平，调低下坠更明显。", "Higher usually means flatter trajectories.") },
            ["BulletMassGram"] = new() { DisplayName = new("弹重", "Bullet Mass"), Effect = new("影响弹丸质量，对后坐、能量保持和穿透表现有连带影响。", "Controls projectile mass and indirectly affects recoil, retained energy, and penetration behavior."), DirectionHint = new("通常调高更重，调低更轻。", "Higher is heavier, lower is lighter.") },
            ["Damage"] = new() { DisplayName = new("伤害", "Damage"), Effect = new("影响对肉体目标造成的基础伤害。", "Base damage against flesh targets."), DirectionHint = new("调高更疼，调低更弱。", "Higher hits harder.") },
            ["PenetrationPower"] = new() { DisplayName = new("穿透力", "Penetration Power"), Effect = new("影响穿甲能力与高护甲目标的威胁。", "Controls armor penetration capability."), DirectionHint = new("调高更容易穿甲，调低更偏向软目标。", "Higher penetrates better.") },
            ["ammoRec"] = new() { DisplayName = new("弹药后坐修正", "Ammo Recoil Modifier"), Effect = new("影响弹药本身对武器后坐的增减。", "Modifies recoil contributed by the ammo itself."), DirectionHint = new("调低通常更好控，调高通常更踢。", "Lower usually controls better.") },
            ["ammoAccr"] = new() { DisplayName = new("弹药精度修正", "Ammo Accuracy Modifier"), Effect = new("影响弹药对精准度的修正。", "Modifies accuracy contributed by the ammo."), DirectionHint = new("调高通常更准，调低更散。", "Higher usually improves precision.") },
            ["ArmorDamage"] = new() { DisplayName = new("甲伤系数", "Armor Damage"), Effect = new("影响子弹对护甲耐久的削减能力。", "Controls how much the round damages armor durability."), DirectionHint = new("调高更伤甲，调低更保守。", "Higher strips armor faster.") },
            ["HeavyBleedingDelta"] = new() { DisplayName = new("重出血修正", "Heavy Bleed Modifier"), Effect = new("影响造成重出血的倾向。", "Controls tendency to inflict heavy bleeding."), DirectionHint = new("调高更容易触发重出血。", "Higher increases heavy bleed chance.") },
            ["LightBleedingDelta"] = new() { DisplayName = new("轻出血修正", "Light Bleed Modifier"), Effect = new("影响造成轻出血的倾向。", "Controls tendency to inflict light bleeding."), DirectionHint = new("调高更容易触发轻出血。", "Higher increases light bleed chance.") },
            ["BallisticCoeficient"] = new() { DisplayName = new("弹道系数", "Ballistic Coefficient"), Effect = new("影响飞行稳定性、速度保持和远距离表现。", "Affects flight stability, velocity retention, and long-range performance."), DirectionHint = new("通常调高远距离表现更好。", "Higher usually improves long-range behavior.") },
            ["MalfMisfireChance"] = new() { DisplayName = new("哑火率", "Misfire Chance"), Effect = new("影响弹药导致哑火的概率。", "Chance that the round causes a misfire."), DirectionHint = new("调低更稳定，调高更危险。", "Lower is more reliable.") },
            ["MisfireChance"] = new() { DisplayName = new("失火率", "Ignition Failure Chance"), Effect = new("影响弹药的综合失火风险。", "General ignition failure risk for the round."), DirectionHint = new("调低更可靠，调高更容易出问题。", "Lower is more reliable.") },
            ["MalfFeedChance"] = new() { DisplayName = new("供弹故障率", "Feed Malfunction Chance"), Effect = new("影响供弹异常的概率。", "Chance of feed-related malfunctions."), DirectionHint = new("调低更稳定，调高更容易卡壳。", "Lower is safer.") },
            ["ReloadSpeedMulti"] = new() { DisplayName = new("换弹速度倍率", "Reload Speed Multiplier"), Effect = new("影响装备对换弹动作的整体倍率。", "Overall reload speed multiplier caused by gear."), DirectionHint = new("小于 1 通常更快，大于 1 通常更慢。", "Below 1 is usually faster, above 1 is slower.") },
            ["Comfort"] = new() { DisplayName = new("舒适度", "Comfort"), Effect = new("影响装备佩戴时的负担感与整体体验。", "Represents wearing burden and general comfort."), DirectionHint = new("通常调低更轻便，调高更沉重或更压迫。", "Lower usually feels lighter.") },
            ["speedPenaltyPercent"] = new() { DisplayName = new("移速惩罚", "Move Speed Penalty"), Effect = new("影响装备对移动速度的惩罚百分比。", "Controls the movement penalty applied by gear."), DirectionHint = new("更接近 0 通常更灵活，更负则更拖慢移动。", "Closer to zero is more agile, more negative is slower.") },
            ["weaponErgonomicPenalty"] = new() { DisplayName = new("武器工效惩罚", "Weapon Ergonomic Penalty"), Effect = new("影响装备对持枪工效的附加惩罚。", "Additional ergonomics penalty applied by gear."), DirectionHint = new("更接近 0 更友好，更负则更影响操控。", "Closer to zero is friendlier, more negative hurts handling more.") },
            ["SpallReduction"] = new() { DisplayName = new("破片防护", "Spall Reduction"), Effect = new("影响面对破片与碎片伤害时的减免能力。", "Controls resistance to spall and fragment damage."), DirectionHint = new("通常调高防护更强。", "Higher usually means better protection.") },
            ["dB"] = new() { DisplayName = new("耳机放大强度", "Headset Amplification"), Effect = new("影响耳机对环境声的增强幅度。", "Controls how strongly headsets amplify surrounding sounds."), DirectionHint = new("调高更容易听清细节，但也可能更刺耳。", "Higher reveals more detail but can sound harsher.") },
            ["GasProtection"] = new() { DisplayName = new("毒气防护", "Gas Protection"), Effect = new("影响面罩或装备对有毒环境的防护能力。", "Protection against gas hazards."), DirectionHint = new("调高通常防护更强。", "Higher usually protects more.") },
            ["RadProtection"] = new() { DisplayName = new("辐射防护", "Radiation Protection"), Effect = new("影响装备对辐射环境的防护能力。", "Protection against radiation exposure."), DirectionHint = new("调高通常防护更强。", "Higher usually protects more.") },
            ["LoyaltyLevel"] = new() { DisplayName = new("商人等级", "Trader Loyalty Level"), Effect = new("影响物品解锁或审计时允许出现的商人等级区间。", "Controls allowed trader loyalty levels in rule outputs or audits."), DirectionHint = new("调低更早可用，调高更晚解锁。", "Lower unlocks earlier, higher unlocks later.") },
        };

    public static IReadOnlyList<(string Key, LocalizedText DisplayName)> SectionGroups { get; } =
    [
        ("weapon", new LocalizedText("武器", "Weapons")),
        ("attachment", new LocalizedText("附件", "Attachments")),
        ("ammo", new LocalizedText("弹药", "Ammo")),
        ("gear", new LocalizedText("装备", "Gear")),
    ];

    public static string GetGroupDisplayName(string groupKey, UiLanguage language)
    {
        return SectionGroups.FirstOrDefault(group => string.Equals(group.Key, groupKey, StringComparison.OrdinalIgnoreCase)).DisplayName.Get(language);
    }

    private static readonly IReadOnlyDictionary<string, LocalizedText> ProfileDisplayNames =
        new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase)
        {
            ["anti_materiel_50bmg"] = new("反器材 .50 BMG", "Anti-Materiel .50 BMG"),
            ["ap_extreme"] = new("极端穿甲", "Extreme AP"),
            ["ap_high"] = new("高穿甲", "High AP"),
            ["armor_chest_rig_heavy"] = new("重型防弹胸挂", "Heavy Armored Rig"),
            ["armor_chest_rig_light"] = new("轻型防弹胸挂", "Light Armored Rig"),
            ["armor_component_accessory"] = new("护甲附件", "Armor Accessory"),
            ["armor_component_faceshield"] = new("面罩组件", "Face Shield Component"),
            ["armor_mask_ballistic"] = new("防弹面罩", "Ballistic Mask"),
            ["armor_mask_decorative"] = new("装饰面罩", "Decorative Mask"),
            ["armor_plate_hard"] = new("硬质插板", "Hard Plate"),
            ["armor_plate_helmet"] = new("头盔插板", "Helmet Plate"),
            ["armor_plate_soft"] = new("软质插板", "Soft Plate"),
            ["armor_vest_heavy"] = new("重型防弹衣", "Heavy Armor Vest"),
            ["armor_vest_light"] = new("轻型防弹衣", "Light Armor Vest"),
            ["assault"] = new("突击步枪", "Assault Rifle"),
            ["back_panel"] = new("背挂扩展板", "Back Panel"),
            ["backpack_compact"] = new("紧凑背包", "Compact Backpack"),
            ["backpack_full"] = new("大型背包", "Full Backpack"),
            ["ball_standard"] = new("标准球弹", "Standard Ball"),
            ["barrel_integral_suppressed"] = new("一体消音枪管", "Integrally Suppressed Barrel"),
            ["barrel_long"] = new("长枪管", "Long Barrel"),
            ["barrel_medium"] = new("中型枪管", "Medium Barrel"),
            ["barrel_short"] = new("短枪管", "Short Barrel"),
            ["belt_harness"] = new("战术腰封", "Belt Harness"),
            ["bipod"] = new("两脚架", "Bipod"),
            ["buffer_adapter"] = new("缓冲管转接件", "Buffer Adapter"),
            ["bullpup"] = new("无托结构", "Bullpup"),
            ["catch"] = new("卡榫机构", "Catch"),
            ["charging_handle"] = new("拉机柄", "Charging Handle"),
            ["chest_rig_heavy"] = new("重型胸挂", "Heavy Chest Rig"),
            ["chest_rig_light"] = new("轻型胸挂", "Light Chest Rig"),
            ["cosmetic_gasmask"] = new("外观防毒面具", "Cosmetic Gas Mask"),
            ["cosmetic_headwear"] = new("外观头饰", "Cosmetic Headwear"),
            ["expanding"] = new("扩张型弹", "Expanding"),
            ["fixed_stock"] = new("固定枪托", "Fixed Stock"),
            ["flashlight_laser"] = new("战术灯激光器", "Flashlight and Laser"),
            ["folding_stock_collapsed"] = new("折叠枪托 收起", "Folding Stock Collapsed"),
            ["folding_stock_extended"] = new("折叠枪托 展开", "Folding Stock Extended"),
            ["foregrip"] = new("前握把", "Foregrip"),
            ["full_power_rifle"] = new("全威力步枪弹", "Full-Power Rifle"),
            ["full_power_rifle_rimmed"] = new("全威力有底缘步枪弹", "Rimmed Full-Power Rifle"),
            ["gasblock"] = new("导气箍", "Gas Block"),
            ["hammer"] = new("击锤机构", "Hammer"),
            ["handguard_long"] = new("长护木", "Long Handguard"),
            ["handguard_medium"] = new("中型护木", "Medium Handguard"),
            ["handguard_short"] = new("短护木", "Short Handguard"),
            ["headset"] = new("耳机", "Headset"),
            ["helmet_heavy"] = new("重型头盔", "Heavy Helmet"),
            ["helmet_light"] = new("轻型头盔", "Light Helmet"),
            ["intermediate_rifle"] = new("中间威力步枪弹", "Intermediate Rifle"),
            ["intermediate_rifle_58x42"] = new("5.8x42 中间威力步枪弹", "5.8x42 Intermediate Rifle"),
            ["intermediate_rifle_762x39"] = new("7.62x39 中间威力步枪弹", "7.62x39 Intermediate Rifle"),
            ["iron_sight"] = new("机械瞄具", "Iron Sight"),
            ["launcher"] = new("榴弹发射器", "Launcher"),
            ["machinegun"] = new("机枪", "Machine Gun"),
            ["magazine"] = new("弹匣", "Magazine"),
            ["magazine_compact"] = new("紧凑弹匣", "Compact Magazine"),
            ["magazine_drum"] = new("弹鼓", "Drum Magazine"),
            ["magazine_extended"] = new("加长弹匣", "Extended Magazine"),
            ["magazine_standard"] = new("标准弹匣", "Standard Magazine"),
            ["magnum_heavy"] = new("重型马格南", "Heavy Magnum"),
            ["mount"] = new("导轨座", "Mount"),
            ["muzzle_adapter"] = new("枪口转接件", "Muzzle Adapter"),
            ["muzzle_brake"] = new("制退器", "Muzzle Brake"),
            ["muzzle_flashhider"] = new("消焰器", "Flash Hider"),
            ["muzzle_suppressor"] = new("消音器", "Suppressor"),
            ["muzzle_suppressor_compact"] = new("紧凑消音器", "Compact Suppressor"),
            ["muzzle_thread"] = new("枪口螺纹保护件", "Muzzle Thread"),
            ["optic_eyecup"] = new("目镜罩", "Optic Eyecup"),
            ["optic_killflash"] = new("消反光罩", "Killflash"),
            ["pdw_high_pen_small"] = new("高穿透小口径 PDW 弹", "High-Pen Small PDW"),
            ["pdw_small_high_velocity"] = new("高初速小口径 PDW 弹", "High-Velocity Small PDW"),
            ["pen_lvl_1"] = new("穿深档位 1", "Penetration Tier 1"),
            ["pen_lvl_2"] = new("穿深档位 2", "Penetration Tier 2"),
            ["pen_lvl_3"] = new("穿深档位 3", "Penetration Tier 3"),
            ["pen_lvl_4"] = new("穿深档位 4", "Penetration Tier 4"),
            ["pen_lvl_5"] = new("穿深档位 5", "Penetration Tier 5"),
            ["pen_lvl_6"] = new("穿深档位 6", "Penetration Tier 6"),
            ["pen_lvl_7"] = new("穿深档位 7", "Penetration Tier 7"),
            ["pen_lvl_8"] = new("穿深档位 8", "Penetration Tier 8"),
            ["pen_lvl_9"] = new("穿深档位 9", "Penetration Tier 9"),
            ["pen_lvl_10"] = new("穿深档位 10", "Penetration Tier 10"),
            ["pen_lvl_11"] = new("穿深档位 11", "Penetration Tier 11"),
            ["pistol"] = new("手枪", "Pistol"),
            ["pistol_caliber"] = new("手枪弹", "Pistol Caliber"),
            ["pistol_compact"] = new("紧凑手枪", "Compact Pistol"),
            ["pistol_grip"] = new("手枪握把", "Pistol Grip"),
            ["protective_eyewear_ballistic"] = new("防弹护目镜", "Ballistic Eyewear"),
            ["protective_eyewear_standard"] = new("标准护目镜", "Standard Eyewear"),
            ["rail_panel"] = new("导轨盖板", "Rail Panel"),
            ["receiver"] = new("机匣", "Receiver"),
            ["rifle_300blk"] = new(".300 Blackout 步枪弹", ".300 Blackout Rifle"),
            ["rifle_545x39"] = new("5.45x39 步枪弹", "5.45x39 Rifle"),
            ["rifle_556x45"] = new("5.56x45 步枪弹", "5.56x45 Rifle"),
            ["rifle_762x39"] = new("7.62x39 步枪弹", "7.62x39 Rifle"),
            ["rifle_762x51"] = new("7.62x51 步枪弹", "7.62x51 Rifle"),
            ["rifle_9x39"] = new("9x39 步枪弹", "9x39 Rifle"),
            ["scope_magnified"] = new("放大瞄具", "Magnified Scope"),
            ["scope_red_dot"] = new("红点瞄具", "Red Dot Sight"),
            ["shot_shell_payload"] = new("霰弹装药", "Shot Shell Payload"),
            ["shotgun"] = new("霰弹枪", "Shotgun"),
            ["shotgun_shell"] = new("霰弹枪弹药", "Shotgun Shell"),
            ["shotgun_shell_12g"] = new("12 号霰弹", "12 Gauge Shell"),
            ["shotgun_shell_20g"] = new("20 号霰弹", "20 Gauge Shell"),
            ["shotgun_shell_23x75"] = new("23x75 霰弹", "23x75 Shell"),
            ["small_high_velocity"] = new("小口径高初速弹", "Small High-Velocity"),
            ["smg"] = new("冲锋枪", "SMG"),
            ["sniper"] = new("狙击步枪", "Sniper Rifle"),
            ["stock"] = new("枪托", "Stock"),
            ["stock_adapter"] = new("枪托转接件", "Stock Adapter"),
            ["stock_ads_support"] = new("贴腮支撑件", "ADS Support Stock Part"),
            ["stock_buttpad"] = new("枪托垫", "Buttpad"),
            ["stock_fixed"] = new("固定式枪托", "Fixed Stock"),
            ["stock_folding"] = new("折叠式枪托", "Folding Stock"),
            ["stock_rear_hook"] = new("尾钩式枪托", "Rear Hook Stock"),
            ["stockless"] = new("无枪托", "Stockless"),
            ["subsonic_heavy"] = new("重型亚音速弹", "Heavy Subsonic"),
            ["subsonic_heavy_9x39"] = new("9x39 重型亚音速弹", "9x39 Heavy Subsonic"),
            ["tracer"] = new("曳光弹", "Tracer"),
            ["trigger"] = new("扳机组件", "Trigger"),
        };

    public static RuleFieldHelp GetFieldHelp(string fieldKey)
    {
        if (FieldHelp.TryGetValue(fieldKey, out var help))
        {
            return help;
        }

        return new RuleFieldHelp
        {
            DisplayName = new(fieldKey, fieldKey),
            Effect = new("该字段会影响生成器输出到补丁中的对应属性范围。", "This field changes the generated output range for the corresponding patch property."),
            DirectionHint = new("数值调高或调低的具体效果取决于字段语义，修改前建议结合生成结果验证。", "The exact effect of increasing or decreasing this field depends on its meaning. Validate with generated output."),
        };
    }

    public static string GetProfileDisplayName(string profileKey, UiLanguage language)
    {
        if (ProfileDisplayNames.TryGetValue(profileKey, out var localizedText))
        {
            return localizedText.Get(language);
        }

        return language == UiLanguage.English
            ? profileKey.Replace('_', ' ')
            : profileKey.Replace('_', ' ');
    }

    public static string FormatNumber(double value, bool preferInt)
    {
        return preferInt
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}