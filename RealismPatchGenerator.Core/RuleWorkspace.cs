namespace RealismPatchGenerator.Core;

public static class RuleWorkspace
{
    private static readonly string[] DefaultRuleFiles =
    [
        "weapon_rules.json",
        "attachment_rules.json",
        "ammo_rules.json",
        "gear_rules.json",
    ];

    public static IReadOnlyList<string> RuleFileNames => DefaultRuleFiles;

    public static string GetRulesDirectory(string basePath)
    {
        return Path.Combine(Path.GetFullPath(basePath), "rules");
    }

    public static string GetRuleFilePath(string basePath, string ruleFileName)
    {
        return Path.Combine(GetRulesDirectory(basePath), ruleFileName);
    }

    public static void EnsureInitialized(string basePath, Action<string>? log = null)
    {
        RuleSetLoader.Load(Path.GetFullPath(basePath), log ?? (_ => { }));
    }

    public static bool TryNormalizeRuleFile(string ruleFileName, string rawText, out string normalizedJson, out string errorMessage)
    {
        return RuleSetLoader.TryNormalizeRuleFile(ruleFileName, rawText, out normalizedJson, out errorMessage);
    }
}