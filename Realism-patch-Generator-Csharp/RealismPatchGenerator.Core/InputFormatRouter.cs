using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class InputFormatRouter
{
    public static SupportedInputFileFormat DetectSupportedFileFormat(JsonObject itemsData, string sourceFile)
    {
        var hasEntries = false;
        var allStandard = true;
        var allWtt = true;
        var allMoxo = true;
        var allMixed = true;
        var allRaidOverhaul = true;

        foreach (var pair in itemsData)
        {
            if (pair.Value is not JsonObject itemData)
            {
                continue;
            }

            hasEntries = true;
            allStandard &= IsRealismStandardTemplateFormat(itemData);
            allWtt &= IsWTTTemplateFormat(itemData);
            allMoxo &= IsMoxoTemplateFormat(itemData);
            allMixed &= IsMixedTemplateFormat(itemData);
            allRaidOverhaul &= IsRaidOverhaulTemplateFormat(itemData);
        }

        if (!hasEntries)
        {
            return SupportedInputFileFormat.Unsupported;
        }

        if (allStandard)
        {
            return SupportedInputFileFormat.RealismStandardTemplate;
        }

        if (allWtt && IsWttArmoryTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.WttArmory_templates;
        }

        if (allWtt && IsEpicTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.Epic_templates;
        }

        if (allWtt && IsConsortiumOfThingsTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.ConsortiumOfThings_templates;
        }

        if (allWtt && IsRequisitionsTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.Requisitions_templates;
        }

        if (allWtt && IsEcoAttachmentTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.EcoAttachment_templates;
        }

        if (allWtt && IsArtemTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.Artem_templates;
        }

        if (allWtt && IsWttStandaloneTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.WttStandalone_templates;
        }

        if (allWtt && IsSptBattlepassTemplatesSourceFile(sourceFile))
        {
            return SupportedInputFileFormat.SptBattlepass_templates;
        }

        if (allMoxo)
        {
            return SupportedInputFileFormat.MoxoTemplate;
        }

        if (allMixed)
        {
            return SupportedInputFileFormat.MixedTemplate;
        }

        if (allRaidOverhaul)
        {
            return SupportedInputFileFormat.RaidOverhaulTemplate;
        }

        return SupportedInputFileFormat.Unsupported;
    }

    public static bool ShouldUseSuffixOutput(string sourceFile, SupportedInputFileFormat inputFormat)
    {
        if (inputFormat != SupportedInputFileFormat.RealismStandardTemplate)
        {
            return true;
        }

        var normalized = sourceFile.Replace('\\', '/');
        var separatorIndex = normalized.IndexOf('/');
        var topLevelDirectory = separatorIndex >= 0 ? normalized[..separatorIndex] : normalized;

        return !topLevelDirectory.Equals("attatchments", StringComparison.OrdinalIgnoreCase)
            && !topLevelDirectory.Equals("gear", StringComparison.OrdinalIgnoreCase)
            && !topLevelDirectory.Equals("weapons", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRealismStandardTemplateFormat(JsonObject itemData)
    {
        return itemData.ContainsKey("$type")
            && itemData.ContainsKey("ItemID")
            && !ItemJsonSchema.HasLegacyFormatMarkers(itemData);
    }

    private static bool IsMoxoTemplateFormat(JsonObject itemData)
    {
        return !itemData.ContainsKey("$type")
            && itemData.ContainsKey("clone")
            && (itemData["item"] is JsonObject || itemData["items"] is JsonObject);
    }

    private static bool IsWTTTemplateFormat(JsonObject itemData)
    {
        return !itemData.ContainsKey("$type")
            && itemData.ContainsKey("itemTplToClone")
            && !itemData.ContainsKey("ItemToClone")
            && !itemData.ContainsKey("clone");
    }

    private static bool IsWttArmoryTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("WTT - Armory_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEpicTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("EpicRangeTime-", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsortiumOfThingsTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("ConsortiumOfThings_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRequisitionsTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("Echoes.of.Tarkov.-.Requisitions_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEcoAttachmentTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("Eco-Attachment Emporium_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsArtemTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("Artem_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWttStandaloneTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("AK50", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("AKResonant", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains("50 BMG", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(".50BMG", StringComparison.OrdinalIgnoreCase)
            || fileName.Contains(".50bmg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSptBattlepassTemplatesSourceFile(string sourceFile)
    {
        var fileName = Path.GetFileName(sourceFile ?? string.Empty);
        return fileName.Contains("SPT Battlepass", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMixedTemplateFormat(JsonObject itemData)
    {
        return !itemData.ContainsKey("$type")
            && !itemData.ContainsKey("itemTplToClone")
            && !itemData.ContainsKey("ItemToClone")
            && (itemData["item"] is JsonObject || itemData["items"] is JsonObject);
    }

    private static bool IsRaidOverhaulTemplateFormat(JsonObject itemData)
    {
        return !itemData.ContainsKey("$type")
            && itemData.ContainsKey("ItemToClone")
            && !itemData.ContainsKey("clone");
    }
}