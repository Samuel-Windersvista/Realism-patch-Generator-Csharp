using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RealismPatchGenerator.Core;

public sealed class OutputRuleAuditor
{
    private const string ArmbandParentId = "5b3f15d486f77432d0509248";
    private const double RangeComparisonEpsilon = 1e-9;
    private static readonly Regex IdLikeNameRegex = new("^[0-9a-f]{16,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string basePath;
    private readonly string outputDirectory;
    private readonly bool includeOk;
    private readonly bool includeTemplateExports;
    private readonly RealismPatchGenerator generator;
    private readonly ItemExceptionDocument itemExceptions;
    private readonly Dictionary<string, JsonObject?> sourceFileCache = new(StringComparer.OrdinalIgnoreCase);

    public OutputRuleAuditor(string basePath, OutputAuditOptions? options = null)
    {
        this.basePath = Path.GetFullPath(basePath);
        outputDirectory = Path.GetFullPath(options?.OutputDirectory ?? Path.Combine(this.basePath, "output"));
        includeOk = options?.IncludeOk ?? false;
        includeTemplateExports = options?.IncludeTemplateExports ?? false;
        generator = new RealismPatchGenerator(this.basePath);
        itemExceptions = ItemExceptionStore.Load(this.basePath);
    }

    public AuditReport Audit()
    {
        var jsonFiles = Directory.Exists(outputDirectory)
            ? Directory.EnumerateFiles(outputDirectory, "*.json", SearchOption.AllDirectories)
                .Where(ShouldAuditFile)
                .OrderBy(path => Path.GetRelativePath(outputDirectory, path), StringComparer.OrdinalIgnoreCase)
                .ToList()
            : [];

        var files = new List<AuditFileReport>();
        var allWarningDetails = new List<AuditWarningDetail>();
        var totalItems = 0;
        var totalViolations = 0;
        var totalWarnings = 0;

        foreach (var jsonFile in jsonFiles)
        {
            var fileReport = AuditFile(jsonFile);
            if (fileReport is null)
            {
                continue;
            }

            files.Add(fileReport);
            totalItems += fileReport.ItemCount;
            totalViolations += fileReport.ViolationCount;
            totalWarnings += fileReport.WarningCount;
            foreach (var item in fileReport.Items)
            {
                allWarningDetails.AddRange(item.WarningDetails);
            }
        }

        return new AuditReport
        {
            OutputDir = outputDirectory,
            ScanMode = includeTemplateExports ? "all_json" : "realism_patch_only",
            FileCount = files.Count,
            ItemCount = totalItems,
            ViolationCount = totalViolations,
            WarningCount = totalWarnings,
            WarningBreakdown = BuildWarningBreakdown(allWarningDetails),
            Files = files,
        };
    }

    public static string BuildConsoleSummary(AuditReport report, int summaryLimit)
    {
        var lines = new List<string>
        {
            new('=', 72),
            "输出结果规则审计",
            new('=', 72),
            $"扫描目录: {report.OutputDir}",
            $"扫描文件: {report.FileCount} 个",
            $"扫描物品: {report.ItemCount} 个",
            $"违规字段: {report.ViolationCount} 处",
            $"警告条目: {report.WarningCount} 条",
        };

        if (report.WarningBreakdown.ByGroup.Count > 0)
        {
            lines.Add("警告分组:");
            foreach (var pair in report.WarningBreakdown.ByGroup)
            {
                lines.Add($"  - {pair.Key}: {pair.Value} 条");
            }
        }

        var findings = new List<string>();
        foreach (var fileReport in report.Files)
        {
            foreach (var item in fileReport.Items)
            {
                if (item.Violations.Count > 0)
                {
                    var first = item.Violations[0];
                    findings.Add($"[违规] {fileReport.File} | {item.ItemId} | {(string.IsNullOrWhiteSpace(item.Name) ? "<unnamed>" : item.Name)} | {first.Message}");
                }
                else if (item.Warnings.Count > 0)
                {
                    findings.Add($"[警告] {fileReport.File} | {item.ItemId} | {(string.IsNullOrWhiteSpace(item.Name) ? "<unnamed>" : item.Name)} | {item.Warnings[0]}");
                }
            }
        }

        lines.Add("-");
        if (findings.Count == 0)
        {
            lines.Add("未发现超出规则范围的物品。");
        }
        else
        {
            lines.Add($"前 {Math.Min(summaryLimit, findings.Count)} 条结果:");
            lines.AddRange(findings.Take(summaryLimit));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private bool ShouldAuditFile(string path)
    {
        if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (includeTemplateExports)
        {
            return true;
        }

        return Path.GetFileNameWithoutExtension(path).EndsWith("_realism_patch", StringComparison.OrdinalIgnoreCase);
    }

    private AuditFileReport? AuditFile(string jsonFile)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(jsonFile));
        }
        catch
        {
            return new AuditFileReport
            {
                File = ToRelativeBasePath(jsonFile),
                SourceFile = DeriveSourceFile(jsonFile),
                ItemCount = 0,
                FlaggedItemCount = 0,
                ViolationCount = 0,
                WarningCount = 1,
                WarningBreakdown = BuildWarningBreakdown([MakeWarningDetail("数据异常", "invalid_json", "文件无法解析为 JSON，已跳过")]),
                Warnings = ["文件无法解析为 JSON，已跳过"],
                Items = [],
            };
        }

        if (root is not JsonObject itemsObject)
        {
            return null;
        }

        var fileItems = new List<AuditItemReport>();
        var fileWarningDetails = new List<AuditWarningDetail>();
        var sourceFile = DeriveSourceFile(jsonFile);
        var flaggedItemCount = 0;
        var violationCount = 0;
        var warningCount = 0;

        foreach (var pair in itemsObject)
        {
            if (pair.Value is not JsonObject patch)
            {
                var detail = MakeWarningDetail("数据异常", "invalid_item_payload", "物品数据不是对象，无法审计");
                var warningItem = new AuditItemReport
                {
                    ItemId = pair.Key,
                    Name = string.Empty,
                    Type = string.Empty,
                    Status = "warning",
                    Warnings = [detail.Message],
                    WarningDetails = [detail],
                    Violations = [],
                    Context = new JsonObject { ["source_file"] = sourceFile },
                };
                fileItems.Add(warningItem);
                fileWarningDetails.Add(detail);
                flaggedItemCount += 1;
                warningCount += 1;
                continue;
            }

            var itemReport = AuditItem(pair.Key, patch, sourceFile);
            if (includeOk || !string.Equals(itemReport.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                fileItems.Add(itemReport);
            }

            if (!string.Equals(itemReport.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                flaggedItemCount += 1;
            }

            violationCount += itemReport.Violations.Count;
            warningCount += itemReport.Warnings.Count;
            fileWarningDetails.AddRange(itemReport.WarningDetails);
        }

        return new AuditFileReport
        {
            File = ToRelativeBasePath(jsonFile),
            SourceFile = sourceFile,
            ItemCount = itemsObject.Count,
            FlaggedItemCount = flaggedItemCount,
            ViolationCount = violationCount,
            WarningCount = warningCount,
            WarningBreakdown = BuildWarningBreakdown(fileWarningDetails),
            Warnings = [],
            Items = fileItems,
        };
    }

    private AuditItemReport AuditItem(string itemId, JsonObject patch, string sourceFile)
    {
        var sourceItems = LoadSourceItems(sourceFile);
		JsonNode? sourceItemNode = null;
		sourceItems?.TryGetPropertyValue(itemId, out sourceItemNode);
        var sourceItem = sourceItemNode as JsonObject;
        var itemInfo = generator.BuildAuditItemInfo(itemId, patch, sourceFile, sourceItem);
        var itemType = patch["$type"]?.GetValue<string?>() ?? string.Empty;
        var violations = new List<AuditViolation>();
        var warnings = new List<string>();
        var warningDetails = new List<AuditWarningDetail>();
        var context = new JsonObject
        {
            ["source_file"] = sourceFile,
        };
        var exceptionFields = itemExceptions.GetOverrideFieldSet(itemId);
        if (exceptionFields.Count > 0)
        {
            var fieldsArray = new JsonArray();
            foreach (var field in exceptionFields.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                fieldsArray.Add(field);
            }

            context["exception_fields"] = fieldsArray;
        }

        var auditExemption = GetAuditExemption(itemInfo, patch);
        if (!string.IsNullOrWhiteSpace(auditExemption))
        {
            context["audit_exemption"] = auditExemption;
            return BuildItemReport(itemId, patch, itemType, violations, warnings, warningDetails, context);
        }

        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            CollectRangeViolations(violations, patch, generator.GetWeaponClampRules(), "global_clamp", exceptionFields);
            var weaponProfile = generator.AuditInferWeaponProfile(patch, itemInfo);
            context["weapon_profile"] = weaponProfile;
            if (!string.IsNullOrWhiteSpace(weaponProfile))
            {
                var (expectedRanges, caliberProfile, stockProfile) = generator.BuildWeaponExpectedRanges(patch, itemInfo, weaponProfile!);
                context["caliber_profile"] = caliberProfile;
                context["stock_profile"] = stockProfile;
                CollectRangeViolations(violations, patch, expectedRanges, "weapon_rule", exceptionFields);
            }
            else
            {
                var detail = BuildProfileGapWarningDetail(patch, "weapon", "weapon_profile_unresolved", "无法推断武器规则档位，未能校验武器范围");
                warnings.Add(detail.Message);
                warningDetails.Add(detail);
            }

            if (!IsFieldExempt(exceptionFields, "RecoilAngle")
                && TryGetNumericValue(patch["RecoilAngle"], out var recoilAngle)
                && (recoilAngle < 30 || recoilAngle > 150))
            {
                violations.Add(BuildRangeViolation("RecoilAngle", patch["RecoilAngle"], 30, 150, "weapon_special"));
            }

            if (!IsFieldExempt(exceptionFields, "HasShoulderContact")
                && string.Equals(weaponProfile, "pistol", StringComparison.OrdinalIgnoreCase)
                && patch["HasShoulderContact"]?.GetValue<bool?>() != false)
            {
                violations.Add(new AuditViolation
                {
                    Field = "HasShoulderContact",
                    Value = patch["HasShoulderContact"]?.DeepClone(),
                    Expected = JsonValue.Create(false),
                    Rule = "weapon_special",
                    Message = "手枪规则要求 HasShoulderContact=False",
                });
            }

            if (generator.TryGetTemplateFieldSetForAudit(itemInfo, out var weaponFieldSet))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    weaponFieldSet.OrderBy(field => field, StringComparer.OrdinalIgnoreCase).ToArray(),
                    "weapon_structure",
                    exceptionFields);
            }
        }
        else if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            CollectRangeViolations(violations, patch, generator.GetAttachmentClampRules(), "global_clamp", exceptionFields);
            var modProfile = generator.AuditInferModProfile(patch, itemInfo);
            context["mod_profile"] = modProfile;
            context["template_file"] = itemInfo.TemplateFile;
            if (!string.IsNullOrWhiteSpace(modProfile) && generator.TryGetAttachmentProfileRanges(modProfile!, out var modRanges))
            {
                CollectRangeViolations(violations, patch, modRanges, "mod_rule", exceptionFields);
            }
            else
            {
                var detail = BuildModWarningDetail(itemInfo, patch, modProfile);
                if (string.Equals(detail.Category, "mod_profile_unresolved", StringComparison.OrdinalIgnoreCase))
                {
                    context["audit_exemption"] = "mod_profile_unresolved";
                    return BuildItemReport(itemId, patch, itemType, violations, warnings, warningDetails, context);
                }

                warnings.Add(detail.Message);
                warningDetails.Add(detail);
            }

