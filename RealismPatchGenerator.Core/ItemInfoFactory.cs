using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class ItemInfoFactory
{
    public static ItemInfo CreateStandardTemplateItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string? sourceFile)
    {
        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RealismStandardTemplate,
        };

        info.ItemType = itemData["$type"]?.GetValue<string?>();
        info.Name = itemData["Name"]?.GetValue<string?>() ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        info.ParentId = generator.NormalizeParentId(itemData["parentId"]?.GetValue<string?>());
        if (!string.IsNullOrWhiteSpace(info.ParentId))
        {
            info.TemplateFile = generator.GetTemplateForParentId(info.ParentId);
        }

        info.Properties = PatchTextInferenceHelpers.ExtractProperties(itemData, ItemJsonSchema.RealismStandardTemplateIgnoredKeys);

        generator.EnrichItemInfoWithSourceContext(info, itemData);
        info.SourceProperties = (JsonObject)info.Properties.DeepClone();
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(info.Properties, info.ItemType, info.SourceProperties["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateStandardTemplateCloneItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
    {
        var properties = PatchTextInferenceHelpers.ExtractProperties(itemData, ItemJsonSchema.RealismStandardTemplateIgnoredKeys);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RealismStandardTemplate,
            TemplateFile = cloneInfo.TemplateFile,
            ParentId = cloneInfo.ParentId,
            ItemType = cloneInfo.ItemType ?? clonePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), clonePatch["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
            IsWeapon = cloneInfo.IsWeapon,
            IsGear = cloneInfo.IsGear,
            IsConsumable = cloneInfo.IsConsumable,
        };

        generator.EnrichItemInfoWithSourceContext(info, clonePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(clonePatch, info.ItemType, clonePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateStandardTemplateCloneItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
    {
        var templateFile = generator.GetTemplateFileByItemId(cloneId);
        var properties = PatchTextInferenceHelpers.ExtractProperties(itemData, ItemJsonSchema.RealismStandardTemplateIgnoredKeys);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RealismStandardTemplate,
            TemplateFile = templateFile,
            ParentId = generator.InferParentIdFromTemplateFile(templateFile ?? string.Empty),
            ItemType = cloneTemplate["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), cloneTemplate["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, cloneTemplate);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(cloneTemplate, info.ItemType, cloneTemplate["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateMoxoItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
    {
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, clonePatch);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.MoxoTemplate,
            TemplateFile = cloneInfo.TemplateFile,
            ParentId = cloneInfo.ParentId,
            ItemType = cloneInfo.ItemType ?? clonePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), clonePatch["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
            IsWeapon = cloneInfo.IsWeapon,
            IsGear = cloneInfo.IsGear,
            IsConsumable = cloneInfo.IsConsumable,
        };

        generator.EnrichItemInfoWithSourceContext(info, clonePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(clonePatch, info.ItemType, clonePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateMoxoItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
    {
        var templateFile = generator.GetTemplateFileByItemId(cloneId);
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, cloneTemplate);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.MoxoTemplate,
            TemplateFile = templateFile,
            ParentId = generator.InferParentIdFromTemplateFile(templateFile ?? string.Empty),
            ItemType = cloneTemplate["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), cloneTemplate["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, cloneTemplate);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(cloneTemplate, info.ItemType, cloneTemplate["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateRaidOverhaulItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, ItemInfo cloneInfo, JsonObject clonePatch)
    {
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, clonePatch);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RaidOverhaulTemplate,
            TemplateFile = cloneInfo.TemplateFile,
            ParentId = cloneInfo.ParentId,
            ItemType = cloneInfo.ItemType ?? clonePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), clonePatch["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
            IsWeapon = cloneInfo.IsWeapon,
            IsGear = cloneInfo.IsGear,
            IsConsumable = cloneInfo.IsConsumable,
        };

        generator.EnrichItemInfoWithSourceContext(info, clonePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(clonePatch, info.ItemType, clonePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateRaidOverhaulItemInfo(RealismPatchGenerator generator, PatchFieldPermissionService fieldPermissionService, string itemId, JsonObject itemData, string sourceFile, string cloneId, JsonObject cloneTemplate)
    {
        var templateFile = generator.GetTemplateFileByItemId(cloneId);
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, cloneTemplate);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RaidOverhaulTemplate,
            TemplateFile = templateFile,
            ParentId = generator.InferParentIdFromTemplateFile(templateFile ?? string.Empty),
            ItemType = cloneTemplate["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), cloneTemplate["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, cloneTemplate);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(cloneTemplate, info.ItemType, cloneTemplate["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateSupportedWttSubclassItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        ItemInfo cloneInfo,
        JsonObject clonePatch,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile)
    {
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, clonePatch);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        var resolvedParentId = resolveParentId(itemData) ?? cloneInfo.ParentId;
        var templateFile = resolveTemplateFile(sourceFile, itemData, resolvedParentId, cloneInfo.TemplateFile);
        var effectiveModType = PatchTextInferenceHelpers.ResolveEffectiveModType(properties, clonePatch, templateFile);
        if (!string.IsNullOrWhiteSpace(effectiveModType))
        {
            properties["ModType"] = effectiveModType;
        }

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.WTTTemplate,
            TemplateFile = templateFile,
            ParentId = resolvedParentId,
            ItemType = cloneInfo.ItemType ?? clonePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), clonePatch["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
            IsWeapon = cloneInfo.IsWeapon,
            IsGear = cloneInfo.IsGear,
            IsConsumable = cloneInfo.IsConsumable,
        };

        generator.EnrichItemInfoWithSourceContext(info, clonePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(clonePatch, info.ItemType, effectiveModType);
        return info;
    }

    public static ItemInfo CreateSupportedWttSubclassItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string cloneId,
        JsonObject cloneTemplate,
        Func<JsonObject, string?> resolveParentId,
        Func<string, JsonObject, string?, string?, string?> resolveTemplateFile)
    {
        var resolvedParentId = resolveParentId(itemData);
        var templateFile = resolveTemplateFile(sourceFile, itemData, resolvedParentId, generator.GetTemplateFileByItemId(cloneId));
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, cloneTemplate);
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        var effectiveModType = PatchTextInferenceHelpers.ResolveEffectiveModType(properties, cloneTemplate, templateFile);
        if (!string.IsNullOrWhiteSpace(effectiveModType))
        {
            properties["ModType"] = effectiveModType;
        }

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.WTTTemplate,
            TemplateFile = templateFile,
            ParentId = resolvedParentId ?? generator.InferParentIdFromTemplateFile(templateFile ?? string.Empty),
            ItemType = cloneTemplate["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.FirstNonEmpty(localizedName, itemData["Name"]?.GetValue<string?>(), cloneTemplate["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, cloneTemplate);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(cloneTemplate, info.ItemType, effectiveModType);
        return info;
    }

    public static ItemInfo CreateMixedDirectItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
    {
        var itemNode = PatchTextInferenceHelpers.GetLegacyItemNode(itemData);
        var itemProps = itemNode?["_props"] as JsonObject;
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, basePatch);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.MixedTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            ItemType = basePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(
                localizedName,
                itemProps?["Name"]?.GetValue<string?>(),
                itemData["Name"]?.GetValue<string?>(),
                itemNode?["_name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, basePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(basePatch, info.ItemType, basePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateRaidOverhaulFallbackItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
    {
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, basePatch);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RaidOverhaulTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            ItemType = basePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(localizedName, itemData["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, basePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(basePatch, info.ItemType, basePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateWttSubclassFallbackItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        JsonObject basePatch)
    {
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);
        var properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, basePatch);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.WTTTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            ItemType = basePatch["$type"]?.GetValue<string?>(),
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(localizedName, itemData["Name"]?.GetValue<string?>()),
            Properties = properties,
            SourceProperties = (JsonObject)properties.DeepClone(),
        };

        generator.EnrichItemInfoWithSourceContext(info, basePatch);
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(basePatch, info.ItemType, basePatch["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateMixedBootstrapItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile)
    {
        var itemNode = PatchTextInferenceHelpers.GetLegacyItemNode(itemData);
        var itemProps = itemNode?["_props"] as JsonObject;
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.MixedTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(
                localizedName,
                itemProps?["Name"]?.GetValue<string?>(),
                itemData["Name"]?.GetValue<string?>(),
                itemNode?["_name"]?.GetValue<string?>()),
            Properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, null),
        };

        info.SourceProperties = (JsonObject)info.Properties.DeepClone();
        generator.EnrichItemInfoWithSourceContext(info, new JsonObject());
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(info.Properties, info.ItemType, info.SourceProperties["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateRaidOverhaulBootstrapItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile,
        string cloneId)
    {
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.RaidOverhaulTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(localizedName, itemData["Name"]?.GetValue<string?>()),
            Properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, null),
        };

        ApplyRaidOverhaulCategoryHints(info, cloneId);
        info.SourceProperties = (JsonObject)info.Properties.DeepClone();
        generator.EnrichItemInfoWithSourceContext(info, new JsonObject());
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(info.Properties, info.ItemType, info.SourceProperties["ModType"]?.GetValue<string?>());
        return info;
    }

    public static ItemInfo CreateWttBootstrapItemInfo(
        RealismPatchGenerator generator,
        PatchFieldPermissionService fieldPermissionService,
        string itemId,
        JsonObject itemData,
        string sourceFile,
        string? parentId,
        string? templateFile)
    {
        var localizedName = PatchTextInferenceHelpers.ExtractLocalizedName(itemData["locales"]) ?? PatchTextInferenceHelpers.ExtractLocalizedName(itemData["LocalePush"]);

        var info = new ItemInfo
        {
            ItemId = itemId,
            SourceFile = sourceFile,
            Format = ItemFormat.WTTTemplate,
            TemplateFile = templateFile,
            ParentId = parentId,
            Name = PatchTextInferenceHelpers.SelectBestDisplayName(localizedName, itemData["Name"]?.GetValue<string?>()),
            Properties = PatchTextInferenceHelpers.ExtractEffectiveInputFields(itemData, null),
        };

        info.SourceProperties = (JsonObject)info.Properties.DeepClone();
        generator.EnrichItemInfoWithSourceContext(info, new JsonObject());
        info.AllowedPatchFields = fieldPermissionService.CreateAllowedPatchFieldSet(info.Properties, info.ItemType, info.SourceProperties["ModType"]?.GetValue<string?>());
        return info;
    }

    private static void ApplyRaidOverhaulCategoryHints(ItemInfo info, string cloneId)
    {
        var normalizedCloneId = cloneId.ToUpperInvariant();
        if (normalizedCloneId.StartsWith("ASSAULTRIFLE_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("SNIPERRIFLE_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("PISTOL_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("SMG_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("GRENADELAUNCHER_", StringComparison.Ordinal))
        {
            info.IsWeapon = true;
            info.ItemType = "RealismMod.Gun, RealismMod";
            return;
        }

        if (normalizedCloneId.StartsWith("VEST_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("ARMOR_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("BACKPACK_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("SECURE_", StringComparison.Ordinal)
            || normalizedCloneId.StartsWith("FACECOVER_", StringComparison.Ordinal))
        {
            info.IsGear = true;
            info.ItemType = "RealismMod.Gear, RealismMod";
        }
    }
}
