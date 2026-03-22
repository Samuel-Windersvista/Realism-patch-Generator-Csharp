using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

public enum ItemFormat
{
    Unknown,
    CurrentPatch,
    Standard,
    Clone,
    ItemToClone,
    Vir,
    TemplateId,
}

public sealed class ItemInfo
{
    public string ItemId { get; init; } = string.Empty;
    public string? ParentId { get; set; }
    public string? CloneId { get; set; }
    public string? TemplateId { get; set; }
    public string? TemplateFile { get; set; }
    public string? Name { get; set; }
    public bool IsWeapon { get; set; }
    public bool IsGear { get; set; }
    public bool IsConsumable { get; set; }
    public string? ItemType { get; set; }
    public JsonObject Properties { get; set; } = [];
    public JsonObject SourceProperties { get; set; } = [];
    public string? SourceFile { get; set; }
    public ItemFormat Format { get; set; }
}

public sealed class GenerationStatistics
{
    public int WeaponCount { get; set; }
    public int AttachmentCount { get; set; }
    public int AmmoCount { get; set; }
    public int GearCount { get; set; }
    public int ConsumableCount { get; set; }
    public int TotalCount => WeaponCount + AttachmentCount + AmmoCount + GearCount + ConsumableCount;
}

public sealed class GenerationResult
{
    public required string BasePath { get; init; }
    public required string OutputPath { get; init; }
    public required uint UsedSeed { get; init; }
    public required GenerationStatistics Statistics { get; init; }
    public IReadOnlyList<string> Logs { get; init; } = [];
}

public static class WorkspaceLocator
{
    public static string? FindApplicationRoot(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var found = FindApplicationRoot(candidate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public static string? FindApplicationRoot(string? startPath)
    {
        var current = GetStartingDirectory(startPath);
        while (current is not null)
        {
            if (IsDataRoot(current.FullName) || IsApplicationMarker(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public static string? FindDataRoot(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var found = FindDataRoot(candidate);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public static string? FindDataRoot(string? startPath)
    {
        var current = GetStartingDirectory(startPath);
        while (current is not null)
        {
            if (IsDataRoot(current.FullName))
            {
                return current.FullName;
            }

            if (IsApplicationMarker(current.FullName))
            {
                return null;
            }

            current = current.Parent;
        }

        return null;
    }

    public static bool IsDataRoot(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var inputDir = Path.Combine(fullPath, "input");
        var templateDir = RuleWorkspace.GetTemplatesDirectory(fullPath);
        return Directory.Exists(inputDir) && Directory.Exists(templateDir);
    }

    private static DirectoryInfo? GetStartingDirectory(string? startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        var path = File.Exists(startPath) ? Path.GetDirectoryName(startPath) : startPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        return new DirectoryInfo(Path.GetFullPath(path));
    }

    private static bool IsApplicationMarker(string path)
    {
        return File.Exists(Path.Combine(path, "RealismPatchGenerator.slnx"))
            || (Directory.Exists(Path.Combine(path, "RealismPatchGenerator.Core"))
                && Directory.Exists(Path.Combine(path, "RealismPatchGenerator.Cli"))
                && Directory.Exists(Path.Combine(path, "RealismPatchGenerator.Gui")));
    }
}