            if (!IsFieldExempt(exceptionFields, "Velocity") && TryGetNumericValue(patch["Velocity"], out var velocity))
            {
                var maxVelocity = (patch["Name"]?.GetValue<string?>() ?? string.Empty).Contains("barrel", StringComparison.OrdinalIgnoreCase) ? 15.0 : 5.0;
                if (velocity < -maxVelocity || velocity > maxVelocity)
                {
                    violations.Add(BuildRangeViolation("Velocity", patch["Velocity"], -maxVelocity, maxVelocity, "mod_special"));
                }
            }

            var modType = patch["ModType"]?.GetValue<string?>() ?? string.Empty;
            if (!IsFieldExempt(exceptionFields, "ModShotDispersion")
                && string.Equals(modType, "barrel_2slot", StringComparison.OrdinalIgnoreCase)
                && TryGetNumericValue(patch["ModShotDispersion"], out var modShotDispersion)
                && (modShotDispersion < 0 || modShotDispersion > 0))
            {
                violations.Add(BuildRangeViolation("ModShotDispersion", patch["ModShotDispersion"], 0, 0, "mod_special"));
            }

            if (string.Equals(modType, "bipod", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var fieldName in new[] { "AutoROF", "SemiROF", "ModMalfunctionChance", "ReloadSpeed", "FixSpeed" })
                {
                    if (!IsFieldExempt(exceptionFields, fieldName)
                        && TryGetNumericValue(patch[fieldName], out var exactZeroValue)
                        && (exactZeroValue < 0 || exactZeroValue > 0))
                    {
                        violations.Add(BuildRangeViolation(fieldName, patch[fieldName], 0, 0, "mod_special"));
                    }
                }
            }

