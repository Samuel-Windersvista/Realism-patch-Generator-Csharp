using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

public sealed class ItemExceptionImportCandidate
{
    public required string ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SourceFile { get; init; } = string.Empty;
    public string LocatedFile { get; init; } = string.Empty;
    public string Origin { get; init; } = string.Empty;
    public JsonObject Fields { get; init; } = [];
}

public static class ItemExceptionImportService
{
    private static readonly HashSet<string> CurrentPatchIgnoredKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "$type", "ItemID", "TemplateID", "parentId", "itemTplToClone", "clone", "ItemToClone",
        "enable", "locales", "LocalePush", "OverrideProperties", "overrideProperties", "item", "items", "handbook",
    };

    public static IReadOnlyList<ItemExceptionImportCandidate> SearchFromOutputByName(string outputDirectory, string nameQuery, int maxResults = 200)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory) || string.IsNullOrWhiteSpace(nameQuery))
        {
            return [];
        }

        var results = new List<ItemExceptionImportCandidate>();
        foreach (var filePath in Directory.EnumerateFiles(outputDirectory, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryParseRoot(filePath, out var root))
            {
                continue;
            }

            foreach (var pair in root.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Value is not JsonObject itemObject)
                {
                    continue;
                }

                var name = itemObject["Name"]?.GetValue<string?>() ?? string.Empty;
                if (!NameMatches(name, nameQuery))
                {
                    continue;
                }

                results.Add(CreateOutputCandidate(outputDirectory, filePath, pair.Key, itemObject));
                if (results.Count >= maxResults)
                {
                    return results;
                }
            }
        }

        return results;
    }

    public static IReadOnlyList<ItemExceptionImportCandidate> SearchFromInputByName(string basePath, string nameQuery, int maxResults = 200)
    {
        var inputRoot = Path.Combine(Path.GetFullPath(basePath), "input");
        if (!Directory.Exists(inputRoot) || string.IsNullOrWhiteSpace(nameQuery))
        {
            return [];
        }

        var results = new List<ItemExceptionImportCandidate>();
        foreach (var filePath in Directory.EnumerateFiles(inputRoot, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryParseRoot(filePath, out var root))
            {
                continue;
            }

            foreach (var pair in root.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (pair.Value is not JsonObject itemObject)
                {
                    continue;
                }

                var name = ExtractName(itemObject);
                if (!NameMatches(name, nameQuery))
                {
                    continue;
                }

                results.Add(CreateInputCandidate(inputRoot, filePath, pair.Key, itemObject, string.Empty));
                if (results.Count >= maxResults)
                {
                    return results;
                }
            }
        }

        return results;
    }

    public static ItemExceptionImportCandidate? ImportFromOutput(string outputDirectory, string itemId, string? sourceFile = null)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            return null;
        }

        foreach (var candidatePath in EnumerateOutputCandidateFiles(outputDirectory, sourceFile))
        {
            var imported = TryReadOutputCandidate(outputDirectory, candidatePath, itemId);
            if (imported is not null)
            {
                return imported;
            }
        }

        foreach (var candidatePath in Directory.EnumerateFiles(outputDirectory, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var imported = TryReadOutputCandidate(outputDirectory, candidatePath, itemId);
            if (imported is not null)
            {
                return imported;
            }
        }

        return null;
    }

    public static ItemExceptionImportCandidate? ImportFromInput(string basePath, string itemId, string? sourceFile = null)
    {
        var inputRoot = Path.Combine(Path.GetFullPath(basePath), "input");
        if (!Directory.Exists(inputRoot))
        {
            return null;
        }

        foreach (var candidatePath in EnumerateInputCandidateFiles(inputRoot, sourceFile))
        {
            var imported = TryReadInputCandidate(inputRoot, candidatePath, itemId, sourceFile ?? string.Empty);
            if (imported is not null)
            {
                return imported;
            }
        }

        foreach (var candidatePath in Directory.EnumerateFiles(inputRoot, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var imported = TryReadInputCandidate(inputRoot, candidatePath, itemId, sourceFile ?? string.Empty);
            if (imported is not null)
            {
                return imported;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateOutputCandidateFiles(string outputDirectory, string? sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            yield break;
        }

        var normalized = sourceFile.Replace('/', Path.DirectorySeparatorChar);
        var sourceName = Path.GetFileNameWithoutExtension(normalized);
        var sourceDir = Path.GetDirectoryName(normalized) ?? string.Empty;
        yield return Path.Combine(outputDirectory, sourceDir, sourceName + "_realism_patch.json");
        yield return Path.Combine(outputDirectory, sourceDir, sourceName + ".json");
    }

    private static IEnumerable<string> EnumerateInputCandidateFiles(string inputRoot, string? sourceFile)
    {
        if (string.IsNullOrWhiteSpace(sourceFile))
        {
            yield break;
        }

        yield return Path.Combine(inputRoot, sourceFile.Replace('/', Path.DirectorySeparatorChar));
    }

    private static ItemExceptionImportCandidate? TryReadOutputCandidate(string outputDirectory, string filePath, string itemId)
    {
        if (!TryParseRoot(filePath, out var root) || root[itemId] is not JsonObject itemObject)
        {
            return null;
        }

        return CreateOutputCandidate(outputDirectory, filePath, itemId, itemObject);
    }

    private static ItemExceptionImportCandidate CreateOutputCandidate(string outputDirectory, string filePath, string itemId, JsonObject itemObject)
    {
        var fields = (JsonObject)itemObject.DeepClone();
        fields.Remove("ItemID");
        return new ItemExceptionImportCandidate
        {
            ItemId = itemId,
            Name = itemObject["Name"]?.GetValue<string?>() ?? string.Empty,
            SourceFile = Path.GetRelativePath(outputDirectory, filePath).Replace('\\', '/'),
            LocatedFile = filePath,
            Origin = "output",
            Fields = fields,
        };
    }

    private static ItemExceptionImportCandidate? TryReadInputCandidate(string inputRoot, string filePath, string itemId, string sourceFile)
    {
        if (!TryParseRoot(filePath, out var root) || root[itemId] is not JsonObject itemObject)
        {
            return null;
        }

        return CreateInputCandidate(inputRoot, filePath, itemId, itemObject, sourceFile);
    }

    private static ItemExceptionImportCandidate CreateInputCandidate(string inputRoot, string filePath, string itemId, JsonObject itemObject, string sourceFile)
    {
        var fields = ExtractEffectiveInputFields(itemObject);
        var name = ExtractName(itemObject);
        if (!string.IsNullOrWhiteSpace(name) && fields["Name"] is null)
        {
            fields["Name"] = name;
        }

        return new ItemExceptionImportCandidate
        {
            ItemId = itemId,
            Name = name,
            SourceFile = string.IsNullOrWhiteSpace(sourceFile)
                ? Path.GetRelativePath(inputRoot, filePath).Replace('\\', '/')
                : sourceFile,
            LocatedFile = filePath,
            Origin = "input",
            Fields = fields,
        };
    }

    private static bool TryParseRoot(string filePath, out JsonObject root)
    {
        root = new JsonObject();
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            root = JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject ?? new JsonObject();
            return root.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool NameMatches(string candidateName, string query)
    {
        return !string.IsNullOrWhiteSpace(candidateName)
            && candidateName.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static JsonObject ExtractEffectiveInputFields(JsonObject itemObject)
    {
        if (itemObject["overrideProperties"] is JsonObject standardOverrides)
        {
            return (JsonObject)standardOverrides.DeepClone();
        }

        if (itemObject["OverrideProperties"] is JsonObject cloneOverrides)
        {
            return (JsonObject)cloneOverrides.DeepClone();
        }

        if (itemObject["items"] is JsonObject itemsObject && itemsObject["_props"] is JsonObject multiProps)
        {
            return (JsonObject)multiProps.DeepClone();
        }

        if (itemObject["item"] is JsonObject itemNode && itemNode["_props"] is JsonObject singleProps)
        {
            return (JsonObject)singleProps.DeepClone();
        }

        var fields = new JsonObject();
        foreach (var pair in itemObject)
        {
            if (!CurrentPatchIgnoredKeys.Contains(pair.Key) && pair.Value is not null)
            {
                fields[pair.Key] = pair.Value.DeepClone();
            }
        }

        return fields;
    }

    private static string ExtractName(JsonObject itemObject)
    {
        if (!string.IsNullOrWhiteSpace(itemObject["Name"]?.GetValue<string?>()))
        {
            return itemObject["Name"]!.GetValue<string>();
        }

        if (itemObject["item"] is JsonObject itemNode && !string.IsNullOrWhiteSpace(itemNode["_name"]?.GetValue<string?>()))
        {
            return itemNode["_name"]!.GetValue<string>();
        }

        return ExtractLocalizedName(itemObject["locales"])
            ?? ExtractLocalizedName(itemObject["LocalePush"])
            ?? string.Empty;
    }

    private static string? ExtractLocalizedName(JsonNode? node)
    {
        if (node is not JsonObject localesObject)
        {
            return null;
        }

        foreach (var locale in localesObject)
        {
            if (locale.Value is not JsonObject localeFields)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(localeFields["Name"]?.GetValue<string?>()))
            {
                return localeFields["Name"]!.GetValue<string>();
            }

            if (!string.IsNullOrWhiteSpace(localeFields["name"]?.GetValue<string?>()))
            {
                return localeFields["name"]!.GetValue<string>();
            }
        }

        return null;
    }
}
