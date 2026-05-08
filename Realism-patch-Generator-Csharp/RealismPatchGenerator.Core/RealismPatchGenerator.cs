using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RealismPatchGenerator.Core;

public sealed class RealismPatchGenerator
{
    private static readonly Regex AlphaNumericTokenRegex = new("[a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MagCapacityNameRegex = new(@"\b(\d{1,3})(?:\s*|-)?(?:round|rnd|rds)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MagCapacityCnRegex = new(@"(?<!\d)(\d{1,3})\s*发", RegexOptions.Compiled);
    private static readonly Regex MagCapacityRuRegex = new(@"(?<![\d.])(\d{1,3})\s*патрон", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BarrelLengthRegex = new(@"(\d+(?:\.\d+)?)\s*(mm|inch|in|"")", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string basePath;
    private readonly string inputPath;
    private readonly string templatesBasePath;
    private readonly Dictionary<string, JsonObject> templateById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> templateFileByItemId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> weaponPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> attachmentPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> ammoPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> gearPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> consumablePatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly PatchOutputBuffer patchOutputBuffer = new();
    private readonly Dictionary<string, ItemInfo> generatedItemInfoById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> templateAliasCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unresolvedTemplateAliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<TemplateAliasCandidate>> templateAliasCandidatesByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> templateParentIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SortedDictionary<string, JsonObject>> templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> logs = [];
    private readonly uint generationSeed;
    private readonly CompatibleRandom random;
    private readonly RuleSet rules;
    private readonly ItemExceptionDocument itemExceptions;
    private long totalFileProcessingTicks;
    private long totalRuleApplicationTicks;
    private long slowestFileProcessingTicks;
    private int processedFileCount;
    private string? slowestProcessedFile;

    internal IReadOnlyDictionary<string, string> TemplateFileByItemId => templateFileByItemId;

    public RealismPatchGenerator(string basePath, uint? seed = null)
    {
        this.basePath = Path.GetFullPath(basePath);
        inputPath = Path.Combine(this.basePath, "input");
        templatesBasePath = RuleWorkspace.GetTemplatesDirectory(this.basePath);
        generationSeed = seed ?? CreateRuntimeSeed();
        random = new CompatibleRandom(generationSeed);
        rules = RuleSetLoader.Load(this.basePath, Log);
        itemExceptions = ItemExceptionStore.Load(this.basePath);
    }

    public GenerationResult Generate(string? outputDirectory = null, Func<string, bool>? inputPathFilter = null)
    {
        ResetPerformanceCounters();
        var allocatedBytesBefore = GC.GetTotalAllocatedBytes(false);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var totalStopwatch = Stopwatch.StartNew();

        EnsureRequiredDirectories();

        var templateLoadStopwatch = Stopwatch.StartNew();
        LoadAllTemplates();
        templateLoadStopwatch.Stop();

        Log($"开始生成现实主义补丁，工作目录: {basePath}");
        Log($"本次生成随机种子: {generationSeed}");
        var inputDiscoveryStopwatch = Stopwatch.StartNew();
        var jsonFiles = Directory.EnumerateFiles(inputPath, "*.json", SearchOption.AllDirectories)
            .Where(path => inputPathFilter is null || inputPathFilter(Path.GetRelativePath(inputPath, path).Replace('\\', '/')))
            .OrderBy(path => Path.GetRelativePath(inputPath, path), StringComparer.OrdinalIgnoreCase)
            .ToList();
        inputDiscoveryStopwatch.Stop();

        Log($"找到 {jsonFiles.Count} 个输入 JSON 文件");
        var fileProcessingStopwatch = Stopwatch.StartNew();
        foreach (var filePath in jsonFiles)
        {
            ProcessItemFile(filePath);
        }
        fileProcessingStopwatch.Stop();

        var outputPath = Path.GetFullPath(outputDirectory ?? Path.Combine(basePath, "output"));
        var outputWriteStopwatch = Stopwatch.StartNew();
        SavePatches(outputPath);
        outputWriteStopwatch.Stop();
        totalStopwatch.Stop();

        var performance = new GenerationPerformanceMetrics
        {
            InputFileCount = jsonFiles.Count,
            ProcessedFileCount = processedFileCount,
            TotalDuration = totalStopwatch.Elapsed,
            TemplateLoadDuration = templateLoadStopwatch.Elapsed,
            InputDiscoveryDuration = inputDiscoveryStopwatch.Elapsed,
            FileProcessingDuration = fileProcessingStopwatch.Elapsed,
            RuleApplicationDuration = TimeSpan.FromTicks(totalRuleApplicationTicks),
            OutputWriteDuration = outputWriteStopwatch.Elapsed,
            SlowestInputFile = slowestProcessedFile,
            SlowestInputFileDuration = TimeSpan.FromTicks(slowestFileProcessingTicks),
            AllocatedBytes = GC.GetTotalAllocatedBytes(false) - allocatedBytesBefore,
            Gen0Collections = GC.CollectionCount(0) - gen0Before,
            Gen1Collections = GC.CollectionCount(1) - gen1Before,
            Gen2Collections = GC.CollectionCount(2) - gen2Before,
        };

        var statistics = new GenerationStatistics
        {
            WeaponCount = weaponPatches.Count,
            AttachmentCount = attachmentPatches.Count,
            AmmoCount = ammoPatches.Count,
            GearCount = gearPatches.Count,
            ConsumableCount = consumablePatches.Count,
        };

        Log($"生成完成，总计 {statistics.TotalCount} 个补丁");
        Log($"性能统计: 总耗时 {performance.TotalDuration.TotalMilliseconds:F1} ms, 模板加载 {performance.TemplateLoadDuration.TotalMilliseconds:F1} ms, 文件处理 {performance.FileProcessingDuration.TotalMilliseconds:F1} ms, 规则执行 {performance.RuleApplicationDuration.TotalMilliseconds:F1} ms, 输出写出 {performance.OutputWriteDuration.TotalMilliseconds:F1} ms, 分配 {performance.AllocatedBytes / 1024d / 1024d:F2} MB");
        return new GenerationResult
        {
            BasePath = basePath,
            OutputPath = outputPath,
            UsedSeed = generationSeed,
            Statistics = statistics,
            Performance = performance,
            Logs = logs.ToArray(),
        };
    }

    private void ResetPerformanceCounters()
    {
        totalFileProcessingTicks = 0;
        totalRuleApplicationTicks = 0;
        slowestFileProcessingTicks = 0;
        processedFileCount = 0;
        slowestProcessedFile = null;
    }

    internal bool TryGetAttachmentProfileRanges(string modProfile, out IReadOnlyDictionary<string, NumericRange> ranges)
    {
        if (rules.Attachment.ModProfileRanges.TryGetValue(modProfile, out var resolvedRanges))
        {
            ranges = resolvedRanges;
            return true;
        }

		ranges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    internal bool TryGetAmmoProfileRanges(string ammoProfile, out IReadOnlyDictionary<string, NumericRange> ranges)
    {
        if (rules.Ammo.AmmoProfileRanges.TryGetValue(ammoProfile, out var resolvedRanges))
        {
            ranges = resolvedRanges;
            return true;
        }

		ranges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    internal bool TryGetGearProfileRanges(string gearProfile, out IReadOnlyDictionary<string, NumericRange> ranges)
    {
        if (rules.Gear.GearProfileRanges.TryGetValue(gearProfile, out var resolvedRanges))
        {
            ranges = resolvedRanges;
            return true;
        }

		ranges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    internal (IReadOnlyDictionary<string, NumericRange> Ranges, string? CaliberProfile, string StockProfile) BuildWeaponExpectedRanges(JsonObject patch, ItemInfo itemInfo, string weaponProfile)
    {
        var ranges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in rules.Weapon.WeaponProfileRanges[weaponProfile])
        {
            ranges[pair.Key] = pair.Value;
        }

        var caliberProfile = InferWeaponCaliberProfile(patch, itemInfo);
        var stockProfile = InferWeaponStockProfile(patch);
        var caliberMods = !string.IsNullOrWhiteSpace(caliberProfile) && rules.Weapon.WeaponCaliberRuleModifiers.TryGetValue(caliberProfile, out var resolvedCaliberMods)
            ? resolvedCaliberMods
            : null;
        var stockMods = rules.Weapon.WeaponStockRuleModifiers.TryGetValue(stockProfile, out var resolvedStockMods)
            ? resolvedStockMods
            : null;

        foreach (var pair in rules.Weapon.WeaponProfileRanges[weaponProfile])
        {
            var min = pair.Value.Min;
            var max = pair.Value.Max;
            var preferInt = pair.Value.PreferInt;
            if (caliberMods is not null && caliberMods.TryGetValue(pair.Key, out var caliberRange))
            {
                min += caliberRange.Min;
                max += caliberRange.Max;
            }

            if (stockMods is not null && stockMods.TryGetValue(pair.Key, out var stockRange))
            {
                min += stockRange.Min;
                max += stockRange.Max;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            ranges[pair.Key] = new NumericRange(min, max, preferInt);
        }

        var supplementalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (caliberMods is not null)
        {
            supplementalKeys.UnionWith(caliberMods.Keys);
        }

        if (stockMods is not null)
        {
            supplementalKeys.UnionWith(stockMods.Keys);
        }

        supplementalKeys.ExceptWith(ranges.Keys);
        foreach (var key in supplementalKeys)
        {
            var min = 0.0;
            var max = 0.0;
            if (caliberMods is not null && caliberMods.TryGetValue(key, out var caliberRange))
            {
                min += caliberRange.Min;
                max += caliberRange.Max;
            }

            if (stockMods is not null && stockMods.TryGetValue(key, out var stockRange))
            {
                min += stockRange.Min;
                max += stockRange.Max;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            ranges[key] = new NumericRange(min, max);
        }

        return (ranges, caliberProfile, stockProfile);
    }

    internal IReadOnlyDictionary<string, NumericRange> BuildAmmoExpectedRanges(string ammoProfile, string penetrationTier, string? specialProfile)
    {
        var expectedRanges = new Dictionary<string, NumericRange>(StringComparer.OrdinalIgnoreCase);
        var baseRanges = rules.Ammo.AmmoProfileRanges[ammoProfile];
        var penetrationMods = rules.Ammo.AmmoPenetrationModifiers.GetValueOrDefault(penetrationTier);
        var specialMods = !string.IsNullOrWhiteSpace(specialProfile) ? rules.Ammo.AmmoSpecialModifiers.GetValueOrDefault(specialProfile!) : null;
        var malfunctionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MalfMisfireChance", "MisfireChance", "MalfFeedChance" };

        foreach (var pair in baseRanges)
        {
            var min = pair.Value.Min;
            var max = pair.Value.Max;
            if (penetrationMods is not null && penetrationMods.TryGetValue(pair.Key, out var tierRange))
            {
                min += tierRange.Min;
                max += tierRange.Max;
            }

            if (specialMods is not null && specialMods.TryGetValue(pair.Key, out var specialRange))
            {
                min += specialRange.Min;
                max += specialRange.Max;
            }

            if (malfunctionKeys.Contains(pair.Key))
            {
                min = Clamp(min, 0.001, 0.015);
                max = Clamp(max, 0.001, 0.015);
            }

            if (string.Equals(pair.Key, "ArmorDamage", StringComparison.OrdinalIgnoreCase))
            {
                min = Clamp(min, 1.0, 1.2);
                max = Clamp(max, 1.0, 1.2);
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            expectedRanges[pair.Key] = new NumericRange(min, max, pair.Value.PreferInt);
        }

        return expectedRanges;
    }

    private void EnsureRequiredDirectories()
    {
        if (!Directory.Exists(inputPath))
        {
            throw new DirectoryNotFoundException($"未找到输入目录: {inputPath}");
        }

        if (!Directory.Exists(templatesBasePath))
        {
            throw new DirectoryNotFoundException($"未找到模板目录: {templatesBasePath}");
        }
    }

    private void EnsureAuditContextLoaded()
    {
        EnsureRequiredDirectories();
        if (templates.Count == 0)
        {
            LoadAllTemplates();
        }
    }

    private void LoadAllTemplates()
    {
        var snapshot = TemplateCatalog.Load(templatesBasePath, Log);

        templates.Clear();
        templateById.Clear();
        templateFileByItemId.Clear();
        templateAliasCache.Clear();
        unresolvedTemplateAliases.Clear();
        templateAliasCandidatesByToken.Clear();
        templateParentIndex.Clear();

        foreach (var pair in snapshot.Templates)
        {
            templates[pair.Key] = pair.Value;
        }

        foreach (var pair in snapshot.TemplateById)
        {
            templateById[pair.Key] = pair.Value;
        }

        foreach (var pair in snapshot.TemplateFileByItemId)
        {
            templateFileByItemId[pair.Key] = pair.Value;
        }

        foreach (var pair in snapshot.TemplateParentIndex)
        {
            templateParentIndex[pair.Key] = pair.Value;
        }

        BuildTemplateAliasIndex();
    }

    private void BuildTemplateAliasIndex()
    {
        foreach (var pair in templateById)
        {
            var candidateTokens = TokenizeCloneReference(pair.Key);
            var candidateName = pair.Value["Name"]?.GetValue<string?>();
            if (!string.IsNullOrWhiteSpace(candidateName))
            {
                candidateTokens.UnionWith(TokenizeCloneReference(candidateName));
            }

            if (candidateTokens.Count == 0)
            {
                continue;
            }

            var candidate = new TemplateAliasCandidate(pair.Key, candidateTokens);
            foreach (var token in candidateTokens)
            {
                if (!templateAliasCandidatesByToken.TryGetValue(token, out var candidates))
                {
                    candidates = [];
                    templateAliasCandidatesByToken[token] = candidates;
                }

                candidates.Add(candidate);
            }
        }
    }

    private void ProcessItemFile(string itemFile)
    {
        var relativeDisplay = Path.GetRelativePath(inputPath, itemFile);
        var stopwatch = Stopwatch.StartNew();
        Log($"处理文件: {relativeDisplay}");

        JsonObject? itemsData;
        try
        {
            using var stream = File.OpenRead(itemFile);
            itemsData = JsonNode.Parse(stream)?.AsObject();
        }
        catch (Exception ex)
        {
            Log($"读取文件失败: {relativeDisplay} - {ex.Message}");
            return;
        }

        if (itemsData is null)
        {
            Log($"跳过空文件: {relativeDisplay}");
            return;
        }

        var inputFormat = InputFormatRouter.DetectSupportedFileFormat(itemsData, relativeDisplay);
        if (inputFormat == SupportedInputFileFormat.Unsupported)
        {
            Log($"跳过暂不支持的输入结构文件: {relativeDisplay}");
            return;
        }

        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processedCount = 0;
        var sourceKey = Path.ChangeExtension(Path.GetRelativePath(inputPath, itemFile), null) ?? Path.GetFileNameWithoutExtension(itemFile);
        patchOutputBuffer.RegisterSource(sourceKey, InputFormatRouter.ShouldUseSuffixOutput(sourceKey, inputFormat));

        foreach (var pair in itemsData)
        {
            if (pair.Value is not JsonObject itemData)
            {
                continue;
            }

            if (ProcessSingleItem(pair.Key, itemData, processed, sourceKey, inputFormat))
            {
                processedCount += 1;
            }
        }

        Log($"处理完成: {relativeDisplay} - {processedCount} 项");
        stopwatch.Stop();
        totalFileProcessingTicks += stopwatch.ElapsedTicks;
        processedFileCount += 1;
        if (stopwatch.ElapsedTicks > slowestFileProcessingTicks)
        {
            slowestFileProcessingTicks = stopwatch.ElapsedTicks;
            slowestProcessedFile = relativeDisplay.Replace('\\', '/');
        }
    }

    private bool ProcessSingleItem(
        string itemId,
        JsonObject itemData,
        HashSet<string> processedItems,
        string sourceFile,
        SupportedInputFileFormat inputFormat)
    {
        if (processedItems.Contains(itemId))
        {
            return true;
        }

        if (itemData["enable"]?.GetValue<bool?>() == false)
        {
            return false;
        }

        if (!PatchBuildRouter.TryBuildPatchForSupportedFormat(this, itemId, itemData, sourceFile, inputFormat, out var patch, out var itemInfo))
        {
            return false;
        }

        SyncItemInfoCategoryFromPatch(patch, itemInfo);
        FinalizePatch(itemId, patch, itemInfo, processedItems, sourceFile);
        StorePatchByPatchType(itemId, patch);
        generatedItemInfoById[itemId] = itemInfo;
        return true;
    }

    internal bool TryBuildPatchForSupportedFormat(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        SupportedInputFileFormat inputFormat,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        switch (inputFormat)
        {
            case SupportedInputFileFormat.RealismStandardTemplate:
                if (TryBuildStandardTemplateClonePatch(itemId, itemData, sourceFile, out patch, out itemInfo))
                {
                    return true;
                }

                itemInfo = ExtractItemInfo(itemId, itemData, sourceFile);
                patch = (JsonObject)itemData.DeepClone();
                patch["ItemID"] = itemId;
                if (patch["Name"] is null && !string.IsNullOrWhiteSpace(itemInfo.Name))
                {
                    patch["Name"] = itemInfo.Name;
                }

                return true;

            case SupportedInputFileFormat.WttArmory_templates:
                return TryBuildWttArmoryTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Epic_templates:
                return TryBuildEpicTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.ConsortiumOfThings_templates:
                return TryBuildConsortiumOfThingsTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Requisitions_templates:
                return TryBuildRequisitionsTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.EcoAttachment_templates:
                return TryBuildEcoAttachmentTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Artem_templates:
                return TryBuildArtemTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.WttStandalone_templates:
                return TryBuildWttStandaloneTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.SptBattlepass_templates:
                return TryBuildSptBattlepassTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.MoxoTemplate:
                return TryBuildMoxoTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.MixedTemplate:
                return TryBuildMixedTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.RaidOverhaulTemplate:
                return TryBuildRaidOverhaulTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            default:
                patch = new JsonObject();
                itemInfo = new ItemInfo();
                return false;
        }
    }

    internal bool TryBuildMixedTemplatePatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        if (itemData.ContainsKey("clone")
            && TryBuildMoxoTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo))
        {
            return true;
        }

        return TryBuildMixedDirectPatch(itemId, itemData, sourceFile, out patch, out itemInfo);
    }

    internal bool TryBuildRaidOverhaulTemplatePatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var cloneId = itemData["ItemToClone"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(cloneId))
        {
            patch = new JsonObject();
            itemInfo = new ItemInfo();
            Log($"RaidOverhaul_templates 缺少可用 ItemToClone 模板: {itemId} -> <null>");
            return false;
        }

        if (TryResolveTemplateCloneByIdOrAlias(cloneId, out var resolvedCloneId, out var cloneTemplate))
        {
            patch = (JsonObject)cloneTemplate.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractRaidOverhaulItemInfo(itemId, itemData, sourceFile, resolvedCloneId, cloneTemplate);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        if (TryGetStoredPatchById(cloneId, out var generatedClonePatch)
            && generatedItemInfoById.TryGetValue(cloneId, out var generatedCloneInfo))
        {
            patch = (JsonObject)generatedClonePatch.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractRaidOverhaulItemInfo(itemId, itemData, sourceFile, generatedCloneInfo, patch);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        return TryBuildRaidOverhaulFallbackPatch(itemId, itemData, sourceFile, cloneId, out patch, out itemInfo);
    }

    internal bool TryBuildWttArmoryTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "WttArmory_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveWttArmoryTemplatesParentId,
            ResolveWttArmoryTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildEpicTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "Epic_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveEpicTemplatesParentId,
            ResolveEpicTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildRequisitionsTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "Requisitions_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveRequisitionsTemplatesParentId,
            ResolveRequisitionsTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildConsortiumOfThingsTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "ConsortiumOfThings_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveConsortiumOfThingsTemplatesParentId,
            ResolveConsortiumOfThingsTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildEcoAttachmentTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "EcoAttachment_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveEcoAttachmentTemplatesParentId,
            ResolveEcoAttachmentTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildArtemTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "Artem_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveArtemTemplatesParentId,
            ResolveArtemTemplatesTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildWttStandaloneTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "WttStandalone_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveWttStandaloneParentId,
            ResolveWttStandaloneTemplateFile,
            out patch,
            out itemInfo);

    internal bool TryBuildSptBattlepassTemplatesPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
        => TryBuildSupportedWttSubclassPatch(
            "SptBattlepass_templates",
            itemId,
            itemData,
            sourceFile,
            ResolveSptBattlepassTemplatesParentId,
            ResolveSptBattlepassTemplatesTemplateFile,
            out patch,
            out itemInfo);

    private bool TryBuildSupportedWttSubclassPatch(
        string subclassName,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var cloneId = itemData["itemTplToClone"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(cloneId))
        {
            patch = new JsonObject();
            itemInfo = new ItemInfo();
            Log($"{subclassName} 缺少可用 itemTplToClone 模板: {itemId} -> <null>");
            return false;
        }

        if (TryResolveTemplateCloneByIdOrAlias(cloneId, out var resolvedCloneId, out var cloneTemplate))
        {
            patch = (JsonObject)cloneTemplate.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractSupportedWttSubclassItemInfo(itemId, itemData, sourceFile, resolvedCloneId, cloneTemplate, resolveParentId, resolveTemplateFile);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        if (TryGetStoredPatchById(cloneId, out var generatedClonePatch)
            && generatedItemInfoById.TryGetValue(cloneId, out var generatedCloneInfo))
        {
            patch = (JsonObject)generatedClonePatch.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractSupportedWttSubclassItemInfo(itemId, itemData, sourceFile, generatedCloneInfo, patch, resolveParentId, resolveTemplateFile);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        return TryBuildSupportedWttSubclassFallbackPatch(subclassName, itemId, itemData, sourceFile, cloneId, resolveParentId, resolveTemplateFile, out patch, out itemInfo);
    }

    internal bool TryBuildMoxoTemplatePatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var cloneId = itemData["clone"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(cloneId))
        {
            patch = new JsonObject();
            itemInfo = new ItemInfo();
            Log($"Moxo_Template 缺少可用 clone 模板: {itemId} -> <null>");
            return false;
        }

        if (templateById.TryGetValue(cloneId, out var cloneTemplate))
        {
            patch = (JsonObject)cloneTemplate.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractMoxoItemInfo(itemId, itemData, sourceFile, cloneId, patch);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        if (TryGetStoredPatchById(cloneId, out var generatedClonePatch)
            && generatedItemInfoById.TryGetValue(cloneId, out var generatedCloneInfo))
        {
            patch = (JsonObject)generatedClonePatch.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractMoxoItemInfo(itemId, itemData, sourceFile, generatedCloneInfo, patch);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        patch = new JsonObject();
        itemInfo = new ItemInfo();
        Log($"Moxo_Template 缺少可用 clone 模板: {itemId} -> {cloneId}");
        return false;
    }

    private bool TryBuildMixedDirectPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var parentId = ResolveMixedDirectParentId(itemData);
        var templateFile = GetTemplateForParentId(parentId);

        patch = CreateMixedBasePatch(itemId, itemData, sourceFile, parentId, templateFile);
        if (patch.Count == 0)
        {
            itemInfo = new ItemInfo();
            Log($"Mixed_templates 无法创建基底补丁: {itemId} -> parent={parentId ?? "<null>"}, template={templateFile ?? "<null>"}");
            return false;
        }

        patch["ItemID"] = itemId;
        itemInfo = ExtractMixedDirectItemInfo(itemId, itemData, sourceFile, parentId, templateFile, patch);
        if (!string.IsNullOrWhiteSpace(itemInfo.Name))
        {
            patch["Name"] = itemInfo.Name;
        }

        return true;
    }

    private JsonObject CreateMixedBasePatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile)
    {
        if (!string.IsNullOrWhiteSpace(templateFile))
        {
            var templatePatch = SelectTemplateData(templateFile, itemId);
            if (templatePatch is not null)
            {
                return templatePatch;
            }
        }

        var bootstrapInfo = CreateMixedBootstrapItemInfo(itemId, itemData, sourceFile, parentId, templateFile);
        return CreateDefaultLegacyPatch(itemId, bootstrapInfo, templateFile);
    }

    private bool TryGetStoredPatchById(string itemId, out JsonObject patch)
    {
        if (weaponPatches.TryGetValue(itemId, out patch)
            || attachmentPatches.TryGetValue(itemId, out patch)
            || ammoPatches.TryGetValue(itemId, out patch)
            || gearPatches.TryGetValue(itemId, out patch)
            || consumablePatches.TryGetValue(itemId, out patch))
        {
            return true;
        }

        patch = new JsonObject();
        return false;
    }

    private bool TryResolveTemplateCloneByIdOrAlias(string cloneId, out string resolvedCloneId, out JsonObject cloneTemplate)
    {
        if (templateById.TryGetValue(cloneId, out cloneTemplate))
        {
            resolvedCloneId = cloneId;
            return true;
        }

        if (templateAliasCache.TryGetValue(cloneId, out resolvedCloneId)
            && templateById.TryGetValue(resolvedCloneId, out cloneTemplate))
        {
            return true;
        }

        if (unresolvedTemplateAliases.Contains(cloneId))
        {
            resolvedCloneId = string.Empty;
            cloneTemplate = new JsonObject();
            return false;
        }

        if (TryResolveTemplateAlias(cloneId, out resolvedCloneId)
            && templateById.TryGetValue(resolvedCloneId, out cloneTemplate))
        {
            templateAliasCache[cloneId] = resolvedCloneId;
            return true;
        }

        resolvedCloneId = string.Empty;
        cloneTemplate = new JsonObject();
        return false;
    }

    private bool TryResolveTemplateAlias(string cloneId, out string resolvedCloneId)
    {
        var aliasTokens = TokenizeCloneReference(cloneId);
        if (aliasTokens.Count == 0)
        {
            resolvedCloneId = string.Empty;
            return false;
        }

        var candidates = new HashSet<TemplateAliasCandidate>();
        foreach (var token in aliasTokens)
        {
            if (!templateAliasCandidatesByToken.TryGetValue(token, out var tokenCandidates))
            {
                continue;
            }

            foreach (var candidate in tokenCandidates)
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            unresolvedTemplateAliases.Add(cloneId);
            resolvedCloneId = string.Empty;
            return false;
        }

        string? bestId = null;
        var bestScore = int.MinValue;
        foreach (var candidate in candidates)
        {
            var score = ScoreCloneAliasMatch(aliasTokens, candidate.Tokens);
            if (score > bestScore)
            {
                bestScore = score;
                bestId = candidate.TemplateId;
            }
        }

        if (bestId is null || bestScore < 10)
        {
            unresolvedTemplateAliases.Add(cloneId);
            resolvedCloneId = string.Empty;
            return false;
        }

        resolvedCloneId = bestId;
        return true;
    }

    private static int ScoreCloneAliasMatch(HashSet<string> aliasTokens, HashSet<string> candidateTokens)
    {
        var overlapCount = aliasTokens.Count(token => candidateTokens.Contains(token));
        if (overlapCount == 0)
        {
            return int.MinValue;
        }

        var numericTokens = aliasTokens.Where(ContainsNumericToken).ToArray();
        if (numericTokens.Length > 0 && numericTokens.Any(token => !candidateTokens.Contains(token)))
        {
            return int.MinValue;
        }

        var score = overlapCount * 10 - Math.Abs(candidateTokens.Count - aliasTokens.Count);
        if (aliasTokens.All(candidateTokens.Contains))
        {
            score += 20;
        }

        return score;
    }

    private static HashSet<string> TokenizeCloneReference(string value)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AlphaNumericTokenRegex.Matches(value))
        {
            var normalized = NormalizeCloneReferenceToken(match.Value);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                tokens.Add(normalized);
            }
        }

        return tokens;
    }

    internal static HashSet<string> ExtractAlphaNumericTokens(string value)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AlphaNumericTokenRegex.Matches(value))
        {
            if (!string.IsNullOrWhiteSpace(match.Value))
            {
                tokens.Add(match.Value);
            }
        }

        return tokens;
    }

    private static string? NormalizeCloneReferenceToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var normalized = token.ToLowerInvariant();
        normalized = normalized switch
        {
            "assaultrifle" => "weapon",
            "sniperrifle" => "weapon",
            "pistol" => "weapon",
            "smg" => "weapon",
            "grenadelauncher" => "weapon",
            "magazine" => "mag",
            "receiver" => "reciever",
            _ => normalized,
        };

        if (normalized is "item" or "equipment" or "std" or "custom")
        {
            return null;
        }

        if (normalized.EndsWith("rnd", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("rds", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("round", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("rounds", StringComparison.OrdinalIgnoreCase))
        {
            var digits = new string(normalized.TakeWhile(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(digits))
            {
                return digits;
            }
        }

        return normalized;
    }

    internal bool TryBuildStandardTemplateClonePatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var templateId = itemData["TemplateID"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(templateId))
        {
            patch = new JsonObject();
            itemInfo = new ItemInfo();
            return false;
        }

        if (templateById.TryGetValue(templateId, out var cloneTemplate))
        {
            patch = (JsonObject)cloneTemplate.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractStandardTemplateCloneItemInfo(itemId, itemData, sourceFile, templateId, cloneTemplate);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        if (TryGetStoredPatchById(templateId, out var generatedClonePatch)
            && generatedItemInfoById.TryGetValue(templateId, out var generatedCloneInfo))
        {
            patch = (JsonObject)generatedClonePatch.DeepClone();
            patch["ItemID"] = itemId;

            itemInfo = ExtractStandardTemplateCloneItemInfo(itemId, itemData, sourceFile, generatedCloneInfo, patch);
            if (!string.IsNullOrWhiteSpace(itemInfo.Name))
            {
                patch["Name"] = itemInfo.Name;
            }

            return true;
        }

        patch = new JsonObject();
        itemInfo = new ItemInfo();
        return false;
    }

    private static bool ContainsNumericToken(string token)
    {
        return token.Any(char.IsDigit);
    }

    private sealed record TemplateAliasCandidate(string TemplateId, HashSet<string> Tokens);

    internal ItemInfo ExtractItemInfo(string itemId, JsonObject itemData, string? sourceFile)
        => ItemInfoFactory.CreateStandardTemplateItemInfo(this, itemId, itemData, sourceFile);

    private ItemInfo ExtractStandardTemplateCloneItemInfo(string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
        => ItemInfoFactory.CreateStandardTemplateCloneItemInfo(this, itemId, itemData, sourceFile, cloneInfo, clonePatch);

    private ItemInfo ExtractStandardTemplateCloneItemInfo(string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
        => ItemInfoFactory.CreateStandardTemplateCloneItemInfo(this, itemId, itemData, sourceFile, cloneId, cloneTemplate);

    private ItemInfo ExtractMoxoItemInfo(string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
        => ItemInfoFactory.CreateMoxoItemInfo(this, itemId, itemData, sourceFile, cloneInfo, clonePatch);

    private ItemInfo ExtractMoxoItemInfo(string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
        => ItemInfoFactory.CreateMoxoItemInfo(this, itemId, itemData, sourceFile, cloneId, cloneTemplate);

    private ItemInfo ExtractRaidOverhaulItemInfo(string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
        => ItemInfoFactory.CreateRaidOverhaulItemInfo(this, itemId, itemData, sourceFile, cloneInfo, clonePatch);

    private ItemInfo ExtractRaidOverhaulItemInfo(string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
        => ItemInfoFactory.CreateRaidOverhaulItemInfo(this, itemId, itemData, sourceFile, cloneId, cloneTemplate);

    private ItemInfo ExtractSupportedWttSubclassItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        ItemInfo cloneInfo,
        JsonObject clonePatch,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile)
        => ItemInfoFactory.CreateSupportedWttSubclassItemInfo(this, itemId, itemData, sourceFile, cloneInfo, clonePatch, resolveParentId, resolveTemplateFile);

    private ItemInfo ExtractSupportedWttSubclassItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string cloneId,
        JsonObject cloneTemplate,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile)
        => ItemInfoFactory.CreateSupportedWttSubclassItemInfo(this, itemId, itemData, sourceFile, cloneId, cloneTemplate, resolveParentId, resolveTemplateFile);

    private ItemInfo ExtractMixedDirectItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
        => ItemInfoFactory.CreateMixedDirectItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile, basePatch);

    private bool TryBuildRaidOverhaulFallbackPatch(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string cloneId,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var parentId = ResolveRaidOverhaulParentId(itemData);
        var templateFile = GetTemplateForParentId(parentId);
        var bootstrapInfo = CreateRaidOverhaulBootstrapItemInfo(itemId, itemData, sourceFile, parentId, templateFile, cloneId);

        if (bootstrapInfo.IsGear)
        {
            patch = CreateDefaultLegacyPatch(itemId, bootstrapInfo, templateFile);
        }
        else if (!string.IsNullOrWhiteSpace(templateFile))
        {
            patch = CreateMixedBasePatch(itemId, itemData, sourceFile, parentId, templateFile);
        }
        else
        {
            patch = CreateDefaultLegacyPatch(itemId, bootstrapInfo, templateFile);
        }

        if (patch.Count == 0)
        {
            itemInfo = new ItemInfo();
            Log($"RaidOverhaul_templates 无法创建基底补丁: {itemId} -> clone={cloneId}, parent={parentId ?? "<null>"}, template={templateFile ?? "<null>"}");
            return false;
        }

        patch["ItemID"] = itemId;
        itemInfo = ExtractRaidOverhaulFallbackItemInfo(itemId, itemData, sourceFile, parentId, templateFile, patch);
        if (!string.IsNullOrWhiteSpace(itemInfo.Name))
        {
            patch["Name"] = itemInfo.Name;
        }

        Log($"RaidOverhaul_templates 回退到 Handbook/模板推断基底: {itemId} -> {cloneId}");
        return true;
    }

    private bool TryBuildSupportedWttSubclassFallbackPatch(
        string subclassName,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string cloneId,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile,
        out JsonObject patch,
        out ItemInfo itemInfo)
    {
        var parentId = resolveParentId(itemData);
        var templateFile = resolveTemplateFile(sourceFile, itemData, parentId, null);
        var bootstrapInfo = CreateWTTBootstrapItemInfo(itemId, itemData, sourceFile, parentId, templateFile);

        if (bootstrapInfo.IsGear || bootstrapInfo.IsConsumable)
        {
            patch = CreateDefaultLegacyPatch(itemId, bootstrapInfo, templateFile);
        }
        else if (!string.IsNullOrWhiteSpace(templateFile))
        {
            patch = CreateMixedBasePatch(itemId, itemData, sourceFile, parentId, templateFile);
        }
        else
        {
            patch = CreateDefaultLegacyPatch(itemId, bootstrapInfo, templateFile);
        }

        if (patch.Count == 0)
        {
            itemInfo = new ItemInfo();
            Log($"{subclassName} 无法创建基底补丁: {itemId} -> clone={cloneId}, parent={parentId ?? "<null>"}, template={templateFile ?? "<null>"}");
            return false;
        }

        patch["ItemID"] = itemId;
        itemInfo = ExtractWttSubclassFallbackItemInfo(itemId, itemData, sourceFile, parentId, templateFile, patch);
        if (!string.IsNullOrWhiteSpace(itemInfo.Name))
        {
            patch["Name"] = itemInfo.Name;
        }

        Log($"{subclassName} 回退到 parentId/Handbook 模板推断基底: {itemId} -> {cloneId}");
        return true;
    }

    private ItemInfo CreateRaidOverhaulBootstrapItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        string cloneId)
        => ItemInfoFactory.CreateRaidOverhaulBootstrapItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile, cloneId);

    private ItemInfo CreateWTTBootstrapItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile)
        => ItemInfoFactory.CreateWttBootstrapItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile);

    private ItemInfo ExtractRaidOverhaulFallbackItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
        => ItemInfoFactory.CreateRaidOverhaulFallbackItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile, basePatch);

    private ItemInfo ExtractWttSubclassFallbackItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
        => ItemInfoFactory.CreateWttSubclassFallbackItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile, basePatch);

    private ItemInfo CreateMixedBootstrapItemInfo(
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile)
        => ItemInfoFactory.CreateMixedBootstrapItemInfo(this, itemId, itemData, sourceFile, parentId, templateFile);

    private JsonObject CreateDefaultLegacyPatch(string itemId, ItemInfo itemInfo, string? templateFile)
    {
        if (IsAmmo(itemInfo.ParentId))
        {
            return CreateDefaultAmmoPatch(itemId, itemInfo);
        }

        if (itemInfo.IsWeapon || IsWeapon(itemInfo.ParentId))
        {
            return CreateDefaultWeaponPatch(itemId, itemInfo);
        }

        if (itemInfo.IsGear || IsGear(itemInfo.ParentId))
        {
            return CreateDefaultGearPatch(itemId, itemInfo);
        }

        if (itemInfo.IsConsumable || IsConsumable(itemInfo.ParentId))
        {
            return CreateDefaultConsumablePatch(itemId, itemInfo);
        }

        return CreateDefaultModPatch(itemId, itemInfo, templateFile ?? string.Empty);
    }

    internal static JsonObject ExtractProperties(JsonObject source, HashSet<string> ignoredKeys)
    {
        var result = new JsonObject();
        foreach (var pair in source)
        {
            if (!ignoredKeys.Contains(pair.Key) && pair.Value is not null)
            {
                result[pair.Key] = pair.Value.DeepClone();
            }
        }

        return result;
    }

    internal static JsonObject ExtractEffectiveInputFields(JsonObject itemData, JsonObject? cloneTemplate)
    {
        JsonObject fields;
        if (itemData["items"] is JsonObject itemsObject && itemsObject["_props"] is JsonObject multiProps)
        {
            fields = (JsonObject)multiProps.DeepClone();
        }
        else if (itemData["item"] is JsonObject itemObject && itemObject["_props"] is JsonObject singleProps)
        {
            fields = (JsonObject)singleProps.DeepClone();
        }
        else if (TryGetRaidOverhaulOverrideProperties(itemData, out var overrideProperties))
        {
            fields = (JsonObject)overrideProperties.DeepClone();
        }
        else
        {
            fields = new JsonObject();
            foreach (var pair in itemData)
            {
                if (!ItemJsonSchema.RealismStandardTemplateIgnoredKeys.Contains(pair.Key) && pair.Value is not null)
                {
                    fields[pair.Key] = pair.Value.DeepClone();
                }
            }
        }

        if (cloneTemplate is null)
        {
            return fields;
        }

        var allowedFields = GetAllowedLegacySourceFields(cloneTemplate);
        var filteredFields = new JsonObject();
        foreach (var pair in fields)
        {
            if ((cloneTemplate.ContainsKey(pair.Key)
                    || allowedFields.Contains(pair.Key)
                    || ShouldPreserveLegacyFilteredField(pair.Key))
                && pair.Value is not null)
            {
                filteredFields[pair.Key] = pair.Value.DeepClone();
            }
        }

        return filteredFields;
    }

    private static bool TryGetRaidOverhaulOverrideProperties(JsonObject itemData, out JsonObject overrideProperties)
    {
        if (itemData["OverrideProperties"] is JsonObject uppercaseOverrideProperties)
        {
            overrideProperties = uppercaseOverrideProperties;
            return true;
        }

        if (itemData["overrideProperties"] is JsonObject lowercaseOverrideProperties)
        {
            overrideProperties = lowercaseOverrideProperties;
            return true;
        }

        overrideProperties = new JsonObject();
        return false;
    }

    private static HashSet<string> GetAllowedLegacySourceFields(JsonObject cloneTemplate)
    {
        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddAllowedLegacySourceFields(allowedFields, cloneTemplate);

        var itemType = cloneTemplate["$type"]?.GetValue<string?>() ?? string.Empty;
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultWeaponTemplate());
            return allowedFields;
        }

        if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultModTemplate());