            if (!IsFieldExempt(exceptionFields, "CanCycleSubs")
                && string.Equals(modProfile, "muzzle_suppressor", StringComparison.OrdinalIgnoreCase)
                && patch.ContainsKey("CanCycleSubs")
                && patch["CanCycleSubs"]?.GetValue<bool?>() != true)
            {
                violations.Add(new AuditViolation
                {
                    Field = "CanCycleSubs",
                    Value = patch["CanCycleSubs"]?.DeepClone(),
                    Expected = JsonValue.Create(true),
                    Rule = "mod_special",
                    Message = "消音器规则要求 CanCycleSubs=True",
                });
            }

            if (string.Equals(modProfile, "stock_adapter", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["DurabilityBurnModificator", "Loudness"],
                    "stock_adapter_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "buffer_adapter", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Dispersion", "CameraRecoil", "HasShoulderContact", "BlocksFolding", "AutoROF", "SemiROF", "ModMalfunctionChance", "StockAllowADS"],
                    "buffer_adapter_structure",
                    exceptionFields);
            }

            if (string.Equals(modType, "buffer", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["DurabilityBurnModificator"],
                    "buffer_structure",
                    exceptionFields);
            }

            if (string.Equals(modType, "hydraulic_buffer", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["DurabilityBurnModificator", "Convergence"],
                    "hydraulic_buffer_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "gasblock", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Loudness", "Velocity"],
                    "gasblock_structure",
                    exceptionFields);
            }

            if (!string.IsNullOrEmpty(modProfile)
                && modProfile.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy", "Dispersion"],
                    "handguard_structure",
                    exceptionFields);
            }

            if (string.Equals(modType, "shot_pump_grip_adapt", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ChamberSpeed", "ReloadSpeed"],
                    "shot_pump_grip_adapt_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "foregrip", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy"],
                    "foregrip_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "pistol_grip", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Dispersion"],
                    "pistol_grip_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "ubgl", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy", "CameraRecoil", "HasShoulderContact", "BlocksFolding", "AutoROF", "SemiROF", "ModMalfunctionChance", "StockAllowADS"],
                    "ubgl_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "receiver", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ChamberSpeed"],
                    "receiver_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "bayonet", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy", "Ergonomics", "Loudness", "MeleeDamage", "MeleePen", "Flash"],
                    "bayonet_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "trigger", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["SemiROF", "Accuracy"],
                    "trigger_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "mount", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["HeatFactor", "CoolFactor", "AimSpeed", "DurabilityBurnModificator"],
                    "mount_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "hammer", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["SemiROF", "Accuracy"],
                    "hammer_structure",
                    exceptionFields);
            }

            if (!string.IsNullOrEmpty(modProfile) && modProfile.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ShotgunDispersion"],
                    "barrel_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "charging_handle", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ReloadSpeed"],
                    "charging_handle_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "flashlight_laser", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["VerticalRecoil", "HorizontalRecoil"],
                    "flashlight_laser_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "scope_red_dot", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy", "AimSpeed", "AimStability", "Ergonomics"],
                    "scope_red_dot_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "scope_magnified", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy", "AimSpeed", "AimStability", "Ergonomics"],
                    "scope_magnified_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "booster", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["AutoROF", "SemiROF", "ModMalfunctionChance"],
                    "booster_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "catch", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ReloadSpeed", "ChamberSpeed", "Accuracy", "FixSpeed"],
                    "catch_structure",
                    exceptionFields);
            }

            if (string.Equals(modType, "barrel_2slot", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["ModShotDispersion"],
                    "barrel_2slot_structure",
                    exceptionFields);
            }

            if (string.Equals(modType, "bipod", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["AutoROF", "SemiROF", "ModMalfunctionChance", "ReloadSpeed", "FixSpeed"],
                    "bipod_structure",
                    exceptionFields);
            }

            if (string.Equals(modProfile, "iron_sight", StringComparison.OrdinalIgnoreCase))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    ["Accuracy"],
                    "iron_sight_structure",
                    exceptionFields);
            }

            if (generator.TryGetTemplateFieldSetForAudit(itemInfo, out var modFieldSet))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    modFieldSet.OrderBy(field => field, StringComparer.OrdinalIgnoreCase).ToArray(),
                    "mod_structure",
                    exceptionFields);
            }
        }
        else if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            var ammoProfile = generator.AuditInferAmmoProfile(patch, itemInfo);
            var penetrationProbe = (JsonObject)patch.DeepClone();
            foreach (var key in new[] { "PenetrationPower", "Penetration", "penPower" })
            {
                if (itemInfo.SourceProperties[key] is not null)
                {
                    penetrationProbe[key] = itemInfo.SourceProperties[key]!.DeepClone();
                    break;
                }
            }

            var penetrationTier = generator.AuditInferAmmoPenetrationTier(penetrationProbe, itemInfo);
            var specialProfile = generator.AuditInferAmmoSpecialProfile(patch, itemInfo);
            context["ammo_profile"] = ammoProfile;
            context["penetration_tier"] = penetrationTier;
            context["special_profile"] = specialProfile;

            if (generator.TryGetAmmoProfileRanges(ammoProfile, out _))
            {
                var expectedRanges = generator.BuildAmmoExpectedRanges(ammoProfile, penetrationTier, specialProfile);
                CollectRangeViolations(violations, patch, expectedRanges, "ammo_rule", exceptionFields);
            }
            else
            {
                var detail = BuildProfileGapWarningDetail(patch, "ammo", "ammo_profile_unresolved", "无法推断弹药规则档位，未能校验弹药范围");
                warnings.Add(detail.Message);
                warningDetails.Add(detail);
            }

            CollectStructureViolations(violations, patch, generator.GetAmmoOutputFieldSet(), "ammo_structure");
        }
        else if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            CollectRangeViolations(violations, patch, generator.GetGearClampRules(), "gear_clamp", exceptionFields);
            var gearProfile = generator.AuditInferGearProfile(patch, itemInfo);
            context["gear_profile"] = gearProfile;
            if (!string.IsNullOrWhiteSpace(gearProfile) && generator.TryGetGearProfileRanges(gearProfile!, out var gearRanges))
            {
                CollectRangeViolations(violations, patch, gearRanges, "gear_rule", exceptionFields);
            }
            else
            {
                var detail = BuildProfileGapWarningDetail(patch, "gear", "gear_profile_unresolved", "无法推断装备规则档位，未能校验装备范围");
                warnings.Add(detail.Message);
                warningDetails.Add(detail);
            }

            if (generator.TryGetTemplateFieldSetForAudit(itemInfo, out var gearFieldSet))
            {
                CollectMissingFieldViolations(
                    violations,
                    patch,
                    gearFieldSet.OrderBy(field => field, StringComparer.OrdinalIgnoreCase).ToArray(),
                    "gear_structure",
                    exceptionFields);
            }
        }
        else
        {
            var detail = MakeWarningDetail("未配置专项审计", "unsupported_item_type", "当前类型未配置专项审计，仅保留基础信息");
            warnings.Add(detail.Message);
            warningDetails.Add(detail);
        }

        return BuildItemReport(itemId, patch, itemType, violations, warnings, warningDetails, context);
    }

    private AuditItemReport BuildItemReport(
        string itemId,
        JsonObject patch,
        string itemType,
        List<AuditViolation> violations,
        List<string> warnings,
        List<AuditWarningDetail> warningDetails,
        JsonObject context)
    {
        return new AuditItemReport
        {
            ItemId = itemId,
            Name = patch["Name"]?.GetValue<string?>() ?? string.Empty,
            Type = itemType,
            Status = violations.Count > 0 ? "violation" : (warnings.Count > 0 ? "warning" : "ok"),
            Warnings = warnings,
            WarningDetails = warningDetails,
            Violations = violations,
            Context = context,
        };
    }

    private JsonObject? LoadSourceItems(string sourceFile)
    {
        if (sourceFileCache.TryGetValue(sourceFile, out var cached))
        {
            return cached;
        }

        var sourcePath = Path.Combine(basePath, "input", sourceFile.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
        {
            sourceFileCache[sourceFile] = null;
            return null;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(sourcePath)) as JsonObject;
            sourceFileCache[sourceFile] = root;
            return root;
        }
        catch
        {
            sourceFileCache[sourceFile] = null;
            return null;
        }
    }

    private string DeriveSourceFile(string jsonFile)
    {
        var relative = Path.GetRelativePath(outputDirectory, jsonFile).Replace('\\', '/');
        var extension = Path.GetExtension(relative);
        var fileName = Path.GetFileNameWithoutExtension(relative);
        if (fileName.EndsWith("_realism_patch", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[..^"_realism_patch".Length];
        }

        var directory = Path.GetDirectoryName(relative)?.Replace('\\', '/');
        return string.IsNullOrWhiteSpace(directory)
            ? fileName + extension
            : $"{directory}/{fileName}{extension}";
    }

    private string ToRelativeBasePath(string path)
    {
        return Path.GetRelativePath(basePath, path).Replace('\\', '/');
    }

    private static WarningBreakdown BuildWarningBreakdown(IEnumerable<AuditWarningDetail> warningDetails)
    {
        var byGroup = warningDetails
            .GroupBy(detail => detail.Group)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var byCategory = warningDetails
            .GroupBy(detail => detail.Category)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new WarningBreakdown
        {
            ByGroup = byGroup,
            ByCategory = byCategory,
        };
    }

    private string? GetAuditExemption(ItemInfo itemInfo, JsonObject patch)
    {
        if (itemInfo.IsConsumable)
        {
            return "consumable";
        }

        var templateFile = itemInfo.TemplateFile ?? string.Empty;
        var itemName = patch["Name"]?.GetValue<string?>()?.Trim().ToLowerInvariant() ?? string.Empty;
        if (templateFile.Contains("cosmeticsTemplates.json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemInfo.ParentId, ArmbandParentId, StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("patch", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("补丁", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("贴章", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("臂章", StringComparison.OrdinalIgnoreCase))
        {
            return "cosmetic";
        }

        return null;
    }

    private static AuditWarningDetail BuildProfileGapWarningDetail(JsonObject patch, string profileKind, string category, string defaultMessage)
    {
        var itemName = patch["Name"]?.GetValue<string?>()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return MakeWarningDetail("信息不足", $"{profileKind}_empty_name", $"名称为空，{defaultMessage}");
        }

        if (IdLikeNameRegex.IsMatch(itemName))
        {
            return MakeWarningDetail("信息不足", $"{profileKind}_id_like_name", $"名称疑似物品ID，{defaultMessage}");
        }

        return MakeWarningDetail("未识别规则档位", category, defaultMessage);
    }

    private static AuditWarningDetail BuildModWarningDetail(ItemInfo itemInfo, JsonObject patch, string? modProfile)
    {
        var templateFile = itemInfo.TemplateFile ?? string.Empty;
        var itemName = patch["Name"]?.GetValue<string?>()?.Trim() ?? string.Empty;
        var modType = patch["ModType"]?.GetValue<string?>()?.Trim() ?? string.Empty;
        var parentId = itemInfo.ParentId ?? string.Empty;

        if (string.IsNullOrWhiteSpace(itemName))
        {
            return MakeWarningDetail("信息不足", "mod_empty_name", "名称为空，无法推断附件规则档位");
        }

        if (IdLikeNameRegex.IsMatch(itemName))
        {
            return MakeWarningDetail("信息不足", "mod_id_like_name", "名称疑似物品ID，无法推断附件规则档位");
        }

        if (string.Equals(templateFile, "AuxiliaryModTemplates.json", StringComparison.OrdinalIgnoreCase))
        {
            return MakeWarningDetail("无规则类别", "unsupported_auxiliary_mod", "当前物品属于辅助小件类，尚未配置附件规则范围");
        }

        if ((string.Equals(templateFile, "UBGLTempaltes.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(templateFile, "UBGLTemplates.json", StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrWhiteSpace(modProfile))
        {
            return MakeWarningDetail("无规则类别", "unsupported_ubgl", "当前物品属于下挂榴弹发射器类，尚未配置附件规则范围");
        }

        var loweredName = itemName.ToLowerInvariant();
        if (loweredName.Contains("patch", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("补丁", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("贴章", StringComparison.OrdinalIgnoreCase)
            || itemName.Contains("臂章", StringComparison.OrdinalIgnoreCase))
        {
            return MakeWarningDetail("无规则类别", "unsupported_cosmetic_patch", "当前物品更像装饰类部件，尚未配置附件规则范围");
        }

        if (loweredName.Contains("lens cap", StringComparison.OrdinalIgnoreCase))
        {
            return MakeWarningDetail("无规则类别", "unsupported_optic_accessory", "当前物品属于瞄具附件小件类，尚未配置附件规则范围");
        }

        if (string.IsNullOrWhiteSpace(templateFile) && string.IsNullOrWhiteSpace(modType) && string.IsNullOrWhiteSpace(parentId))
        {
            return MakeWarningDetail("信息不足", "mod_missing_name_metadata_signals", "名称缺少有效类别关键词，且缺少模板/父类/ModType 信息，无法推断附件规则档位");
        }

        if (!string.IsNullOrWhiteSpace(modProfile))
        {
            return MakeWarningDetail("无规则类别", "recognized_mod_profile_without_rule", $"已识别附件档位 {modProfile}，但当前没有对应规则范围");
        }

        return MakeWarningDetail("未识别规则档位", "mod_profile_unresolved", "无法推断附件规则档位，未能校验附件范围");
    }

    private static AuditWarningDetail MakeWarningDetail(string group, string category, string message)
    {
        return new AuditWarningDetail
        {
            Group = group,
            Category = category,
            Message = message,
        };
    }

    private static void CollectRangeViolations(List<AuditViolation> violations, JsonObject patch, IReadOnlyDictionary<string, NumericRange> expectedRanges, string ruleName, IReadOnlySet<string>? exemptFields)
    {
        foreach (var pair in expectedRanges)
        {
            if (IsFieldExempt(exemptFields, pair.Key))
            {
                continue;
            }

            if (!patch.ContainsKey(pair.Key) || !TryGetNumericValue(patch[pair.Key], out var current))
            {
                continue;
            }

            if (current < pair.Value.Min - RangeComparisonEpsilon || current > pair.Value.Max + RangeComparisonEpsilon)
            {
                violations.Add(BuildRangeViolation(pair.Key, patch[pair.Key], pair.Value.Min, pair.Value.Max, ruleName));
            }
        }
    }

    private static void CollectStructureViolations(List<AuditViolation> violations, JsonObject patch, IReadOnlySet<string> expectedFields, string ruleName)
    {
        foreach (var field in expectedFields)
        {
            if (!patch.ContainsKey(field))
            {
                violations.Add(new AuditViolation
                {
                    Field = field,
                    Expected = JsonValue.Create("present"),
                    Rule = ruleName,
                    Message = $"输出缺少标准字段 {field}",
                });
            }
        }

        foreach (var pair in patch)
        {
            if (!expectedFields.Contains(pair.Key))
            {
                violations.Add(new AuditViolation
                {
                    Field = pair.Key,
                    Value = pair.Value?.DeepClone(),
                    Rule = ruleName,
                    Message = $"输出包含非标准字段 {pair.Key}",
                });
            }
        }
    }

    private static void CollectMissingFieldViolations(List<AuditViolation> violations, JsonObject patch, IReadOnlyList<string> expectedFields, string ruleName, IReadOnlySet<string>? exemptFields)
    {
        foreach (var field in expectedFields)
        {
            if (IsFieldExempt(exemptFields, field))
            {
                continue;
            }

            if (!patch.ContainsKey(field) && !HasMissingFieldViolation(violations, field))
            {
                violations.Add(new AuditViolation
                {
                    Field = field,
                    Expected = JsonValue.Create("present"),
                    Rule = ruleName,
                    Message = $"输出缺少必需字段 {field}",
                });
            }
        }
    }

    private static bool HasMissingFieldViolation(List<AuditViolation> violations, string field)
    {
        foreach (var violation in violations)
        {
            if (!string.Equals(violation.Field, field, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (violation.Expected is JsonValue expectedValue
                && expectedValue.TryGetValue<string>(out var expectedText)
                && string.Equals(expectedText, "present", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFieldExempt(IReadOnlySet<string>? exemptFields, string field)
    {
        return exemptFields is not null && exemptFields.Contains(field);
    }

    private static AuditViolation BuildRangeViolation(string field, JsonNode? value, double min, double max, string ruleName)
    {
        return new AuditViolation
        {
            Field = field,
            Value = value?.DeepClone(),
            ExpectedMin = min,
            ExpectedMax = max,
            Rule = ruleName,
            Message = $"{field}={FormatValue(value)} 超出允许范围 [{min.ToString(CultureInfo.InvariantCulture)}, {max.ToString(CultureInfo.InvariantCulture)}]",
        };
    }

    private static string FormatValue(JsonNode? value)
    {
        return value switch
        {
            null => "null",
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var text) => text,
            _ => value.ToJsonString(),
        };
    }

    private static bool TryGetNumericValue(JsonNode? node, out double value)
    {
        switch (node)
        {
            case null:
                value = 0;
                return false;
            case JsonValue jsonValue when jsonValue.TryGetValue<double>(out var doubleValue):
                value = doubleValue;
                return true;
            case JsonValue jsonValue when jsonValue.TryGetValue<int>(out var intValue):
                value = intValue;
                return true;
            case JsonValue jsonValue when jsonValue.TryGetValue<long>(out var longValue):
                value = longValue;
                return true;
            case JsonValue jsonValue when jsonValue.TryGetValue<decimal>(out var decimalValue):
                value = (double)decimalValue;
                return true;
            default:
                value = 0;
                return false;
        }
    }
}