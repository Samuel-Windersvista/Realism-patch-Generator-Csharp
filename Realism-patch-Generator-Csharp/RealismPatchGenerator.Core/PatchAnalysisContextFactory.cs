using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed class PatchAnalysisContext
{
    public string NormalizedParentId { get; init; } = string.Empty;
    public string TemplateFileName { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string WeapType { get; init; } = string.Empty;
    public string ModType { get; init; } = string.Empty;
    public string ArmorClass { get; init; } = string.Empty;
    public string GearArmorClassText { get; init; } = string.Empty;
    public string AmmoCaliberText { get; init; } = string.Empty;
    public string AmmoVariantText { get; init; } = string.Empty;
    public string WeaponCaliberText { get; init; } = string.Empty;
    public bool? HasShoulderContact { get; init; }
    public bool? IsGasMask { get; init; }
    public bool HasGasOrRadProtection { get; init; }
    public HashSet<string> NameTokens { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AmmoVariantTokens { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

internal static class PatchAnalysisContextFactory
{
    public static PatchAnalysisContext Create(RealismPatchGenerator generator, JsonObject patch, ItemInfo itemInfo)
    {
        var name = RealismPatchGenerator.GetLowerText(patch["Name"]);
        var weaponType = RealismPatchGenerator.GetLowerText(patch["WeapType"]);
        var modType = RealismPatchGenerator.GetLowerText(patch["ModType"]);
        var armorClass = RealismPatchGenerator.GetLowerText(patch["ArmorClass"]).Trim();

        return new PatchAnalysisContext
        {
            NormalizedParentId = generator.NormalizeParentId(itemInfo.ParentId) ?? string.Empty,
            TemplateFileName = Path.GetFileName(itemInfo.TemplateFile ?? string.Empty),
            SourceFileName = Path.GetFileName(itemInfo.SourceFile ?? string.Empty),
            Name = name,
            WeapType = weaponType,
            ModType = modType,
            ArmorClass = armorClass,
            GearArmorClassText = BuildCombinedLowerText(
                patch["ArmorClass"],
                patch["Name"],
                itemInfo.Properties["ArmorClass"],
                itemInfo.Properties["armorClass"],
                itemInfo.Properties["Name"],
                itemInfo.Properties["name"]),
            AmmoCaliberText = BuildCombinedLowerText(
                patch["Caliber"],
                patch["AmmoCaliber"],
                patch["caliber"],
                patch["ammoCaliber"],
                patch["Name"],
                itemInfo.Properties["Caliber"],
                itemInfo.Properties["ammoCaliber"],
                itemInfo.Properties["AmmoCaliber"]),
            AmmoVariantText = BuildCombinedLowerText(
                patch["Name"],
                patch["ShortName"],
                patch["Description"],
                patch["AmmoTooltipClass"],
                itemInfo.Properties["Name"],
                itemInfo.Properties["ShortName"],
                itemInfo.Properties["Description"],
                itemInfo.Properties["AmmoTooltipClass"],
                itemInfo.Properties["Caliber"]),
            WeaponCaliberText = BuildCombinedLowerText(
                patch["Caliber"],
                patch["AmmoCaliber"],
                patch["ShortName"],
                patch["Name"],
                itemInfo.Properties["Caliber"],
                itemInfo.Properties["ammoCaliber"],
                itemInfo.Properties["AmmoCaliber"],
                itemInfo.Properties["ShortName"],
                itemInfo.Properties["Name"]),
            HasShoulderContact = RealismPatchGenerator.ToOptionalBool(patch["HasShoulderContact"]),
            IsGasMask = RealismPatchGenerator.ToOptionalBool(patch["IsGasMask"]),
            HasGasOrRadProtection = patch["GasProtection"] is not null
                || patch["RadProtection"] is not null
                || itemInfo.Properties["GasProtection"] is not null
                || itemInfo.Properties["gasProtection"] is not null
                || itemInfo.Properties["RadProtection"] is not null
                || itemInfo.Properties["radProtection"] is not null,
            NameTokens = RealismPatchGenerator.ExtractAlphaNumericTokens(name),
            AmmoVariantTokens = RealismPatchGenerator.ExtractAlphaNumericTokens(BuildCombinedLowerText(
                patch["Name"],
                patch["ShortName"],
                patch["Description"],
                patch["AmmoTooltipClass"],
                itemInfo.Properties["Name"],
                itemInfo.Properties["ShortName"],
                itemInfo.Properties["Description"],
                itemInfo.Properties["AmmoTooltipClass"],
                itemInfo.Properties["Caliber"])),
        };
    }

    private static string BuildCombinedLowerText(params JsonNode?[] nodes)
    {
        return string.Join(' ', nodes
            .Select(RealismPatchGenerator.GetText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.ToLowerInvariant()));
    }
}