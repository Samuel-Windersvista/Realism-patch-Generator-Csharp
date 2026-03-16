using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

public sealed class ItemExceptionEntry
{
    public string ItemId { get; init; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public JsonObject Overrides { get; set; } = [];

    public IReadOnlyCollection<string> GetOverrideFields()
    {
        return Overrides
            .Select(pair => pair.Key)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

public sealed class ItemExceptionDocument
{
    public Dictionary<string, ItemExceptionEntry> Items { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetEntry(string itemId, out ItemExceptionEntry entry)
    {
        if (Items.TryGetValue(itemId, out var existing) && existing.Enabled)
        {
            entry = existing;
            return true;
        }

        entry = new ItemExceptionEntry();
        return false;
    }

    public HashSet<string> GetOverrideFieldSet(string itemId)
    {
        return TryGetEntry(itemId, out var entry)
            ? entry.GetOverrideFields().ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

public static class ItemExceptionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public const string FileName = "item_exceptions.json";

    public static string GetFilePath(string basePath)
    {
        return Path.Combine(Path.GetFullPath(basePath), "rules", FileName);
    }

    public static ItemExceptionDocument Load(string basePath)
    {
        var path = GetFilePath(basePath);
        if (!File.Exists(path))
        {
            return new ItemExceptionDocument();
        }

        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidOperationException($"例外物品文件不是有效 JSON 对象: {path}");

        var document = new ItemExceptionDocument();
        if (root["items"] is not JsonObject itemsObject)
        {
            return document;
        }

        foreach (var pair in itemsObject.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (pair.Value is not JsonObject entryObject)
            {
                continue;
            }

            var itemId = pair.Key.Trim();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            document.Items[itemId] = new ItemExceptionEntry
            {
                ItemId = itemId,
                Enabled = entryObject["enabled"]?.GetValue<bool?>() ?? true,
                Name = entryObject["name"]?.GetValue<string?>() ?? string.Empty,
                SourceFile = entryObject["sourceFile"]?.GetValue<string?>() ?? string.Empty,
                Notes = entryObject["notes"]?.GetValue<string?>() ?? string.Empty,
                Overrides = entryObject["overrides"] as JsonObject is { } overrides
                    ? (JsonObject)overrides.DeepClone()
                    : [],
            };
        }

        return document;
    }

    public static void Save(string basePath, ItemExceptionDocument document)
    {
        var path = GetFilePath(basePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var itemsObject = new JsonObject();
        foreach (var pair in document.Items.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var entry = pair.Value;
            var itemId = string.IsNullOrWhiteSpace(entry.ItemId) ? pair.Key : entry.ItemId.Trim();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                continue;
            }

            itemsObject[itemId] = new JsonObject
            {
                ["enabled"] = entry.Enabled,
                ["name"] = string.IsNullOrWhiteSpace(entry.Name) ? null : entry.Name,
                ["sourceFile"] = string.IsNullOrWhiteSpace(entry.SourceFile) ? null : entry.SourceFile,
                ["notes"] = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes,
                ["overrides"] = entry.Overrides.Count == 0 ? new JsonObject() : (JsonObject)entry.Overrides.DeepClone(),
            };
        }

        var root = new JsonObject
        {
            ["items"] = itemsObject,
        };

        File.WriteAllText(path, root.ToJsonString(JsonOptions));
    }
}