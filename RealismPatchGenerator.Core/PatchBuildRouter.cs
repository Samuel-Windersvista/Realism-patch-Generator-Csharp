using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class PatchBuildRouter
{
    public static bool TryBuildPatchForSupportedFormat(
        RealismPatchGenerator generator,
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
                if (generator.TryBuildStandardTemplateClonePatch(itemId, itemData, sourceFile, out patch, out itemInfo))
                {
                    return true;
                }

                itemInfo = generator.ExtractItemInfo(itemId, itemData, sourceFile);
                patch = (JsonObject)itemData.DeepClone();
                patch["ItemID"] = itemId;
                if (patch["Name"] is null && !string.IsNullOrWhiteSpace(itemInfo.Name))
                {
                    patch["Name"] = itemInfo.Name;
                }

                return true;

            case SupportedInputFileFormat.WttArmory_templates:
                return generator.TryBuildWttArmoryTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Epic_templates:
                return generator.TryBuildEpicTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.ConsortiumOfThings_templates:
                return generator.TryBuildConsortiumOfThingsTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Requisitions_templates:
                return generator.TryBuildRequisitionsTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.EcoAttachment_templates:
                return generator.TryBuildEcoAttachmentTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.Artem_templates:
                return generator.TryBuildArtemTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.WttStandalone_templates:
                return generator.TryBuildWttStandaloneTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.SptBattlepass_templates:
                return generator.TryBuildSptBattlepassTemplatesPatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.MoxoTemplate:
                return generator.TryBuildMoxoTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.MixedTemplate:
                return generator.TryBuildMixedTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            case SupportedInputFileFormat.RaidOverhaulTemplate:
                return generator.TryBuildRaidOverhaulTemplatePatch(itemId, itemData, sourceFile, out patch, out itemInfo);

            default:
                patch = new JsonObject();
                itemInfo = new ItemInfo();
                return false;
        }
    }
}