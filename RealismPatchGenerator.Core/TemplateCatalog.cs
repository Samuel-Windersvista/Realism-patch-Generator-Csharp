using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class TemplateCatalog
{
    private static readonly string[] TemplateDirectories = ["weapons", "attatchments", "ammo", "gear", "consumables"];

    public static TemplateCatalogSnapshot Load(string templatesBasePath, Action<string> log)
    {
        var templates = new Dictionary<string, SortedDictionary<string, JsonObject>>(StringComparer.OrdinalIgnoreCase);
        var templateById = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        var templateFileByItemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var templateParentIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativeDir in TemplateDirectories)
        {
            var directoryPath = Path.Combine(templatesBasePath, relativeDir);
            if (!Directory.Exists(directoryPath))
            {
                continue;
            }

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(filePath);
                var text = File.ReadAllText(filePath);
                var root = JsonNode.Parse(text)?.AsObject();
                if (root is null)
                {
                    continue;
                }

                var byId = new SortedDictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in root)
                {
                    if (pair.Value is not JsonObject value)
                    {
                        continue;
                    }

                    byId[pair.Key] = (JsonObject)value.DeepClone();
                    templateById[pair.Key] = (JsonObject)value.DeepClone();
                    templateFileByItemId[pair.Key] = fileName;
                }

                templates[fileName] = byId;
                log($"已加载模板: {fileName} ({byId.Count} 项)");
            }
        }

        foreach (var pair in StaticData.ParentIdToTemplate)
        {
            if (string.Equals(pair.Value, "AMMO", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var templateFile = Path.GetFileName(pair.Value);
            if (!templateParentIndex.TryGetValue(templateFile, out var parents))
            {
                parents = [];
                templateParentIndex[templateFile] = parents;
            }

            parents.Add(pair.Key);
        }

        return new TemplateCatalogSnapshot(templates, templateById, templateFileByItemId, templateParentIndex);
    }
}

internal sealed record TemplateCatalogSnapshot(
    Dictionary<string, SortedDictionary<string, JsonObject>> Templates,
    Dictionary<string, JsonObject> TemplateById,
    Dictionary<string, string> TemplateFileByItemId,
    Dictionary<string, List<string>> TemplateParentIndex);