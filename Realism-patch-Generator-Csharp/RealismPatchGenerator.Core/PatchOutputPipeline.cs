using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

internal static class PatchOutputPipeline
{
    private static readonly JsonSerializerOptions OutputJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonWriterOptions OutputWriterOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Save(string outputPath, IReadOnlyList<FilePatchOutput> outputs, Action<string> log)
    {
        Directory.CreateDirectory(outputPath);

        foreach (var output in outputs)
        {
            if (output.Entries.Count == 0)
            {
                continue;
            }

            var sourceRelative = output.SourceFile.Replace('\\', '/');
            var sourceDir = Path.GetDirectoryName(sourceRelative) ?? string.Empty;
            var sourceName = Path.GetFileName(sourceRelative);
            var outputFileName = output.UseSuffixOutput ? $"{sourceName}_realism_patch.json" : $"{sourceName}.json";
            var outputFile = Path.Combine(outputPath, sourceDir, outputFileName);
            var alternateOutputFile = Path.Combine(outputPath, sourceDir, output.UseSuffixOutput ? $"{sourceName}.json" : $"{sourceName}_realism_patch.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

            if (!string.Equals(alternateOutputFile, outputFile, StringComparison.OrdinalIgnoreCase)
                && File.Exists(alternateOutputFile))
            {
                File.Delete(alternateOutputFile);
            }

            using var stream = File.Create(outputFile);
            using var writer = new Utf8JsonWriter(stream, OutputWriterOptions);
            writer.WriteStartObject();
            foreach (var entry in output.Entries)
            {
                writer.WritePropertyName(entry.Key);
                entry.Value.WriteTo(writer, OutputJsonOptions);
            }

            writer.WriteEndObject();
            writer.Flush();

            log($"已导出: {Path.GetRelativePath(outputPath, outputFile)}");
        }
    }
}

internal sealed class PatchOutputBuffer
{
    private readonly Dictionary<string, OrderedPatchGroup> fileBasedPatches = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> fileBasedPatchOrder = [];
    private readonly Dictionary<string, bool> fileUsesSuffixOutput = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterSource(string sourceFile, bool useSuffixOutput)
    {
        fileUsesSuffixOutput[sourceFile] = useSuffixOutput;
    }

    public void AddOrUpdate(string sourceFile, string itemId, JsonObject patch)
    {
        if (!fileBasedPatches.TryGetValue(sourceFile, out var group))
        {
            group = new OrderedPatchGroup();
            fileBasedPatches[sourceFile] = group;
            fileBasedPatchOrder.Add(sourceFile);
        }

        group.AddOrUpdate(itemId, patch);
    }

    public List<FilePatchOutput> CreateOutputs()
    {
        var outputs = new List<FilePatchOutput>(fileBasedPatchOrder.Count);
        foreach (var sourceFile in fileBasedPatchOrder)
        {
            var group = fileBasedPatches[sourceFile];
            outputs.Add(new FilePatchOutput(
                sourceFile,
                fileUsesSuffixOutput.GetValueOrDefault(sourceFile, true),
                group.Entries.ToList()));
        }

        return outputs;
    }

    private sealed class OrderedPatchGroup
    {
        private readonly Dictionary<string, JsonObject> items = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> itemOrder = [];

        public IEnumerable<KeyValuePair<string, JsonObject>> Entries
        {
            get
            {
                foreach (var itemId in itemOrder)
                {
                    yield return new KeyValuePair<string, JsonObject>(itemId, items[itemId]);
                }
            }
        }

        public void AddOrUpdate(string itemId, JsonObject patch)
        {
            if (!items.ContainsKey(itemId))
            {
                itemOrder.Add(itemId);
            }

            items[itemId] = patch;
        }
    }
}

internal sealed record FilePatchOutput(string SourceFile, bool UseSuffixOutput, IReadOnlyList<KeyValuePair<string, JsonObject>> Entries);