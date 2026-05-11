using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed class PatchStore
{
    private readonly Dictionary<string, JsonObject> weaponPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> attachmentPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> ammoPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> gearPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, JsonObject> consumablePatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ItemInfo> generatedItemInfoById = new(StringComparer.OrdinalIgnoreCase);

    public void StorePatch(string itemId, JsonObject patch)
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

    public void StoreItemInfo(string itemId, ItemInfo itemInfo)
        => generatedItemInfoById[itemId] = itemInfo;

    public bool TryGetStoredPatchById(string itemId, out JsonObject patch)
    {
        if (weaponPatches.TryGetValue(itemId, out var weaponPatch))
        {
            patch = weaponPatch!;
            return true;
        }

        if (attachmentPatches.TryGetValue(itemId, out var attachmentPatch))
        {
            patch = attachmentPatch!;
            return true;
        }

        if (ammoPatches.TryGetValue(itemId, out var ammoPatch))
        {
            patch = ammoPatch!;
            return true;
        }

        if (gearPatches.TryGetValue(itemId, out var gearPatch))
        {
            patch = gearPatch!;
            return true;
        }

        if (consumablePatches.TryGetValue(itemId, out var consumablePatch))
        {
            patch = consumablePatch!;
            return true;
        }

        patch = new JsonObject();
        return false;
    }

    public bool TryGetStoredPatchAndInfo(string itemId, out JsonObject patch, out ItemInfo itemInfo)
    {
        if (TryGetStoredPatchById(itemId, out patch)
            && generatedItemInfoById.TryGetValue(itemId, out var storedItemInfo))
        {
            itemInfo = storedItemInfo!;
            return true;
        }

        itemInfo = new ItemInfo();
        return false;
    }

    public GenerationStatistics CreateStatistics()
        => new()
        {
            WeaponCount = weaponPatches.Count,
            AttachmentCount = attachmentPatches.Count,
            AmmoCount = ammoPatches.Count,
            GearCount = gearPatches.Count,
            ConsumableCount = consumablePatches.Count,
        };
}
