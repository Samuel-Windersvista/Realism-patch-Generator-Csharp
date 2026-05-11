using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed record TemplateAliasCandidate(string TemplateId, HashSet<string> Tokens);

internal sealed class TemplateMetadataCache
{
    private readonly Dictionary<string, string> templateAliasCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unresolvedTemplateAliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> templateAllowedFieldMapCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> isWeaponCache = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, List<TemplateAliasCandidate>> AliasCandidatesByToken { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void ClearMutableCaches()
    {
        templateAliasCache.Clear();
        unresolvedTemplateAliases.Clear();
        templateAllowedFieldMapCache.Clear();
        isWeaponCache.Clear();
    }

    public bool TryGetResolvedAlias(string cloneId, out string resolvedCloneId)
        => templateAliasCache.TryGetValue(cloneId, out resolvedCloneId!);

    public void StoreResolvedAlias(string cloneId, string resolvedCloneId)
        => templateAliasCache[cloneId] = resolvedCloneId;

    public bool IsUnresolvedAlias(string cloneId)
        => unresolvedTemplateAliases.Contains(cloneId);

    public void StoreUnresolvedAlias(string cloneId)
        => unresolvedTemplateAliases.Add(cloneId);

    public bool TryGetAllowedFieldMap(string templateFileName, out Dictionary<string, string> fieldMap)
        => templateAllowedFieldMapCache.TryGetValue(templateFileName, out fieldMap!);

    public void StoreAllowedFieldMap(string templateFileName, Dictionary<string, string> fieldMap)
        => templateAllowedFieldMapCache[templateFileName] = fieldMap;

    public bool TryGetIsWeapon(string normalizedParentId, out bool isWeapon)
        => isWeaponCache.TryGetValue(normalizedParentId, out isWeapon);

    public void StoreIsWeapon(string normalizedParentId, bool isWeapon)
        => isWeaponCache[normalizedParentId] = isWeapon;
}
