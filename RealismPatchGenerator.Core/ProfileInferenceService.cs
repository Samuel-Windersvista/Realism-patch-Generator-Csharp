using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class ProfileInferenceService
{
    public static string? InferWeaponProfile(RuleSet rules, PatchAnalysisContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.NormalizedParentId))
        {
            foreach (var pair in rules.Weapon.WeaponParentGroups)
            {
                if (pair.Value.Contains(context.NormalizedParentId))
                {
                    return pair.Key;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(context.TemplateFileName)
            && rules.Weapon.TemplateFileToWeaponProfile.TryGetValue(context.TemplateFileName, out var templateProfile))
        {
            return templateProfile;
        }

        if (!string.IsNullOrWhiteSpace(context.SourceFileName)
            && rules.Weapon.TemplateFileToWeaponProfile.TryGetValue(context.SourceFileName, out var sourceFileProfile))
        {
            return sourceFileProfile;
        }

        if (context.Name.Contains("pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("handgun", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("pistol", StringComparison.OrdinalIgnoreCase))
        {
            return "pistol";
        }

        if (context.Name.Contains("smg", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("smg", StringComparison.OrdinalIgnoreCase))
        {
            return "smg";
        }

        if (context.Name.Contains("launcher", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("grenade launcher", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("m203", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("gp25", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("ubgl", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("launcher", StringComparison.OrdinalIgnoreCase))
        {
            return "launcher";
        }

        if (context.Name.Contains("sniper", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("marksman", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("dmr", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("anti-materiel", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("anti materiel", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("狙击", StringComparison.OrdinalIgnoreCase))
        {
            return "sniper";
        }

        if (context.NameTokens.Contains("lmg")
            || context.NameTokens.Contains("mg")
            || context.Name.Contains("machinegun", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("machinegun", StringComparison.OrdinalIgnoreCase))
        {
            return "machinegun";
        }

        if (context.Name.Contains("shotgun", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("shotgun", StringComparison.OrdinalIgnoreCase))
        {
            return "shotgun";
        }

        if (context.Name.Contains("carbine", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("assault", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("rifle", StringComparison.OrdinalIgnoreCase))
        {
            return "assault";
        }

        return null;
    }

    public static string? InferWeaponCaliberProfile(RuleSet rules, PatchAnalysisContext context)
    {
        foreach (var pair in rules.Weapon.CaliberProfileKeywords)
        {
            if (pair.Keywords.Any(keyword => context.WeaponCaliberText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return pair.Profile;
            }
        }

        if (context.WeapType.Contains("pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("handgun", StringComparison.OrdinalIgnoreCase))
        {
            return "pistol_caliber";
        }

        return null;
    }

    public static string InferWeaponStockProfile(PatchAnalysisContext context)
    {
        if (context.Name.Contains("bullpup", StringComparison.OrdinalIgnoreCase)
            || context.WeapType.Contains("bullpup", StringComparison.OrdinalIgnoreCase))
        {
            return "bullpup";
        }

        if (context.WeapType.Contains("pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("machine pistol", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("stockless", StringComparison.OrdinalIgnoreCase))
        {
            return "stockless";
        }

        if (context.Name.Contains("folded", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("stock folded", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("no stock", StringComparison.OrdinalIgnoreCase))
        {
            return "folding_stock_collapsed";
        }

        if (context.Name.Contains("fold", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("folding", StringComparison.OrdinalIgnoreCase))
        {
            return context.HasShoulderContact != false ? "folding_stock_extended" : "folding_stock_collapsed";
        }

        if (context.HasShoulderContact == false)
        {
            return "stockless";
        }

        return "fixed_stock";
    }

    public static string InferAmmoProfile(RuleSet rules, PatchAnalysisContext context)
    {
        foreach (var keywordProfile in rules.Ammo.AmmoProfileKeywords)
        {
            if (keywordProfile.Keywords.Any(keyword => context.AmmoCaliberText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return keywordProfile.Profile;
            }
        }

        return "intermediate_rifle";
    }

    public static string? InferAmmoSpecialProfile(RuleSet rules, PatchAnalysisContext context)
    {
        foreach (var keywordProfile in rules.Ammo.AmmoSpecialKeywords)
        {
            foreach (var keyword in keywordProfile.Keywords)
            {
                var normalized = keyword.Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");
                if (string.IsNullOrWhiteSpace(normalized))
                {
                    continue;
                }

                if (normalized.Contains(' '))
                {
                    if (context.AmmoVariantText.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        return keywordProfile.Profile;
                    }

                    var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0 && parts.All(context.AmmoVariantTokens.Contains))
                    {
                        return keywordProfile.Profile;
                    }

                    continue;
                }

                if (context.AmmoVariantTokens.Contains(normalized))
                {
                    return keywordProfile.Profile;
                }
            }
        }

        return null;
    }

    public static string? InferGearProfile(PatchAnalysisContext context)
    {
        var hasArmorClass = !string.IsNullOrWhiteSpace(context.ArmorClass)
            && context.ArmorClass is not "unclassified" and not "none" and not "null";

        if (context.NormalizedParentId is "644120aa86ffbe10ee032b6f" or "5b5f704686f77447ec5d76d7")
        {
            return InferArmorPlateProfile(context);
        }

        var parentProfileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["5448e54d4bdc2dcc718b4568"] = "armor_vest",
            ["57bef4c42459772e8d35a53b"] = "armor_chest_rig",
            ["5448e5284bdc2dcb718b4567"] = "chest_rig",
            ["5a341c4086f77401f2541505"] = "helmet",
            ["5a341c4686f77469e155819e"] = "armor_mask",
            ["55d7217a4bdc2d86028b456d"] = "armor_component",
            ["5448e53e4bdc2d60728b4567"] = "backpack",
            ["5645bcb74bdc2ded0b8b4578"] = "headset",
            ["5b3f15d486f77432d0509248"] = "cosmetic_gasmask",
        };

        if (!string.IsNullOrWhiteSpace(context.NormalizedParentId)
            && parentProfileMap.TryGetValue(context.NormalizedParentId, out var profile))
        {
            return profile switch
            {
                "helmet" => InferHelmetProfile(context),
                "armor_vest" or "armor_chest_rig" => InferBodyArmorProfile(profile, context),
                "chest_rig" => InferChestRigProfile(context),
                "backpack" => InferBackpackProfile(context),
                "armor_component" or "armor_mask" => InferFaceProtectionProfile(profile, context),
                "cosmetic_gasmask" => InferCosmeticGearProfile(context),
                _ => profile,
            };
        }

        if (context.TemplateFileName == "armorPlateTemplates.json")
        {
            return InferArmorPlateProfile(context);
        }

        if (context.TemplateFileName == "cosmeticsTemplates.json")
        {
            return InferCosmeticGearProfile(context);
        }

        if (context.TemplateFileName == "helmetTemplates.json")
        {
            return InferHelmetProfile(context);
        }

        if (context.TemplateFileName == "armorVestsTemplates.json")
        {
            return InferBodyArmorProfile("armor_vest", context);
        }

        if (context.TemplateFileName == "armorChestrigTemplates.json")
        {
            return InferBodyArmorProfile("armor_chest_rig", context);
        }

        if (context.TemplateFileName == "chestrigTemplates.json")
        {
            return InferChestRigProfile(context);
        }

        if (context.TemplateFileName == "bagTemplates.json")
        {
            return InferBackpackProfile(context);
        }

        if (context.TemplateFileName == "armorMasksTemplates.json" && ContainsAnyKeyword(context.Name, ["glasses", "goggles", "eyewear", "射击眼镜", "护目镜", "眼镜", "condor"]))
        {
            return InferEyewearProfile(context);
        }

        if (context.TemplateFileName == "armorMasksTemplates.json")
        {
            return InferFaceProtectionProfile("armor_mask", context);
        }

        if (context.TemplateFileName == "armorComponentsTemplates.json")
        {
            return InferFaceProtectionProfile("armor_component", context);
        }

        if (context.TemplateFileName == "headsetTemplates.json")
        {
            return "headset";
        }

        if (ContainsAnyKeyword(context.Name, ["headset", "headphones", "耳机", "耳麦"]))
        {
            return "headset";
        }

        if (ContainsAnyKeyword(context.Name, ["beret", "贝雷帽", "boonie", "watch cap"]))
        {
            return "cosmetic_headwear";
        }

        if (ContainsAnyKeyword(context.Name, ["back panel", "背部面板"]))
        {
            return "back_panel";
        }

        if (ContainsAnyKeyword(context.Name, ["腰带", "belt", "warbelt", "battle belt", "警用腰带", "mule"]))
        {
            return "belt_harness";
        }

        if (ContainsAnyKeyword(context.Name, ["backpack", "ruck", "pack", "bag", "背包", "背负系统", "bvs", "nice comm"]))
        {
            return InferBackpackProfile(context);
        }

        if (ContainsAnyKeyword(context.Name, ["soft armor", "armor plate", "plate", "插板", "软甲", "防弹插板"]))
        {
            return InferArmorPlateProfile(context);
        }

        if (ContainsAnyKeyword(context.Name, ["helmet", "头盔", "helm", "ops-core", "ops core", "fast mt", "tc2000", "mich", "ronin"]))
        {
            return InferHelmetProfile(context);
        }

        if (ContainsAnyKeyword(context.Name, ["glasses", "goggles", "eyewear", "射击眼镜", "护目镜", "眼镜", "condor"]))
        {
            return InferEyewearProfile(context);
        }

        if (ContainsAnyKeyword(context.Name, ["visor", "face shield", "mandible", "aventail", "side armor", "applique", "护颈", "面甲"]))
        {
            return InferFaceProtectionProfile("armor_component", context);
        }

        if (ContainsAnyKeyword(context.Name, ["gas mask", "respirator", "mask", "面罩", "防毒"]))
        {
            return InferFaceProtectionProfile("armor_mask", context);
        }

        if (ContainsAnyKeyword(context.Name, ["plate carrier", "armor rig", "armored rig", "carrier", "jpc", "apc", "sohpc", "cgpc", "avs", "tqs", "战术背心", "携行背心", "板携行", "板携行背心", "护甲胸挂", "防弹胸挂"]))
        {
            return InferBodyArmorProfile("armor_chest_rig", context);
        }

        if (hasArmorClass && ContainsAnyKeyword(context.Name, ["rig", "胸挂", "背心", "vest"]))
        {
            return InferBodyArmorProfile("armor_chest_rig", context);
        }

        if (ContainsAnyKeyword(context.Name, ["rig", "胸挂"]))
        {
            return InferChestRigProfile(context);
        }

        if (hasArmorClass && ContainsAnyKeyword(context.Name, ["背心", "vest"]))
        {
            return InferBodyArmorProfile("armor_vest", context);
        }

        if (ContainsAnyKeyword(context.Name, ["armor", "vest", "body armor", "护甲", "防弹衣"]))
        {
            return InferBodyArmorProfile("armor_vest", context);
        }

        return null;
    }

    public static string? InferModProfile(RuleSet rules, PatchAnalysisContext context, JsonObject patch, ItemInfo itemInfo)
    {
        var baseProfile = !string.IsNullOrWhiteSpace(context.NormalizedParentId)
            && rules.Attachment.ModParentBaseProfiles.TryGetValue(context.NormalizedParentId, out var mappedProfile)
            ? mappedProfile
            : null;

        if (string.Equals(context.ModType, "bayonet", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("bayonet", StringComparison.OrdinalIgnoreCase))
        {
            return "bayonet";
        }

        if (string.Equals(context.ModType, "booster", StringComparison.OrdinalIgnoreCase)
            || context.Name.Contains("booster", StringComparison.OrdinalIgnoreCase))
        {
            return "booster";
        }

        if (context.ModType.Contains("muzzle", StringComparison.OrdinalIgnoreCase) || (baseProfile?.StartsWith("muzzle", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            if (context.ModType is "muzzle_supp_adapter" or "sig_taper_brake" || context.ModType.Contains("adapter", StringComparison.OrdinalIgnoreCase))
            {
                return "muzzle_adapter";
            }

            if (context.Name.Contains("adapter", StringComparison.OrdinalIgnoreCase) && ContainsAnyKeyword(context.Name, ["muzzle", "suppressor", "silencer", "taper", "qd"]))
            {
                return "muzzle_adapter";
            }

            if (ContainsAnyKeyword(context.Name, ["silencer", "suppressor", "qd", "pbs", "消音器", "抑制器", "消声器", "глушитель"]))
            {
                return RealismPatchGenerator.InferSuppressorProfileFromName(context.Name);
            }

            if (ContainsAnyKeyword(context.Name, ["brake", "comp", "compensator", "制退器"]))
            {
                return "muzzle_brake";
            }

            if (ContainsAnyKeyword(context.Name, ["thread", "protector", "螺纹保护", "保护帽"]))
            {
                return "muzzle_thread";
            }

            if (ContainsAnyKeyword(context.Name, ["消焰器", "消焰", "火帽", "flash hider"]))
            {
                return "muzzle_flashhider";
            }

            return "muzzle_flashhider";
        }

        if (context.ModType.Contains("barrel", StringComparison.OrdinalIgnoreCase) || context.ModType.Contains("short_barrel", StringComparison.OrdinalIgnoreCase) || (baseProfile?.StartsWith("barrel", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return RealismPatchGenerator.InferBarrelProfileFromName(context.Name);
        }

        if (context.ModType.Contains("handguard", StringComparison.OrdinalIgnoreCase) || (baseProfile?.StartsWith("handguard", StringComparison.OrdinalIgnoreCase) ?? false) || RealismPatchGenerator.IsHandguardLikeName(context.Name))
        {
            return RealismPatchGenerator.InferHandguardProfileFromName(context.Name);
        }

        if (context.ModType == "magazine" || string.Equals(baseProfile, "magazine", StringComparison.OrdinalIgnoreCase) || (context.ModType.Contains("mag", StringComparison.OrdinalIgnoreCase) && !context.ModType.Contains("malf", StringComparison.OrdinalIgnoreCase)))
        {
            return RealismPatchGenerator.InferMagazineProfile(RealismPatchGenerator.ExtractMagCapacity(itemInfo, context.Name), context.Name);
        }

        if (context.ModType == "foregrip_adapter")
        {
            return "mount";
        }

        if (context.ModType is "grip" or "foregrip" or "verticalgrip" or "handstop" || context.ModType.Contains("foregrip", StringComparison.OrdinalIgnoreCase))
        {
            return "foregrip";
        }

        if (context.ModType == "bipod")
        {
            return "bipod";
        }

        if (context.ModType is "gas" or "gasblock" or "gas_block")
        {
            return "gasblock";
        }

        if (context.ModType is "stock_adapter" or "grip_stock_adapter")
        {
            return "stock_adapter";
        }

        if (context.ModType is "buffer_adapter" or "buffer_tube" || context.ModType.StartsWith("buffer", StringComparison.OrdinalIgnoreCase))
        {
            return "buffer_adapter";
        }

        if (context.ModType.Contains("buttpad", StringComparison.OrdinalIgnoreCase))
        {
            return "stock_buttpad";
        }

        if (context.ModType == "stock" || context.ModType.StartsWith("stock", StringComparison.OrdinalIgnoreCase) || context.ModType.EndsWith("_stock", StringComparison.OrdinalIgnoreCase))
        {
            return RealismPatchGenerator.InferModStockProfile(context.Name, patch, itemInfo);
        }

        if (context.ModType is "pistolgrip" or "pistol_grip" || (context.ModType.Contains("pistol", StringComparison.OrdinalIgnoreCase) && context.ModType.Contains("grip", StringComparison.OrdinalIgnoreCase)))
        {
            return "pistol_grip";
        }

        if (string.Equals(context.ModType, "UBGL", StringComparison.OrdinalIgnoreCase)
            || context.ModType.Contains("grenade_launcher", StringComparison.OrdinalIgnoreCase)
            || context.ModType.Contains("grenade launcher", StringComparison.OrdinalIgnoreCase))
        {
            return "ubgl";
        }

        if (context.ModType == "receiver" || context.ModType.Contains("receiver", StringComparison.OrdinalIgnoreCase) || context.ModType.Contains("reciever", StringComparison.OrdinalIgnoreCase))
        {
            return "receiver";
        }

        if (context.ModType == "mount" || context.ModType.Contains("mount", StringComparison.OrdinalIgnoreCase) || context.ModType.Contains("rail", StringComparison.OrdinalIgnoreCase))
        {
            if (ContainsAnyKeyword(context.Name, ["silencer_", "suppressor", "消音器", "抑制器", "消声器", "глушитель"]))
            {
                return RealismPatchGenerator.InferSuppressorProfileFromName(context.Name);
            }

            if (ContainsAnyKeyword(context.Name, ["barrel and rail system", "rail system", "front-end assembly", "front end assembly"]) && ContainsAnyKeyword(context.Name, ["m-lok", "mlok", "handguard", "forend", "barrel"]))
            {
                return RealismPatchGenerator.InferHandguardProfileFromName(context.Name);
            }

            return "mount";
        }

        if (context.ModType == "iron_sight")
        {
            return "iron_sight";
        }

        if (context.ModType == "trigger")
        {
            return "trigger";
        }

        if (context.ModType == "catch")
        {
            return "catch";
        }

        if (context.ModType == "hammer")
        {
            return "hammer";
        }

        if (context.ModType is "reflex_sight" or "compact_reflex_sight")
        {
            return "scope_red_dot";
        }

        if (context.ModType is "scope" or "assault_scope")
        {
            return "scope_magnified";
        }

        if (context.ModType.Contains("laser", StringComparison.OrdinalIgnoreCase) || context.ModType.Contains("flashlight", StringComparison.OrdinalIgnoreCase) || context.ModType.Contains("tactical", StringComparison.OrdinalIgnoreCase))
        {
            return "flashlight_laser";
        }

        if (context.ModType == "sight")
        {
            var sightProfile = RealismPatchGenerator.InferSightProfileFromName(context.Name);
            if (!string.IsNullOrWhiteSpace(sightProfile))
            {
                return sightProfile;
            }

            if (string.Equals(context.TemplateFileName, "ScopeTemplates.json", StringComparison.OrdinalIgnoreCase))
            {
                return "scope_red_dot";
            }

            if (baseProfile is "iron_sight" or "scope_red_dot" or "scope_magnified")
            {
                return baseProfile;
            }

            return "scope_red_dot";
        }

        var fallbackProfile = InferModProfileFromNameFallback(context.Name, patch, itemInfo);
        if (!string.IsNullOrWhiteSpace(fallbackProfile))
        {
            return fallbackProfile;
        }

        if (!string.IsNullOrWhiteSpace(context.TemplateFileName))
        {
            var templateProfile = InferModProfileFromTemplateFile(context.TemplateFileName, patch, itemInfo);
            if (!string.IsNullOrWhiteSpace(templateProfile))
            {
                return templateProfile;
            }
        }

        return baseProfile;
    }

    private static string? InferModProfileFromTemplateFile(string? templateFile, JsonObject patch, ItemInfo itemInfo)
    {
        if (string.IsNullOrWhiteSpace(templateFile))
        {
            return null;
        }

        var itemName = RealismPatchGenerator.GetLowerText(patch["Name"]);
        return templateFile switch
        {
            "MagazineTemplates.json" => RealismPatchGenerator.InferMagazineProfile(RealismPatchGenerator.ExtractMagCapacity(itemInfo, itemName), itemName),
            "BarrelTemplates.json" => RealismPatchGenerator.InferBarrelProfileFromName(itemName),
            "HandguardTemplates.json" => RealismPatchGenerator.InferHandguardProfileFromName(itemName),
            "StockTemplates.json" => RealismPatchGenerator.InferModStockProfile(itemName, patch, itemInfo),
            "ChargingHandleTemplates.json" => "charging_handle",
            "ScopeTemplates.json" => RealismPatchGenerator.InferSightProfileFromName(itemName) ?? "scope_red_dot",
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

    private static string? InferModProfileFromNameFallback(string name, JsonObject patch, ItemInfo itemInfo)
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
            return RealismPatchGenerator.InferModStockProfile(name, patch, itemInfo);
        }

        if (name.StartsWith("receiver_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("reciever_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["机匣", "机匣盖", "防尘盖", "receiver", "reciever", "dust cover", "upper receiver", "upper reciever", "slide", "крышка ствольной коробки"]))
        {
            return "receiver";
        }

        if (name.StartsWith("mag_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("magazine_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["弹匣", "magazine", "drum", "casket", "магазин"]))
        {
            return RealismPatchGenerator.InferMagazineProfile(RealismPatchGenerator.ExtractMagCapacity(itemInfo, name), name);
        }

        if (RealismPatchGenerator.IsHandguardLikeName(name))
        {
            return RealismPatchGenerator.InferHandguardProfileFromName(name);
        }

        if (name.StartsWith("silencer_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["suppressor", "消音器", "抑制器", "消声器", "глушитель"]))
        {
            return RealismPatchGenerator.InferSuppressorProfileFromName(name);
        }

        if (name.StartsWith("railq", StringComparison.OrdinalIgnoreCase))
        {
            return "handguard_medium";
        }

        if (ContainsAnyKeyword(name, ["barrel and rail system", "rail system", "front-end assembly", "front end assembly"]) && ContainsAnyKeyword(name, ["m-lok", "mlok", "keymod", "barrel", "forend", "handguard", "护木"]))
        {
            return RealismPatchGenerator.InferHandguardProfileFromName(name);
        }

        if (name.StartsWith("mount_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["导轨", "基座", "偏移座", "镜座", "mount", "rail segment", "rail", "offset mount"]))
        {
            return "mount";
        }

        if (name.StartsWith("barrel_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["枪管", "barrel", "ствол"]))
        {
            return RealismPatchGenerator.InferBarrelProfileFromName(name);
        }

        if (name.StartsWith("sight_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("scope_", StringComparison.OrdinalIgnoreCase) || ContainsAnyKeyword(name, ["瞄具", "瞄准镜", "全息", "红点", "反射式"]))
        {
            return RealismPatchGenerator.InferSightProfileFromName(name) ?? "scope_red_dot";
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
            return RealismPatchGenerator.InferHandguardProfileFromName(name);
        }

        return null;
    }

    private static string InferArmorPlateProfile(PatchAnalysisContext context)
    {
        if (ContainsAnyKeyword(context.GearArmorClassText, ["helmet_armor", "helmet armor", "helmet", "ears", "nape", "top", "jaw", "eyes"]))
        {
            return "armor_plate_helmet";
        }

        if (ContainsAnyKeyword(context.GearArmorClassText, ["soft armor", "soft", "backer", "iiia", "gost 2", "gost 2a", "2a", "3a", "soft_armor", "软甲", "软插板"]))
        {
            return "armor_plate_soft";
        }

        return "armor_plate_hard";
    }

    private static string InferBodyArmorProfile(string baseProfile, PatchAnalysisContext context)
    {
        if (ContainsAnyKeyword(context.GearArmorClassText, ["gost 4", "gost 5", "gost 5a", "gost 6", "nij iii+", "nij iv", "rf3", "xsapi", "esapi", "mk4a", "rev. g", "rev. j", "pm 5", "pm 8", "pm 10", "plates"]))
        {
            return $"{baseProfile}_heavy";
        }

        if (ContainsAnyKeyword(context.GearArmorClassText, ["gost 2", "gost 2a", "gost 3", "gost 3a", "nij ii", "nij iia", "nij iii", "pm 2", "pm 3"]))
        {
            return $"{baseProfile}_light";
        }

        return $"{baseProfile}_heavy";
    }

    private static string? InferCosmeticGearProfile(PatchAnalysisContext context)
    {
        if (context.IsGasMask == true)
        {
            return "cosmetic_gasmask";
        }

        if (context.HasGasOrRadProtection)
        {
            return "cosmetic_gasmask";
        }

        if (ContainsAnyKeyword(context.Name, ["gas mask", "respirator", "防毒", "防毒面具", "gasmask", "maska"]))
        {
            return "cosmetic_gasmask";
        }

        if (ContainsAnyKeyword(context.Name, ["beret", "贝雷帽", "cap", "帽", "boonie", "watch cap"]))
        {
            return "cosmetic_headwear";
        }

        return null;
    }

    private static string InferHelmetProfile(PatchAnalysisContext context)
    {
        return ContainsAnyKeyword(context.Name, ["altyn", "rys", "ronin", "maska", "vulkan", "tor", "zsh", "lshz", "kiver", "sphera", "devtac", "k1c", "shpm", "psh97", "ssh-68", "ssh68", "neosteel"])
            ? "helmet_heavy"
            : "helmet_light";
    }

    private static string InferFaceProtectionProfile(string baseProfile, PatchAnalysisContext context)
    {
        if (baseProfile == "armor_component")
        {
            return ContainsAnyKeyword(context.Name, ["shield", "face shield", "faceshield", "visor", "面甲", "面罩"])
                ? "armor_component_faceshield"
                : "armor_component_accessory";
        }

        return ContainsAnyKeyword(context.GearArmorClassText, ["nij", "gost", "v50", "anti-shatter", "ansi", "mil-prf", "bs en", "ballistic"])
            ? "armor_mask_ballistic"
            : "armor_mask_decorative";
    }

    private static string InferBackpackProfile(PatchAnalysisContext context)
    {
        return ContainsAnyKeyword(context.Name, ["sling", "daypack", "day pack", "drawbridge", "switchblade", "medpack", "medbag", "redfox", "wild", "takedown", "t20", "vertx"])
            ? "backpack_compact"
            : "backpack_full";
    }

    private static string InferEyewearProfile(PatchAnalysisContext context)
    {
        return ContainsAnyKeyword(context.GearArmorClassText, ["v50", "anti-shatter", "ansi", "mil-prf", "ballistic", "z87", "31013"])
            ? "protective_eyewear_ballistic"
            : "protective_eyewear_standard";
    }

    private static string InferChestRigProfile(PatchAnalysisContext context)
    {
        return ContainsAnyKeyword(context.Name, ["bankrobber", "micro", "d3crx", "cs_assault", "thunderbolt", "bssmk1", "recon", "zulu"])
            ? "chest_rig_light"
            : "chest_rig_heavy";
    }

    private static bool ContainsAnyKeyword(string text, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}