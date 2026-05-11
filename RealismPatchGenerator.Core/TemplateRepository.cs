using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RealismPatchGenerator.Core;

internal sealed class TemplateRepository
{
    private static readonly Regex AlphaNumericTokenRegex = new("[a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Func<string?, string?> normalizeParentId;
    private readonly Dictionary<string, JsonObject> templateById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> templateFileByItemId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> templateParentIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SortedDictionary<string, JsonObject>> templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly TemplateMetadataCache templateMetadataCache = new();

    internal TemplateMetadataCache MetadataCache => templateMetadataCache;

    internal IReadOnlyDictionary<string, SortedDictionary<string, JsonObject>> Templates => templates;

    internal IReadOnlyDictionary<string, JsonObject> TemplateById => templateById;

    internal IReadOnlyDictionary<string, string> TemplateFileByItemId => templateFileByItemId;

    public TemplateRepository(Func<string?, string?> normalizeParentId)
    {
        this.normalizeParentId = normalizeParentId;
    }

    public bool IsLoaded => templates.Count > 0;

    public void Load(string templatesBasePath, Action<string> log)
    {
        var snapshot = TemplateCatalog.Load(templatesBasePath, log);
        ReplaceSnapshot(snapshot);
    }

    public void Reload()
    {
        templateMetadataCache.ClearMutableCaches();
        templateMetadataCache.AliasCandidatesByToken.Clear();
        BuildTemplateAliasIndex();
    }

    private void ReplaceSnapshot(TemplateCatalogSnapshot snapshot)
    {
        templates.Clear();
        templateById.Clear();
        templateFileByItemId.Clear();
        templateParentIndex.Clear();
        templateMetadataCache.ClearMutableCaches();
        templateMetadataCache.AliasCandidatesByToken.Clear();

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

    public string? GetTemplateFileByItemId(string itemId)
        => templateFileByItemId.GetValueOrDefault(itemId);

    public bool TryResolveTemplateCloneByIdOrAlias(string cloneId, out string resolvedCloneId, out JsonObject cloneTemplate)
    {
        if (templateById.TryGetValue(cloneId, out var directCloneTemplate))
        {
            cloneTemplate = directCloneTemplate;
            resolvedCloneId = cloneId;
            return true;
        }

        if (templateMetadataCache.TryGetResolvedAlias(cloneId, out resolvedCloneId)
            && templateById.TryGetValue(resolvedCloneId, out var cachedCloneTemplate))
        {
            cloneTemplate = cachedCloneTemplate;
            return true;
        }

        if (templateMetadataCache.IsUnresolvedAlias(cloneId))
        {
            resolvedCloneId = string.Empty;
            cloneTemplate = new JsonObject();
            return false;
        }

        if (TryResolveTemplateAlias(cloneId, out resolvedCloneId)
            && templateById.TryGetValue(resolvedCloneId, out var resolvedCloneTemplate))
        {
            cloneTemplate = resolvedCloneTemplate;
            templateMetadataCache.StoreResolvedAlias(cloneId, resolvedCloneId);
            return true;
        }

        resolvedCloneId = string.Empty;
        cloneTemplate = new JsonObject();
        return false;
    }

    public JsonObject? SelectTemplateData(string templateFile, string itemId, bool allowFallback = true)
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

    public string? InferParentIdFromTemplateFile(string templateFile)
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

    public string? GetTemplateForParentId(string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return null;
        }

        var normalized = normalizeParentId(parentId);
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

    public bool IsWeapon(string? parentId)
    {
        if (string.IsNullOrWhiteSpace(parentId))
        {
            return false;
        }

        var normalizedParentId = normalizeParentId(parentId) ?? parentId;
        if (templateMetadataCache.TryGetIsWeapon(normalizedParentId, out var cached))
        {
            return cached;
        }

        var templateFile = GetTemplateForParentId(normalizedParentId);
        if (string.IsNullOrWhiteSpace(templateFile) || !templates.TryGetValue(templateFile, out var templateItems))
        {
            templateMetadataCache.StoreIsWeapon(normalizedParentId, false);
            return false;
        }

        var result = templateItems.Values.Any(item => (item["$type"]?.GetValue<string?>() ?? string.Empty).Contains("RealismMod.Gun", StringComparison.OrdinalIgnoreCase));
        templateMetadataCache.StoreIsWeapon(normalizedParentId, result);
        return result;
    }

    public bool TryGetTemplateData(string templateFile, out SortedDictionary<string, JsonObject> templateData)
        => templates.TryGetValue(Path.GetFileName(templateFile), out templateData!);

    public bool TryGetAllowedFieldMap(string templateFileName, out Dictionary<string, string> fieldMap)
        => templateMetadataCache.TryGetAllowedFieldMap(templateFileName, out fieldMap);

    public void StoreAllowedFieldMap(string templateFileName, Dictionary<string, string> fieldMap)
        => templateMetadataCache.StoreAllowedFieldMap(templateFileName, fieldMap);

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
                if (!templateMetadataCache.AliasCandidatesByToken.TryGetValue(token, out var candidates))
                {
                    candidates = [];
                    templateMetadataCache.AliasCandidatesByToken[token] = candidates;
                }

                candidates.Add(candidate);
            }
        }
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
            if (!templateMetadataCache.AliasCandidatesByToken.TryGetValue(token, out var tokenCandidates))
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
            templateMetadataCache.StoreUnresolvedAlias(cloneId);
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

        if (bestId is null || bestScore < 15)
        {
            templateMetadataCache.StoreUnresolvedAlias(cloneId);
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

    private static bool ContainsNumericToken(string token)
    {
        return token.Any(char.IsDigit);
    }
}