            var modType = cloneTemplate["ModType"]?.GetValue<string?>();
            if (!string.IsNullOrWhiteSpace(modType)
                && StaticData.ModTypeSpecificAttributes.TryGetValue(modType, out var modTypeAttributes))
            {
                AddAllowedLegacySourceFields(allowedFields, modTypeAttributes);
            }

            return allowedFields;
        }

        if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultAmmoTemplate());
            return allowedFields;
        }

        if (itemType.Contains("RealismMod.Consumable", StringComparison.OrdinalIgnoreCase))
        {
            AddAllowedLegacySourceFields(allowedFields, StaticData.CreateDefaultConsumableTemplate());
        }

        return allowedFields;
    }

    private static void AddAllowedLegacySourceFields(HashSet<string> allowedFields, JsonObject fieldSource)
    {
        foreach (var pair in fieldSource)
        {
            allowedFields.Add(pair.Key);
        }
    }

    private static bool ShouldPreserveLegacyFilteredField(string fieldName)
    {
           return string.Equals(fieldName, "ConflictingItems", StringComparison.OrdinalIgnoreCase)
              || string.Equals(fieldName, "SingleFireRate", StringComparison.OrdinalIgnoreCase);
    }

    internal static string? ResolveEffectiveModType(JsonObject properties, JsonObject referenceData, string? templateFile)
    {
        var sourceModType = properties["ModType"]?.GetValue<string?>();
        if (!string.IsNullOrWhiteSpace(sourceModType))
        {
            return sourceModType;
        }

        var referenceModType = referenceData["ModType"]?.GetValue<string?>();
        if (!string.IsNullOrWhiteSpace(referenceModType))
        {
            return referenceModType;
        }

        var templateName = Path.GetFileName(templateFile ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(templateName)
            && StaticData.TemplateFileToModType.TryGetValue(templateName, out var templateModType)
            && !string.IsNullOrWhiteSpace(templateModType))
        {
            return templateModType;
        }

        if (properties["Cartridges"] is not null
            || referenceData["Cartridges"] is not null
            || ((properties["LoadUnloadModifier"] is not null || referenceData["LoadUnloadModifier"] is not null)
                && (properties["CheckTimeModifier"] is not null || referenceData["CheckTimeModifier"] is not null)))
        {
            return "magazine";
        }

        return null;
    }

    private string? ResolveWttArmoryTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveEpicTemplatesParentId(JsonObject itemData)
    {
        var handbookParent = ResolveWttHandbookParentId(itemData);
        if (!string.IsNullOrWhiteSpace(handbookParent))
        {
            return handbookParent;
        }

        return NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
    }

    private string? ResolveRequisitionsTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveConsortiumOfThingsTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveEcoAttachmentTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveArtemTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveWttStandaloneParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveSptBattlepassTemplatesParentId(JsonObject itemData)
    {
        var directParent = NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(directParent))
        {
            return directParent;
        }

        return ResolveWttHandbookParentId(itemData);
    }

    private string? ResolveWttHandbookParentId(JsonObject itemData)
    {
        var handbookParent = itemData["handbookParentId"]?.GetValue<string?>()
            ?? itemData["handbook"]?["ParentId"]?.GetValue<string?>()
            ?? itemData["HandbookParent"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(handbookParent))
        {
            return null;
        }

        handbookParent = StaticData.HandbookParentToId.GetValueOrDefault(handbookParent, handbookParent);
        return NormalizeParentId(handbookParent);
    }

    private string? ResolveWttArmoryTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveEpicTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveRequisitionsTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveConsortiumOfThingsTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveEcoAttachmentTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveArtemTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveWttStandaloneTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveSptBattlepassTemplatesTemplateFile(
        string sourceFile,
        JsonObject itemData,
        string? parentId,
        string? cloneTemplateFile = null)
    {
        return GetTemplateForParentId(parentId)
            ?? ResolveWttTemplateFileHint(sourceFile, itemData)
            ?? cloneTemplateFile;
    }

    private string? ResolveWttTemplateFileHint(string sourceFile, JsonObject itemData)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return ResolveWttTemplateFileHintFromFileName(fileName, itemData);
    }

    private string? ResolveWttTemplateFileHintFromFileName(string fileName, JsonObject itemData)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        if (ContainsAnyKeyword(fileName, ["Ammo", "Ammunition"]))
        {
            return "ammoTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Foregrips"]))
        {
            return "ForegripTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["IronSights", "Iron_Sights"]))
        {
            return "IronSightTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Magazines", "Mags"]))
        {
            return "MagazineTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Muzzles", "Suppressors", "Muzzle Devices", "Muzzle_Devices"]))
        {
            return "MuzzleDeviceTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["PistolGrips", "Pistol_Grips", "Pgrips"]))
        {
            return "PistolGripTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Scopes", "Optics", "Eotech", "Red_Dots", "Magnifiers"]))
        {
            return "ScopeTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Receivers", "Recievers"]))
        {
            return "ReceiverTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Stocks"]))
        {
            return "StockTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Barrels"]))
        {
            return "BarrelTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Handguards", "HGS", "Skull_HG"]))
        {
            return "HandguardTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Mounts", "Rails", "Rail"]))
        {
            return "MountTemplates.json";
        }

        if (ContainsAnyKeyword(fileName, ["Lasers"]))
        {
            return "FlashlightLaserTemplates.json";
        }

        if (fileName.Contains("Weapon", StringComparison.OrdinalIgnoreCase))
        {
            var resolvedParentId = ResolveWttArmoryTemplatesParentId(itemData);
            return GetTemplateForParentId(resolvedParentId);
        }

        return null;
    }

    private string? ResolveMixedDirectParentId(JsonObject itemData)
    {
        var itemNode = GetLegacyItemNode(itemData);
        var itemParentId = NormalizeParentId(itemNode?["_parent"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(itemParentId))
        {
            return itemParentId;
        }

        var handbookParent = itemData["handbook"]?["ParentId"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(handbookParent))
        {
            return null;
        }

        handbookParent = StaticData.HandbookParentToId.GetValueOrDefault(handbookParent, handbookParent);
        return NormalizeParentId(handbookParent);
    }

    private string? ResolveRaidOverhaulParentId(JsonObject itemData)
    {
        var handbookParent = itemData["HandbookParent"]?.GetValue<string?>()
            ?? itemData["Handbook"]?["HandbookParent"]?.GetValue<string?>();
        if (string.IsNullOrWhiteSpace(handbookParent))
        {
            return null;
        }

        handbookParent = StaticData.HandbookParentToId.GetValueOrDefault(handbookParent, handbookParent);
        return NormalizeParentId(handbookParent);
    }

    internal static JsonObject? GetLegacyItemNode(JsonObject itemData)
    {
        return itemData["item"] as JsonObject ?? itemData["items"] as JsonObject;
    }

    internal static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    internal static string? SelectBestDisplayName(string? localizedName, params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value) && !LooksLikeInternalItemName(value))
            {
                return value;
            }
        }

        if (!string.IsNullOrWhiteSpace(localizedName))
        {
            return localizedName;
        }

        return FirstNonEmpty(values);
    }

    private static bool LooksLikeInternalItemName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 6)
        {
            return false;
        }

        if (trimmed.StartsWith("mod_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("ammo_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("stock_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("pistolgrip_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("reciever_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("receiver_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("sight_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("mag_", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trimmed.Length == 24 && trimmed.All(Uri.IsHexDigit))
        {
            return true;
        }

        var hasSeparator = trimmed.Contains('_') || trimmed.Contains('/');
        var allAsciiTokenChars = trimmed.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-' or '.' or '"');
        var hasLowercase = trimmed.Any(char.IsLower);
        var hasWhitespace = trimmed.Any(char.IsWhiteSpace);

        return hasSeparator && allAsciiTokenChars && hasLowercase && !hasWhitespace;
    }

    internal void EnrichItemInfoWithSourceContext(ItemInfo info, JsonObject itemData)
    {
        if (string.IsNullOrWhiteSpace(info.TemplateFile) && !string.IsNullOrWhiteSpace(info.SourceFile))
        {
            info.TemplateFile = InferTemplateFileFromSourceFile(info.SourceFile!);
        }

        if (string.IsNullOrWhiteSpace(info.ParentId) && !string.IsNullOrWhiteSpace(info.TemplateFile))
        {
            info.ParentId = InferParentIdFromTemplateFile(info.TemplateFile!);
        }

        info.ItemType ??= itemData["$type"]?.GetValue<string?>();
        var itemType = info.ItemType ?? string.Empty;
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            info.IsWeapon = true;
        }
        else if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            info.IsGear = true;
        }
        else if (itemType.Contains("RealismMod.Consumable", StringComparison.OrdinalIgnoreCase))
        {
            info.IsConsumable = true;
        }

        if (!string.IsNullOrWhiteSpace(info.ParentId))
        {
            info.IsWeapon |= IsWeapon(info.ParentId);
            info.IsGear |= IsGear(info.ParentId);
            info.IsConsumable |= IsConsumable(info.ParentId);
        }

        var source = info.SourceFile?.Replace('\\', '/').ToLowerInvariant() ?? string.Empty;
        if (source.StartsWith("weapons/"))
        {
            info.IsWeapon = true;
        }
        else if (source.StartsWith("gear/"))
        {
            info.IsGear = true;
        }
        else if (source.StartsWith("consumables/"))
        {
            info.IsConsumable = true;
        }
    }

    private void FinalizePatch(string itemId, JsonObject patch, ItemInfo itemInfo, HashSet<string> processedItems, string sourceFile)
    {
        MergeInputProperties(patch, itemInfo);
        EnsureBasicFields(itemId, patch, itemInfo);
        var ruleStopwatch = Stopwatch.StartNew();
        ApplyRealismSanityCheck(patch, itemInfo);
        ruleStopwatch.Stop();
        totalRuleApplicationTicks += ruleStopwatch.ElapsedTicks;
        ApplyItemException(itemId, patch);
        PruneDisallowedOutputFields(patch, itemInfo);
        NormalizeStructuredOutput(patch, itemInfo);
        AddToFilePatches(itemId, patch, sourceFile);
        processedItems.Add(itemId);
    }

    private void ApplyItemException(string itemId, JsonObject patch)
    {
        if (!itemExceptions.TryGetEntry(itemId, out var exceptionEntry))
        {
            return;
        }

        foreach (var pair in exceptionEntry.Overrides)
        {
            patch[pair.Key] = pair.Value?.DeepClone();
        }

        if (exceptionEntry.Overrides.Count > 0)
        {
            Log($"应用例外物品覆盖: {itemId} ({exceptionEntry.Overrides.Count} 个字段)");
        }
    }

    private void EnsureBasicFields(string itemId, JsonObject patch, ItemInfo itemInfo)
    {
        patch["ItemID"] = itemId;
        if (patch["Name"] is null)
        {
            patch["Name"] = itemInfo.Name ?? InferFallbackName(itemInfo);
        }

        if (patch["$type"] is null)
        {
            if (itemInfo.IsWeapon)
            {
                patch["$type"] = "RealismMod.Gun, RealismMod";
            }
            else if (itemInfo.IsConsumable)
            {
                patch["$type"] = "RealismMod.Consumable, RealismMod";
            }
            else if (IsAmmo(itemInfo.ParentId))
            {
                patch["$type"] = "RealismMod.Ammo, RealismMod";
            }
            else if (itemInfo.IsGear)
            {
                patch["$type"] = "RealismMod.Gear, RealismMod";
            }
            else
            {
                patch["$type"] = "RealismMod.WeaponMod, RealismMod";
            }
        }

        if (ShouldEnsureConflictingItems(patch, itemInfo) && patch["ConflictingItems"] is null)
        {
            patch["ConflictingItems"] = new JsonArray();
        }
    }

    private static bool ShouldEnsureConflictingItems(JsonObject patch, ItemInfo itemInfo)
    {
        if (itemInfo.IsConsumable || itemInfo.IsGear)
        {
            return false;
        }

        if (itemInfo.IsWeapon)
        {
            return true;
        }

        var patchType = patch["$type"]?.GetValue<string?>();
        if (patchType is null)
        {
            return true;
        }

        return patchType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase)
            || patchType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase);
    }

    private string InferFallbackName(ItemInfo itemInfo)
    {
        if (IsAmmo(itemInfo.ParentId))
        {
            return $"ammo_{itemInfo.ItemId}";
        }

        if (itemInfo.IsWeapon)
        {
            return $"weapon_{itemInfo.ItemId}";
        }

        if (itemInfo.IsConsumable)
        {
            return $"consumable_{itemInfo.ItemId}";
        }

        return $"mod_{itemInfo.ItemId}";
    }

    private void MergeInputProperties(JsonObject patch, ItemInfo itemInfo)
    {
        if (!string.IsNullOrWhiteSpace(itemInfo.Name))
        {
            patch["Name"] = itemInfo.Name;
        }

        foreach (var pair in itemInfo.Properties)
        {
            if (pair.Value is not null && ShouldMergeSourceProperty(patch, itemInfo, pair.Key))
            {
                patch[pair.Key] = pair.Value.DeepClone();
            }
        }
    }

    private static void SyncItemInfoCategoryFromPatch(JsonObject patch, ItemInfo itemInfo)
    {
        var patchType = patch["$type"]?.GetValue<string?>() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(patchType))
        {
            return;
        }

        itemInfo.IsWeapon = patchType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase);
        itemInfo.IsGear = patchType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase);
        itemInfo.IsConsumable = patchType.Contains("RealismMod.Consumable", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldMergeSourceProperty(JsonObject patch, ItemInfo itemInfo, string fieldName)
    {
        if (string.Equals(fieldName, "Name", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(itemInfo.Name))
        {
            return false;
        }

        if (itemInfo.Format == ItemFormat.RealismStandardTemplate)
        {
            return true;
        }

        return true;
    }

    private void PruneDisallowedOutputFields(JsonObject patch, ItemInfo itemInfo)
    {
        var allowedFields = GetAllowedOutputFieldMap(itemInfo, patch);
        var keys = patch.Select(pair => pair.Key).ToArray();
        foreach (var key in keys)
        {
            if (!allowedFields.TryGetValue(key, out var canonicalKey))
            {
                patch.Remove(key);
                continue;
            }

            if (string.Equals(key, canonicalKey, StringComparison.Ordinal))
            {
                continue;
            }

            if (patch[canonicalKey] is null && patch[key] is not null)
            {
                patch[canonicalKey] = patch[key]!.DeepClone();
            }

            patch.Remove(key);
        }
    }

    private Dictionary<string, string> GetAllowedOutputFieldMap(ItemInfo itemInfo, JsonObject patch)
    {
        var allowedFields = CreateAllowedFieldMap();

        foreach (var fieldName in itemInfo.AllowedPatchFields)
        {
            TryAddCanonicalField(allowedFields, fieldName);
        }

        if (!string.IsNullOrWhiteSpace(itemInfo.TemplateFile)
            && templates.TryGetValue(Path.GetFileName(itemInfo.TemplateFile), out var templateData))
        {
            foreach (var template in templateData.Values)
            {
                foreach (var pair in template)
                {
                    TryAddCanonicalField(allowedFields, pair.Key);
                }
            }
        }

        var itemType = patch["$type"]?.GetValue<string?>() ?? itemInfo.ItemType ?? string.Empty;
        var modType = patch["ModType"]?.GetValue<string?>() ?? itemInfo.SourceProperties["ModType"]?.GetValue<string?>();
        AddRuleAllowedFields(allowedFields, itemType, modType);
        AddRequiredAllowedFields(allowedFields, itemType);
        return allowedFields;
    }

    internal HashSet<string> CreateAllowedPatchFieldSet(JsonObject templateSource, string? itemType, string? modType)
    {
        var allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in templateSource)
        {
            allowedFields.Add(pair.Key);
        }

        AddRuleAllowedFieldsToSet(allowedFields, itemType, modType);
        AddRequiredAllowedFieldsToSet(allowedFields, itemType ?? string.Empty);
        return allowedFields;
    }

    private void AddRuleAllowedFieldsToSet(HashSet<string> allowedFields, string? itemType, string? modType)
    {
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddRuleAllowedFields(fieldMap, itemType, modType);
        foreach (var fieldName in fieldMap.Values)
        {
            allowedFields.Add(fieldName);
        }
    }

    private void AddRuleAllowedFields(IDictionary<string, string> allowedFields, string? itemType, string? modType)
    {
        var resolvedItemType = itemType ?? string.Empty;
        if (resolvedItemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Weapon.GunClampRules.Keys);
            AddFieldNames(allowedFields, ["HasShoulderContact", "WeapType", "RecoilAngle", "CameraRecoil", "Price", "SingleFireRate"]);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponProfileRanges.Values);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponCaliberRuleModifiers.Values);
            AddRangeFieldNames(allowedFields, rules.Weapon.WeaponStockRuleModifiers.Values);
            return;
        }

        if (resolvedItemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Attachment.ModClampRules.Keys);
            AddFieldNames(allowedFields, ["CameraRecoil", "AimStability", "Flash", "HeatFactor", "CoolFactor", "AimSpeed", "Handling", "ReloadSpeed", "LoadUnloadModifier", "CheckTimeModifier", "ModMalfunctionChance", "DurabilityBurnModificator", "Price"]);
            AddRangeFieldNames(allowedFields, rules.Attachment.ModProfileRanges.Values);

            if (!string.IsNullOrWhiteSpace(modType)
                && StaticData.ModTypeSpecificAttributes.TryGetValue(modType, out var modTypeAttributes))
            {
                AddFieldNames(allowedFields, modTypeAttributes.Select(pair => pair.Key));
            }

            return;
        }

        if (resolvedItemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, rules.Gear.GearClampRules.Keys);
            AddFieldNames(allowedFields, ["SpallReduction", "ReloadSpeedMulti", "Comfort", "speedPenaltyPercent", "weaponErgonomicPenalty", "GasProtection", "RadProtection", "dB", "Price"]);
            AddRangeFieldNames(allowedFields, rules.Gear.GearProfileRanges.Values);
            return;
        }

        if (resolvedItemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["InitialSpeed", "BulletMassGram", "Damage", "PenetrationPower", "ammoRec", "ammoAccr", "ArmorDamage", "HeatFactor", "HeavyBleedingDelta", "LightBleedingDelta", "DurabilityBurnModificator", "BallisticCoeficient", "MalfMisfireChance", "MisfireChance", "MalfFeedChance"]);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoProfileRanges.Values);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoSpecialModifiers.Values);
            AddRangeFieldNames(allowedFields, rules.Ammo.AmmoPenetrationModifiers.Values);
        }
    }

    private static void AddRequiredAllowedFields(IDictionary<string, string> allowedFields, string itemType)
    {
        if (itemType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["Weight", "LoyaltyLevel", "Price"]);
            return;
        }

        if (itemType.Contains("RealismMod.WeaponMod", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["Weight", "LoyaltyLevel", "ModType", "Price"]);
            return;
        }

        if (itemType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["LoyaltyLevel", "BasePriceModifier"]);
            return;
        }

        if (itemType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            AddFieldNames(allowedFields, ["LoyaltyLevel", "Price"]);
        }
    }

    private static void AddRequiredAllowedFieldsToSet(HashSet<string> allowedFields, string itemType)
    {
        var fieldMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddRequiredAllowedFields(fieldMap, itemType);
        foreach (var fieldName in fieldMap.Values)
        {
            allowedFields.Add(fieldName);
        }
    }

    private static void AddRangeFieldNames(IDictionary<string, string> allowedFields, IEnumerable<IReadOnlyDictionary<string, NumericRange>> rangeMaps)
    {
        foreach (var rangeMap in rangeMaps)
        {
            AddFieldNames(allowedFields, rangeMap.Keys);
        }
    }

    private static void AddFieldNames(IDictionary<string, string> allowedFields, IEnumerable<string> fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            TryAddCanonicalField(allowedFields, fieldName);
        }
    }

    private static Dictionary<string, string> CreateAllowedFieldMap()
    {
        var allowedFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // 最终输出字段边界只允许 Realism 标准补丁字段；源 mod 输入字段可参与识别和推断，但不能直接泄漏到输出。
        TryAddCanonicalField(allowedFields, "$type");
        TryAddCanonicalField(allowedFields, "ItemID");
        TryAddCanonicalField(allowedFields, "Name");
        TryAddCanonicalField(allowedFields, "ConflictingItems");
        return allowedFields;
    }

    private static void TryAddCanonicalField(IDictionary<string, string> allowedFields, string fieldName)
    {
        if (!allowedFields.ContainsKey(fieldName))
        {
            allowedFields[fieldName] = fieldName;
        }
    }

    private void NormalizeStructuredOutput(JsonObject patch, ItemInfo itemInfo)
    {
        if (IsAmmo(itemInfo.ParentId)
            || (patch["$type"]?.GetValue<string?>()?.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            NormalizeAmmoOutputStructure(patch);
        }
    }

    private void NormalizeAmmoOutputStructure(JsonObject patch)
    {
        var normalized = StaticData.CreateDefaultAmmoTemplate();
        var fieldOrder = GetAmmoOutputFieldOrder();
        foreach (var field in fieldOrder)
        {
            if (patch[field] is not null)
            {
                normalized[field] = patch[field]!.DeepClone();
            }
        }

        patch.Clear();
        foreach (var field in fieldOrder)
        {
            patch[field] = normalized[field]?.DeepClone();
        }
    }

    private IReadOnlyList<string> GetAmmoOutputFieldOrder()
    {
        if (templates.TryGetValue("ammoTemplates.json", out var templateData) && templateData.Count > 0)
        {
            var firstTemplate = templateData.First().Value;
            var fields = firstTemplate.Select(pair => pair.Key).ToArray();
            if (fields.Length > 0)
            {
                return fields;
            }
        }

        return StaticData.AmmoOutputFieldOrder;
    }

    private void AddToFilePatches(string itemId, JsonObject patch, string sourceFile)
        => patchOutputBuffer.AddOrUpdate(sourceFile, itemId, patch);

    private void StorePatchByPatchType(string itemId, JsonObject patch)
    {
        var patchType = patch["$type"]?.GetValue<string?>() ?? string.Empty;
        if (patchType.Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase))
        {
            weaponPatches[itemId] = patch;
        }
        else if (patchType.Contains("RealismMod.Ammo", StringComparison.OrdinalIgnoreCase))
        {
            ammoPatches[itemId] = patch;
        }
        else if (patchType.Contains("RealismMod.Gear", StringComparison.OrdinalIgnoreCase))
        {
            gearPatches[itemId] = patch;
        }
        else if (patchType.Contains("RealismMod.Consumable", StringComparison.OrdinalIgnoreCase))
        {
            consumablePatches[itemId] = patch;
        }
        else
        {
            attachmentPatches[itemId] = patch;
        }
    }

    private void SavePatches(string outputPath)
        => PatchOutputPipeline.Save(outputPath, patchOutputBuffer.CreateOutputs(), Log);

    private JsonObject? SelectTemplateData(string templateFile, string itemId, bool allowFallback = true)
    {
        if (!templates.TryGetValue(Path.GetFileName(templateFile), out var templateData) || templateData.Count == 0)
        {
            return null;
        }

        if (templateData.TryGetValue(itemId, out var byItemId))
        {
            var exact = (JsonObject)byItemId.DeepClone();
            exact["ItemID"] = itemId;
            return exact;
        }

        if (!allowFallback)
        {
            return null;
        }

        var standard = templateData.FirstOrDefault(pair =>
            (pair.Value["Name"]?.GetValue<string?>() ?? string.Empty).Contains("std", StringComparison.OrdinalIgnoreCase)
            || (pair.Value["Name"]?.GetValue<string?>() ?? string.Empty).Contains("standard", StringComparison.OrdinalIgnoreCase));

        var fallbackSource = standard.Value ?? templateData.First().Value;
        var fallback = (JsonObject)fallbackSource.DeepClone();
        fallback["ItemID"] = itemId;
        return fallback;
    }

    private JsonObject CreateDefaultWeaponPatch(string itemId, ItemInfo itemInfo)
    {
        var patch = StaticData.CreateDefaultWeaponTemplate();
        patch["ItemID"] = itemId;
        patch["Name"] = itemInfo.Name ?? $"weapon_{itemId}";
        if (itemInfo.Properties["Weight"] is not null)
        {
            patch["Weight"] = itemInfo.Properties["Weight"]!.DeepClone();
        }

        if (itemInfo.Properties["Ergonomics"] is not null)
        {
            patch["Ergonomics"] = itemInfo.Properties["Ergonomics"]!.DeepClone();
        }

        if (itemInfo.Properties["bFirerate"] is not null)
        {
            patch["AutoROF"] = itemInfo.Properties["bFirerate"]!.DeepClone();
        }

        return patch;
    }

    private JsonObject CreateDefaultModPatch(string itemId, ItemInfo itemInfo, string templateFile)
    {
        var patch = StaticData.CreateDefaultModTemplate();
        patch["ItemID"] = itemId;
        patch["Name"] = itemInfo.Name ?? $"mod_{itemId}";

        var sourceModType = itemInfo.SourceProperties["ModType"]?.GetValue<string?>();
        var modType = !string.IsNullOrWhiteSpace(sourceModType)
            ? sourceModType!
            : StaticData.TemplateFileToModType.TryGetValue(Path.GetFileName(templateFile), out var templateModType)
            ? templateModType
            : string.Empty;
        patch["ModType"] = modType;

        if (!string.IsNullOrWhiteSpace(modType) && StaticData.ModTypeSpecificAttributes.TryGetValue(modType, out var attrs))
        {
            foreach (var pair in attrs)
            {
                patch[pair.Key] = pair.Value?.DeepClone();
            }
        }

        if (itemInfo.Properties["Weight"] is not null)
        {
            patch["Weight"] = itemInfo.Properties["Weight"]!.DeepClone();
        }

        if (itemInfo.Properties["Ergonomics"] is not null)
        {
            patch["Ergonomics"] = itemInfo.Properties["Ergonomics"]!.DeepClone();
        }

        return patch;
    }

    private JsonObject CreateDefaultAmmoPatch(string itemId, ItemInfo itemInfo)
    {
        var patch = StaticData.CreateDefaultAmmoTemplate();
        patch["ItemID"] = itemId;
        patch["Name"] = itemInfo.Name ?? $"ammo_{itemId}";

        foreach (var field in new[] { "Damage", "PenetrationPower", "InitialSpeed", "BulletMassGram", "BallisticCoeficient" })
        {
            if (itemInfo.Properties[field] is not null)
            {
                patch[field] = itemInfo.Properties[field]!.DeepClone();
            }
        }

        return patch;
    }

    private JsonObject CreateDefaultConsumablePatch(string itemId, ItemInfo itemInfo)
    {
        var patch = StaticData.CreateDefaultConsumableTemplate();
        patch["ItemID"] = itemId;
        patch["Name"] = itemInfo.Name ?? $"consumable_{itemId}";
        return patch;
    }

    private JsonObject CreateDefaultGearPatch(string itemId, ItemInfo itemInfo)
    {
        var patch = StaticData.CreateDefaultGearTemplate();
        patch["ItemID"] = itemId;
        patch["Name"] = itemInfo.Name ?? $"gear_{itemId}";

        foreach (var field in new[] { "Weight", "AllowADS", "ArmorClass", "CanSpall", "SpallReduction", "ReloadSpeedMulti", "Comfort", "speedPenaltyPercent", "mousePenalty", "weaponErgonomicPenalty", "TemplateType", "Price" })
        {
            if (itemInfo.Properties[field] is not null)
            {
                patch[field] = itemInfo.Properties[field]!.DeepClone();
            }
        }

        return patch;
    }

    internal static string? ExtractLocalizedName(JsonNode? localeNode)
    {
        if (localeNode is not JsonObject localeObject)
        {
            return null;
        }

        foreach (var lang in new[] { "en", "ch", "zh", "ru" })
        {
            if (localeObject[lang] is JsonObject localized)
            {
                var name = localized["name"]?.GetValue<string?>() ?? localized["Name"]?.GetValue<string?>();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }
        }

        var direct = localeObject["name"]?.GetValue<string?>() ?? localeObject["Name"]?.GetValue<string?>();
        return string.IsNullOrWhiteSpace(direct) ? null : direct;
    }

    private string? InferTemplateFileFromSourceFile(string sourceFile)
    {
        var normalized = sourceFile.Replace('\\', '/');
        var top = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant();
        if (top is not "weapons" and not "attatchments" and not "gear" and not "ammo" and not "consumables")
        {
            return null;
        }

        var fileName = Path.GetFileName(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : fileName + ".json";
    }

    internal string? InferParentIdFromTemplateFile(string templateFile)
    {
        if (!templateParentIndex.TryGetValue(Path.GetFileName(templateFile), out var parentIds) || parentIds.Count == 0)
        {
            return Path.GetFileName(templateFile) switch
            {
                "ammoTemplates.json" => "5485a8684bdc2da71d8b4567",
                _ => null,
            };
        }

        if (parentIds.Count == 1)
        {
            return parentIds[0];
        }

        return Path.GetFileName(templateFile) switch
        {
            "ScopeTemplates.json" => "55818ae44bdc2dde698b456c",
            "MuzzleDeviceTemplates.json" => "550aa4bf4bdc2dd6348b456b",
            "FlashlightLaserTemplates.json" => "55818b084bdc2d5b648b4571",
            "ReceiverTemplates.json" => "55818a304bdc2db5418b457d",
            "UBGLTempaltes.json" => "55818b014bdc2ddc698b456b",
            "ammoTemplates.json" => "5485a8684bdc2da71d8b4567",
            "armorPlateTemplates.json" => "644120aa86ffbe10ee032b6f",
            "meds.json" => "5448f3ac4bdc2dce718b4569",
            "food.json" => "5448e8d04bdc2ddf718b4569",
            _ => null,
        };
    }

    internal string? NormalizeParentId(string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return parentId;
        }

        return parentId.All(ch => char.IsUpper(ch) || ch == '_') || parentId.Contains('_')
            ? StaticData.ItemTypeNameToId.GetValueOrDefault(parentId, parentId)
            : parentId;
    }

    internal string? GetTemplateForParentId(string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return null;
        }

        var normalized = NormalizeParentId(parentId);
        if (normalized is null)
        {
            return null;
        }

        if (string.Equals(normalized, "5485a8684bdc2da71d8b4567", StringComparison.OrdinalIgnoreCase))
        {
            return "ammoTemplates.json";
        }

        return StaticData.ParentIdToTemplate.TryGetValue(normalized, out var templatePath)
            ? Path.GetFileName(templatePath)
            : null;
    }

    private bool IsWeapon(string? parentId)
    {
        var templateFile = GetTemplateForParentId(parentId);
        if (string.IsNullOrWhiteSpace(templateFile) || !templates.TryGetValue(templateFile, out var templateItems))
        {
            return false;
        }

        return templateItems.Values.Any(item => (item["$type"]?.GetValue<string?>() ?? string.Empty).Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAmmo(string? parentId)
    {
        return string.Equals(parentId, "5485a8684bdc2da71d8b4567", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsumable(string? parentId)
    {
        return parentId is "5448e8d04bdc2ddf718b4569" or "5448e8d64bdc2dce718b4568" or "5448f3ac4bdc2dce718b4569" or "5448f39d4bdc2d0a728b4568" or "5448f3a14bdc2d27728b4569" or "5448f3a64bdc2d60728b456a";
    }

    private static bool IsGear(string? parentId)
    {
        return parentId is "5448e54d4bdc2dcc718b4568" or "644120aa86ffbe10ee032b6f" or "5b5f704686f77447ec5d76d7" or "5448e53e4bdc2d60728b4567" or "5448e5284bdc2dcb718b4567" or "57bef4c42459772e8d35a53b" or "5a341c4086f77401f2541505" or "5a341c4686f77469e155819e" or "5645bcb74bdc2ded0b8b4578" or "5b3f15d486f77432d0509248";
    }

    private void ApplyRealismSanityCheck(JsonObject patch, ItemInfo itemInfo)
        => PatchRuleApplier.ApplyRealismSanityCheck(this, rules, patch, itemInfo);

    internal static double Normalize(double value, double min, double max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        if (Math.Abs(max - min) < double.Epsilon)
        {
            return 0.0;
        }

        return Clamp((value - min) / (max - min), 0.0, 1.0);
    }

    internal static void ApplyFieldClamps(JsonObject patch, IReadOnlyDictionary<string, NumericRange> clampRules)
    {
        foreach (var pair in clampRules)
        {
            if (patch[pair.Key] is null || !TryGetNumericValue(patch[pair.Key], out var value))
            {
                continue;
            }

            patch[pair.Key] = CreateNumericNode(Clamp(value, pair.Value.Min, pair.Value.Max), IsIntegerNode(patch[pair.Key]));
        }
    }

    internal static void ApplyGlobalSafetyClamps(JsonObject patch)
    {
        foreach (var pair in patch.ToList())
        {
            if (!TryGetNumericValue(pair.Value, out var value))
            {
                continue;
            }

            if (pair.Key.Contains("Recoil", StringComparison.OrdinalIgnoreCase))
            {
                patch[pair.Key] = CreateNumericNode(Clamp(value, -2000, 2000), IsIntegerNode(pair.Value));
            }
            else if (pair.Key.Contains("Ergonomics", StringComparison.OrdinalIgnoreCase))
            {
                patch[pair.Key] = CreateNumericNode(Clamp(value, -50, 100), IsIntegerNode(pair.Value));
            }
            else if (pair.Key.Contains("Weight", StringComparison.OrdinalIgnoreCase))
            {
                patch[pair.Key] = CreateNumericNode(Clamp(value, 0, 50), IsIntegerNode(pair.Value));
            }
            else if (pair.Key.Contains("Multi", StringComparison.OrdinalIgnoreCase) || pair.Key.Contains("Factor", StringComparison.OrdinalIgnoreCase))
            {
                patch[pair.Key] = CreateNumericNode(Clamp(value, 0.01, 10), IsIntegerNode(pair.Value));
            }
        }
    }

    internal void ApplyNumericRanges(JsonObject patch, IReadOnlyDictionary<string, NumericRange> ranges, bool ensureFields, bool preserveExistingValues = false)
    {
        foreach (var pair in ranges)
        {
            if (patch[pair.Key] is null)
            {
                if (!ensureFields)
                {
                    continue;
                }

                patch[pair.Key] = CreateNumericNode(GetRangeSeedValue(pair.Value.Min, pair.Value.Max, pair.Value.PreferInt), pair.Value.PreferInt);
            }

            patch[pair.Key] = preserveExistingValues
                ? ClampRangeValue(patch[pair.Key], pair.Value.Min, pair.Value.Max, pair.Value.PreferInt)
                : SampleRangeValue(patch[pair.Key], pair.Value.Min, pair.Value.Max, pair.Value.PreferInt);
        }
    }

    private string? InferWeaponProfile(JsonObject patch, ItemInfo itemInfo)
        => ProfileInferenceService.InferWeaponProfile(rules, PatchAnalysisContextFactory.Create(this, patch, itemInfo));

    private string ExtractGearArmorClassText(JsonObject patch, ItemInfo itemInfo)
        => PatchAnalysisContextFactory.Create(this, patch, itemInfo).GearArmorClassText;

    private string InferArmorPlateProfile(JsonObject patch, ItemInfo itemInfo)
    {
        var armorText = ExtractGearArmorClassText(patch, itemInfo);
        if (ContainsAnyKeyword(armorText, ["helmet_armor", "helmet armor", "helmet", "ears", "nape", "top", "jaw", "eyes"]))
        {
            return "armor_plate_helmet";
        }

        if (ContainsAnyKeyword(armorText, ["soft armor", "soft", "backer", "iiia", "gost 2", "gost 2a", "2a", "3a", "soft_armor", "软甲", "软插板"]))
        {
            return "armor_plate_soft";
        }

        return "armor_plate_hard";
    }

    private string InferBodyArmorProfile(string baseProfile, JsonObject patch, ItemInfo itemInfo)
    {
        var armorText = ExtractGearArmorClassText(patch, itemInfo);
        if (ContainsAnyKeyword(armorText, ["gost 4", "gost 5", "gost 5a", "gost 6", "nij iii+", "nij iv", "rf3", "xsapi", "esapi", "mk4a", "rev. g", "rev. j", "pm 5", "pm 8", "pm 10", "plates"]))
        {
            return $"{baseProfile}_heavy";
        }

        if (ContainsAnyKeyword(armorText, ["gost 2", "gost 2a", "gost 3", "gost 3a", "nij ii", "nij iia", "nij iii", "pm 2", "pm 3"]))
        {
            return $"{baseProfile}_light";
        }

        return $"{baseProfile}_heavy";
    }

    private string? InferCosmeticGearProfile(JsonObject patch, ItemInfo itemInfo)
    {
        var name = GetLowerText(patch["Name"]);
        if (ToOptionalBool(patch["IsGasMask"]) == true)
        {
            return "cosmetic_gasmask";
        }

        if (patch["GasProtection"] is not null || patch["RadProtection"] is not null)
        {
            return "cosmetic_gasmask";
        }

        if (itemInfo.Properties["GasProtection"] is not null || itemInfo.Properties["gasProtection"] is not null || itemInfo.Properties["RadProtection"] is not null || itemInfo.Properties["radProtection"] is not null)
        {
            return "cosmetic_gasmask";
        }

        if (ContainsAnyKeyword(name, ["gas mask", "respirator", "防毒", "防毒面具", "gasmask", "maska"]))
        {
            return "cosmetic_gasmask";
        }

        if (ContainsAnyKeyword(name, ["beret", "贝雷帽", "cap", "帽", "boonie", "watch cap"]))
        {
            return "cosmetic_headwear";
        }

        return null;
    }

    private string InferHelmetProfile(JsonObject patch)
    {
        var name = GetLowerText(patch["Name"]);
        if (ContainsAnyKeyword(name, ["altyn", "rys", "ronin", "maska", "vulkan", "tor", "zsh", "lshz", "kiver", "sphera", "devtac", "k1c", "shpm", "psh97", "ssh-68", "ssh68", "neosteel"]))
        {
            return "helmet_heavy";
        }

        return "helmet_light";
    }

    private string InferFaceProtectionProfile(string baseProfile, JsonObject patch, ItemInfo itemInfo)
    {
        var name = GetLowerText(patch["Name"]);
        if (baseProfile == "armor_component")
        {
            return ContainsAnyKeyword(name, ["shield", "face shield", "faceshield", "visor", "面甲", "面罩"])
                ? "armor_component_faceshield"
                : "armor_component_accessory";
        }

        var armorText = ExtractGearArmorClassText(patch, itemInfo);
        return ContainsAnyKeyword(armorText, ["nij", "gost", "v50", "anti-shatter", "ansi", "mil-prf", "bs en", "ballistic"])
            ? "armor_mask_ballistic"
            : "armor_mask_decorative";
    }

    private string InferBackpackProfile(JsonObject patch)
    {
        var name = GetLowerText(patch["Name"]);
        return ContainsAnyKeyword(name, ["sling", "daypack", "day pack", "drawbridge", "switchblade", "medpack", "medbag", "redfox", "wild", "takedown", "t20", "vertx"])
            ? "backpack_compact"
            : "backpack_full";
    }

    private string InferEyewearProfile(JsonObject patch, ItemInfo itemInfo)
    {
        var armorText = ExtractGearArmorClassText(patch, itemInfo);
        return ContainsAnyKeyword(armorText, ["v50", "anti-shatter", "ansi", "mil-prf", "ballistic", "z87", "31013"])
            ? "protective_eyewear_ballistic"
            : "protective_eyewear_standard";
    }

    private string InferChestRigProfile(JsonObject patch)
    {
        var name = GetLowerText(patch["Name"]);
        return ContainsAnyKeyword(name, ["bankrobber", "micro", "d3crx", "cs_assault", "thunderbolt", "bssmk1", "recon", "zulu"])
            ? "chest_rig_light"
            : "chest_rig_heavy";
    }

    private string? InferGearProfile(JsonObject patch, ItemInfo itemInfo)
        => ProfileInferenceService.InferGearProfile(PatchAnalysisContextFactory.Create(this, patch, itemInfo));

    private void ApplyAmmoProfileRanges(JsonObject patch, ItemInfo itemInfo)
        => AmmoRuleEngine.ApplyAmmoProfileRanges(this, rules, patch, itemInfo);

    private string? InferModProfile(JsonObject patch, ItemInfo itemInfo)
        => ProfileInferenceService.InferModProfile(rules, PatchAnalysisContextFactory.Create(this, patch, itemInfo), patch, itemInfo);

    private string? InferModProfileFromTemplateFile(string? templateFile, JsonObject patch, ItemInfo itemInfo)
    {
        if (string.IsNullOrWhiteSpace(templateFile))
        {
            return null;
        }

        var itemName = GetLowerText(patch["Name"]);
        return templateFile switch
        {
            "MagazineTemplates.json" => InferMagazineProfile(ExtractMagCapacity(itemInfo, itemName), itemName),
            "BarrelTemplates.json" => InferBarrelProfileFromName(itemName),
            "HandguardTemplates.json" => InferHandguardProfileFromName(itemName),
            "StockTemplates.json" => InferModStockProfile(itemName, patch, itemInfo),
            "ChargingHandleTemplates.json" => "charging_handle",
            "ScopeTemplates.json" => InferSightProfileFromName(itemName) ?? "scope_red_dot",
            "MuzzleDeviceTemplates.json" => "muzzle_flashhider",
            "ForegripTemplates.json" => "foregrip",
            "PistolGripTemplates.json" => "pistol_grip",
            "ReceiverTemplates.json" => "receiver",
            "GasblockTemplates.json" => "gasblock",
            "MountTemplates.json" => "mount",
            "FlashlightLaserTemplates.json" => "flashlight_laser",
            "IronSightTemplates.json" => "iron_sight",
            "UBGLTempaltes.json" => "ubgl",
            "UBGLTemplates.json" => "ubgl",
            _ => null,
        };
    }

    private string? InferModProfileFromNameFallback(string name, JsonObject patch, ItemInfo itemInfo)
    {
        if (name.StartsWith("catch_", StringComparison.OrdinalIgnoreCase))
        {
            return "catch";
        }

        if (name.StartsWith("hammer_", StringComparison.OrdinalIgnoreCase))
        {
            return "hammer";
        }

        if (name.StartsWith("trigger_", StringComparison.OrdinalIgnoreCase))
        {
            return "trigger";
        }

        if (name.StartsWith("charge_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["charging handle", "charging_handle", "拉机柄"]))
        {
            return "charging_handle";
        }

        if (name.StartsWith("bipod_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["bipod", "二脚架"]))
        {
            return "bipod";
        }

        if (ContainsAnyKeyword(name, ["rear_hook", "rear hook"]))
        {
            return "stock_rear_hook";
        }

        if (name.Contains("eyecup", StringComparison.OrdinalIgnoreCase))
        {
            return "optic_eyecup";
        }

        if (name.Contains("killflash", StringComparison.OrdinalIgnoreCase))
        {
            return "optic_killflash";
        }

        if (name.Contains("panel", StringComparison.OrdinalIgnoreCase))
        {
            return "rail_panel";
        }

        if (name.StartsWith("gas_block_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("gasblock_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["gas block", "导气箍"]))
        {
            return "gasblock";
        }

        if (name.StartsWith("foregrip_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["前握把", "垂直前握把", "斜握把", "握把挡块", "前握挡块", "hand stop", "grip stop", "handstop", "vertical grip", "angled grip", "foregrip", "sturmgriff"]))
        {
            return "foregrip";
        }

        if (name.StartsWith("pistolgrip_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["pistol grip", "小角度握把", "后握把", "пистолетная рукоятка"]))
        {
            return "pistol_grip";
        }

        if (name.Contains("握把", StringComparison.OrdinalIgnoreCase) && !ContainsAnyKeyword(name, ["前握把", "垂直", "斜握"]))
        {
            return "pistol_grip";
        }

        if (name.StartsWith("stock_adapter_", StringComparison.OrdinalIgnoreCase))
        {
            return "stock_adapter";
        }

        if (ContainsAnyKeyword(name, ["buttpad", "butt pad", "托腮", "枪托垫", "后托垫"]))
        {
            return "stock_buttpad";
        }

        if (name.StartsWith("buffer_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("buffertube_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["buffer tube", "缓冲管"]))
        {
            return "buffer_adapter";
        }

        if (name.StartsWith("stock_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["枪托", "buttstock", "brace", "底盘枪托", "приклад", "托"]))
        {
            return InferModStockProfile(name, patch, itemInfo);
        }

        if (name.StartsWith("receiver_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("reciever_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["机匣", "机匣盖", "防尘盖", "receiver", "reciever", "dust cover", "upper receiver", "upper reciever", "slide", "крышка ствольной коробки"]))
        {
            return "receiver";
        }

        if (name.StartsWith("mag_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("magazine_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["弹匣", "magazine", "drum", "casket", "магазин"]))
        {
            return InferMagazineProfile(ExtractMagCapacity(itemInfo, name), name);
        }

        if (IsHandguardLikeName(name))
        {
            return InferHandguardProfileFromName(name);
        }

        if (name.StartsWith("silencer_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["suppressor", "消音器", "抑制器", "消声器", "глушитель"]))
        {
            return InferSuppressorProfileFromName(name);
        }

        if (name.StartsWith("railq", StringComparison.OrdinalIgnoreCase))
        {
            return "handguard_medium";
        }

        if (ContainsAnyKeyword(name, ["barrel and rail system", "rail system", "front-end assembly", "front end assembly"]) && ContainsAnyKeyword(name, ["m-lok", "mlok", "keymod", "barrel", "forend", "handguard", "护木"]))
        {
            return InferHandguardProfileFromName(name);
        }

        if (name.StartsWith("mount_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["导轨", "基座", "偏移座", "镜座", "mount", "rail segment", "rail", "offset mount"]))
        {
            return "mount";
        }

        if (name.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["枪管", "barrel", "ствол"]))
        {
            return InferBarrelProfileFromName(name);
        }

        if (name.StartsWith("sight_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("scope_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["瞄具", "瞄准镜", "全息", "红点", "反射式"]))
        {
            return InferSightProfileFromName(name) ?? "scope_red_dot";
        }

        if (name.Contains("adapter", StringComparison.OrdinalIgnoreCase) && ContainsAnyKeyword(name, ["muzzle", "suppressor", "silencer", "taper", "qd", "消音器", "抑制器"]))
        {
            return "muzzle_adapter";
        }

        if (name.Contains("booster", StringComparison.OrdinalIgnoreCase))
        {
            return "booster";
        }

        if (ContainsAnyKeyword(name, ["thread protector", "螺纹保护", "protective cap"]))
        {
            return "muzzle_thread";
        }

        if (ContainsAnyKeyword(name, ["制退器", "compensator", "muzzle brake", "brake"]))
        {
            return "muzzle_brake";
        }

        if (name.StartsWith("muzzle_", StringComparison.OrdinalIgnoreCase) || name.Contains("flashhider", StringComparison.OrdinalIgnoreCase) || name.Contains("compensator", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["消焰器", "消焰", "火帽", "flash hider"]))
        {
            return "muzzle_flashhider";
        }

        if (ContainsAnyKeyword(name, ["flashlight", "laser", "peq", "dbal", "x400", "xc1", "战术灯", "战术装置", "手电", "手电筒", "激光", "镭射", "照明", "wmx", "wmlx", "x300", "m300", "m600", "m640", "wmx200"]) && !ContainsAnyKeyword(name, ["偏移座", "基座", "导轨", "mount", "rail"]))
        {
            return "flashlight_laser";
        }

        if (ContainsAnyKeyword(name, ["gas tube", "导气管"]))
        {
            return "gasblock";
        }

        if (ContainsAnyKeyword(name, ["front-end assembly", "front end assembly"]))
        {
            return InferHandguardProfileFromName(name);
        }

        return null;
    }

    internal static string InferMagazineProfile(int? capacity, string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["drum", "casket", "quad", "coupled", "twin", "beta", "helical", "snail"]))
        {
            return "magazine_drum";
        }

        if (ContainsAnyKeyword(itemName, ["extended", "extend", "加长", "扩容"]))
        {
            return "magazine_extended";
        }

        if (ContainsAnyKeyword(itemName, ["compact", "short", "stubby", "短弹匣", "短匣"]))
        {
            return "magazine_compact";
        }

        if (capacity is null)
        {
            return "magazine_standard";
        }

        if (capacity <= 20)
        {
            return "magazine_compact";
        }

        if (capacity <= 40)
        {
            return "magazine_standard";
        }

        if (capacity <= 60)
        {
            return "magazine_extended";
        }

        return "magazine_drum";
    }

    internal static int? ExtractMagCapacity(ItemInfo itemInfo, string itemName)
    {
        foreach (var key in new[] { "Capacity", "capacity", "MaxCount", "max_count", "CartridgeMaxCount", "cartridgeMaxCount" })
        {
            if (TryReadCapacityValue(itemInfo.Properties[key], out var capacity))
            {
                return capacity;
            }
        }

        if (itemInfo.Properties["Cartridges"] is JsonArray cartridges)
        {
            foreach (var slot in cartridges.OfType<JsonObject>())
            {
                if (TryReadCapacityValue(slot["_max_count"], out var capacity))
                {
                    return capacity;
                }
            }
        }

        foreach (var regex in new[] { MagCapacityNameRegex, MagCapacityCnRegex, MagCapacityRuRegex })
        {
            var match = regex.Match(itemName);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var parsed) && parsed is >= 1 and <= 200)
            {
                return parsed;
            }
        }

        return null;
    }

    private static bool TryReadCapacityValue(JsonNode? node, out int capacity)
    {
        capacity = 0;
        if (node is null)
        {
            return false;
        }

        if (TryGetNumericValue(node, out var numericValue))
        {
            var parsed = (int)Math.Round(numericValue);
            if (parsed is >= 1 and <= 200)
            {
                capacity = parsed;
                return true;
            }
        }

        var textValue = GetText(node);
        if (int.TryParse(textValue, out var parsedInt) && parsedInt is >= 1 and <= 200)
        {
            capacity = parsedInt;
            return true;
        }

        return false;
    }

    internal static string InferBarrelProfileFromName(string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["integral barrel-suppressor", "integral suppressor", "integrally suppressed", "barrel-suppressor", "一体消音枪管", "整体消音枪管"]))
        {
            return "barrel_integral_suppressed";
        }

        var barrelLength = ExtractBarrelLengthMm(itemName);
        if (ContainsAnyKeyword(itemName, ["shortened", "short", "sbr", "kurz"]))
        {
            return "barrel_short";
        }

        if (barrelLength is not null && barrelLength <= 330 && ContainsAnyKeyword(itemName, ["carbine", "smg", "pdw", "shotgun", "12ga", "762x51", "556x45", "545x39", "762x39"]))
        {
            return "barrel_short";
        }

        if (ContainsAnyKeyword(itemName, ["extended", "long", "rifle length", "full length"]))
        {
            return "barrel_long";
        }

        return "barrel_medium";
    }

    internal static double? ExtractBarrelLengthMm(string itemName)
    {
        var matches = BarrelLengthRegex.Matches(itemName);
        if (matches.Count == 0)
        {
            return null;
        }

        var match = matches[^1];
        if (!double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        var unit = match.Groups[2].Value.ToLowerInvariant();
        return unit is "inch" or "in" or "\"" ? value * 25.4 : value;
    }

    internal static bool IsHandguardLikeName(string itemName)
    {
        return itemName.StartsWith("handguard_", StringComparison.OrdinalIgnoreCase)
            || ContainsAnyKeyword(itemName, ["护木", "forend", "handguard", "front-end assembly", "front end assembly", "цевье"]);
    }

    internal static string InferHandguardProfileFromName(string itemName)
    {
        if (ContainsAnyKeyword(itemName, ["short", "carbine", "pdw", "compact"]))
        {
            return "handguard_short";
        }

        if (ContainsAnyKeyword(itemName, ["long", "extended", "rifle length", "full length"]))
        {
            return "handguard_long";
        }

        return "handguard_medium";
    }

    internal static string InferSuppressorProfileFromName(string itemName)
    {
        return ContainsAnyKeyword(itemName, ["mini", "mini2", "compact", "short", "45s", "rbs", "k-can", "mini monster"])
            ? "muzzle_suppressor_compact"
            : "muzzle_suppressor";
    }

    internal static string? InferSightProfileFromName(string itemName)
    {
        var normalized = itemName.Replace(',', '.');
        if (ContainsAnyKeyword(normalized, ["sight_front", "sight_rear", "front sight", "rear sight", "sight post", "front post", "iron", "mbus", "flip", "backup", "drum rear sight", "tritium rear sight", "tritium front sight"]))
        {
            return "iron_sight";
        }

        if (ContainsAnyKeyword(normalized, ["red dot", "reddot", "reflex", "holo", "holographic", "rds", "eotech", "xps", "exps", "aimpoint", "micro", "t1", "t2", "pk06", "okp", "kobra", "romeo", "holosun", "delta point", "deltapoint", "rmr", "srs", "uh-1", "1p87", "comp_m4", "comp m4", "compm4", "aimpooint", "boss_xe", "boss xe"]))
        {
            return "scope_red_dot";
        }

        if (Regex.IsMatch(normalized, @"(?:^|[^0-9])1(?:[.]0+)?x(?:[^0-9]|$)", RegexOptions.IgnoreCase))
        {
            return "scope_red_dot";
        }

        if (ContainsAnyKeyword(normalized, ["acog", "prism", "specter", "hamr", "valday", "lpvo", "vudu", "razor", "march", "bravo4", "ta01", "ta11", "ps320", "hensoldt"]))
        {
            return "scope_magnified";
        }

        if (Regex.IsMatch(normalized, @"(?:^|[^0-9])(2|3|4|5|6|7|8|9|10|11|12)(?:[.]\d+)?x(?:[^0-9]|$)", RegexOptions.IgnoreCase)
            || Regex.IsMatch(normalized, @"(?:^|[^0-9])1(?:[.]\d+)?[-/](2|3|4|5|6|7|8|9|10|11|12)(?:[.]\d+)?(?:x)?(?:[^0-9]|$)", RegexOptions.IgnoreCase))
        {
            return "scope_magnified";
        }

        return null;
    }

    internal static string InferModStockProfile(string itemName, JsonObject patch, ItemInfo itemInfo)
    {
        if (ContainsAnyKeyword(itemName, ["buttpad", "recoil pad", "butt pad", "shoulder pad", "托腮", "后托垫", "枪托垫", "缓冲垫"]))
        {
            return "stock_buttpad";
        }

        var stockAllowAds = ToOptionalBool(itemInfo.Properties["StockAllowADS"]) ?? ToOptionalBool(patch["StockAllowADS"]);
        var hasShoulder = ToOptionalBool(itemInfo.Properties["HasShoulderContact"]) ?? ToOptionalBool(patch["HasShoulderContact"]);
        if (stockAllowAds == true)
        {
            return "stock_ads_support";
        }

        if (hasShoulder == false || ContainsAnyKeyword(itemName, ["fold", "folding", "collapsed", "retracted", "telescop", "wire", "pdw", "skeleton", "折叠", "伸缩", "收缩", "骨架", "折叠托", "伸缩托", "枪托"]))
        {
            return "stock_folding";
        }

        return "stock_fixed";
    }

    private static bool ContainsAnyKeyword(string text, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private string? InferWeaponCaliberProfile(JsonObject patch, ItemInfo itemInfo)
        => ProfileInferenceService.InferWeaponCaliberProfile(rules, PatchAnalysisContextFactory.Create(this, patch, itemInfo));

    private string InferWeaponStockProfile(JsonObject patch)
        => ProfileInferenceService.InferWeaponStockProfile(PatchAnalysisContextFactory.Create(this, patch, new ItemInfo()));

    internal JsonNode? SampleRangeValue(JsonNode? originalNode, double min, double max, bool preferInt)
    {
        if (!TryGetNumericValue(originalNode, out var originalValue))
        {
            return originalNode?.DeepClone();
        }

        if (min > max)
        {
            (min, max) = (max, min);
        }

        if (Math.Abs(max - min) < double.Epsilon)
        {
            return CreateNumericNode(min, preferInt);
        }

        var clampedOriginal = Clamp(originalValue, min, max);
        var center = (min + max) / 2.0;
        var mode = clampedOriginal * 0.7 + center * 0.3;
        var sampled = SampleTriangular(min, max, mode);
        return CreateNumericNode(Clamp(sampled, min, max), preferInt, min, max);
    }

    internal static JsonNode? ClampRangeValue(JsonNode? originalNode, double min, double max, bool preferInt)
    {
        if (!TryGetNumericValue(originalNode, out var originalValue))
        {
            return originalNode?.DeepClone();
        }

        if (min > max)
        {
            (min, max) = (max, min);
        }

        if (Math.Abs(max - min) < double.Epsilon)
        {
            return CreateNumericNode(min, preferInt);
        }

        return CreateNumericNode(Clamp(originalValue, min, max), preferInt, min, max);
    }

    private double SampleTriangular(double min, double max, double mode)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        mode = Clamp(mode, min, max);
        if (Math.Abs(max - min) < double.Epsilon)
        {
            return min;
        }

        return random.Triangular(min, max, mode);
    }

    internal static double GetRangeSeedValue(double min, double max, bool preferInt)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        var seed = min <= 0 && max >= 0 ? 0.0 : (min + max) / 2.0;
        return preferInt ? Math.Round(seed) : seed;
    }

    private static uint CreateRuntimeSeed()
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        RandomNumberGenerator.Fill(bytes);
        return BitConverter.ToUInt32(bytes);
    }

    internal static JsonNode CreateNumericNode(double value, bool preferInt, params double[] precisionHints)
    {
        if (preferInt)
        {
            return JsonValue.Create((int)Math.Round(value));
        }

        var precision = InferFloatPrecision(precisionHints.Length == 0 ? [value] : precisionHints);
        return JsonValue.Create(Math.Round(value, precision));
    }

    private static int InferFloatPrecision(params double[] values)
    {
        var precision = 2;
        foreach (var value in values)
        {
            var normalized = value.ToString("0.######", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');
            var separatorIndex = normalized.IndexOf('.');
            if (separatorIndex < 0)
            {
                continue;
            }

            precision = Math.Max(precision, normalized.Length - separatorIndex - 1);
        }

        return Math.Clamp(precision, 2, 4);
    }

    internal static double Clamp(double value, double min, double max)
    {
        if (min > max)
        {
            (min, max) = (max, min);
        }

        return Math.Max(min, Math.Min(max, value));
    }

    internal static bool TryGetNumericValue(JsonNode? node, out double value)
    {
        value = 0;
        if (node is null)
        {
            return false;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out _))
            {
                return false;
            }

            if (jsonValue.TryGetValue<double>(out var doubleValue))
            {
                value = doubleValue;
                return true;
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                value = longValue;
                return true;
            }

            if (jsonValue.TryGetValue<int>(out var intValue))
            {
                value = intValue;
                return true;
            }

            if (jsonValue.TryGetValue<decimal>(out var decimalValue))
            {
                value = (double)decimalValue;
                return true;
            }

            if (jsonValue.TryGetValue<string>(out var stringValue)
                && double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        return false;
    }

    internal static bool IsIntegerNode(JsonNode? node)
    {
        return node is JsonValue jsonValue
            && (jsonValue.TryGetValue<int>(out _) || jsonValue.TryGetValue<long>(out _));
    }

    internal static bool? ToOptionalBool(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolValue))
            {
                return boolValue;
            }

            if (jsonValue.TryGetValue<string>(out var stringValue) && bool.TryParse(stringValue, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    internal static string GetLowerText(JsonNode? node)
    {
        return GetText(node)?.ToLowerInvariant() ?? string.Empty;
    }

    internal static string? GetText(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string>(out var stringValue))
            {
                return stringValue;
            }

            if (jsonValue.TryGetValue<int>(out var intValue))
            {
                return intValue.ToString(CultureInfo.InvariantCulture);
            }

            if (jsonValue.TryGetValue<long>(out var longValue))
            {
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (jsonValue.TryGetValue<double>(out var doubleValue))
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            if (jsonValue.TryGetValue<bool>(out var boolValue))
            {
                return boolValue ? "true" : "false";
            }
        }

        return node.ToJsonString();
    }

    private void Log(string message)
    {
        logs.Add(message);
    }
}