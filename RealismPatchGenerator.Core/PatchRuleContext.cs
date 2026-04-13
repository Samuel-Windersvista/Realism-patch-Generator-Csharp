using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal sealed class PatchRuleContext
{
    private PatchAnalysisContext? analysisContext;
    private bool weaponProfileResolved;
    private string? weaponProfile;
    private bool weaponCaliberProfileResolved;
    private string? weaponCaliberProfile;
    private bool weaponStockProfileResolved;
    private string weaponStockProfile = string.Empty;
    private bool gearProfileResolved;
    private string? gearProfile;
    private bool ammoProfileResolved;
    private string ammoProfile = string.Empty;
    private bool ammoSpecialProfileResolved;
    private string? ammoSpecialProfile;
    private bool modProfileResolved;
    private string? modProfile;
    private bool penetrationValueResolved;
    private double? penetrationValue;
    private bool ammoPenetrationTierResolved;
    private string ammoPenetrationTier = string.Empty;

    public PatchRuleContext(RealismPatchGenerator generator, RuleSet rules, JsonObject patch, ItemInfo itemInfo)
    {
        Generator = generator;
        Rules = rules;
        Patch = patch;
        ItemInfo = itemInfo;
    }

    public RealismPatchGenerator Generator { get; }

    public RuleSet Rules { get; }

    public JsonObject Patch { get; }

    public ItemInfo ItemInfo { get; }

    public PatchAnalysisContext AnalysisContext => analysisContext ??= PatchAnalysisContextFactory.Create(Generator, Patch, ItemInfo);

    public void InvalidateAnalysis()
    {
        analysisContext = null;
        weaponProfileResolved = false;
        weaponCaliberProfileResolved = false;
        weaponStockProfileResolved = false;
        gearProfileResolved = false;
        ammoProfileResolved = false;
        ammoSpecialProfileResolved = false;
        modProfileResolved = false;
        ammoPenetrationTierResolved = false;
    }

    public string? GetWeaponProfile()
    {
        if (!weaponProfileResolved)
        {
            weaponProfile = ProfileInferenceService.InferWeaponProfile(Rules, AnalysisContext);
            weaponProfileResolved = true;
        }

        return weaponProfile;
    }

    public string? GetWeaponCaliberProfile()
    {
        if (!weaponCaliberProfileResolved)
        {
            weaponCaliberProfile = ProfileInferenceService.InferWeaponCaliberProfile(Rules, AnalysisContext);
            weaponCaliberProfileResolved = true;
        }

        return weaponCaliberProfile;
    }

    public string GetWeaponStockProfile()
    {
        if (!weaponStockProfileResolved)
        {
            weaponStockProfile = ProfileInferenceService.InferWeaponStockProfile(AnalysisContext);
            weaponStockProfileResolved = true;
        }

        return weaponStockProfile;
    }

    public string? GetGearProfile()
    {
        if (!gearProfileResolved)
        {
            gearProfile = ProfileInferenceService.InferGearProfile(AnalysisContext);
            gearProfileResolved = true;
        }

        return gearProfile;
    }

    public string GetAmmoProfile()
    {
        if (!ammoProfileResolved)
        {
            ammoProfile = ProfileInferenceService.InferAmmoProfile(Rules, AnalysisContext);
            ammoProfileResolved = true;
        }

        return ammoProfile;
    }

    public string? GetAmmoSpecialProfile()
    {
        if (!ammoSpecialProfileResolved)
        {
            ammoSpecialProfile = ProfileInferenceService.InferAmmoSpecialProfile(Rules, AnalysisContext);
            ammoSpecialProfileResolved = true;
        }

        return ammoSpecialProfile;
    }

    public string? GetModProfile()
    {
        if (!modProfileResolved)
        {
            modProfile = ProfileInferenceService.InferModProfile(Rules, AnalysisContext, Patch, ItemInfo);
            modProfileResolved = true;
        }

        return modProfile;
    }

    public double? GetPenetrationValue()
    {
        if (!penetrationValueResolved)
        {
            penetrationValue = AmmoRuleEngine.ExtractPenetrationValue(Patch, ItemInfo);
            penetrationValueResolved = true;
        }

        return penetrationValue;
    }

    public string GetAmmoPenetrationTier()
    {
        if (!ammoPenetrationTierResolved)
        {
            var resolvedPenetration = GetPenetrationValue();
            if (resolvedPenetration is null)
            {
                ammoPenetrationTier = "pen_lvl_5";
            }
            else
            {
                ammoPenetrationTier = resolvedPenetration > 130 ? "pen_lvl_11" : "pen_lvl_1";
                foreach (var pair in Rules.Ammo.AmmoPenetrationTiers)
                {
                    if (resolvedPenetration >= pair.Value.Min && resolvedPenetration <= pair.Value.Max)
                    {
                        ammoPenetrationTier = pair.Key;
                        break;
                    }
                }
            }

            ammoPenetrationTierResolved = true;
        }

        return ammoPenetrationTier;
    }
}