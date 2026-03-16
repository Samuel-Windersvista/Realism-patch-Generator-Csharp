using System.Text.Json.Nodes;
using RealismPatchGenerator.Core;
using Xunit;

namespace RealismPatchGenerator.Tests;

public sealed class ItemExceptionImportServiceTests : IDisposable
{
    private readonly string basePath;

    public ItemExceptionImportServiceTests()
    {
        basePath = Path.Combine(Path.GetTempPath(), "realism-exception-import-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "input", "weapons"));
        Directory.CreateDirectory(Path.Combine(basePath, "output", "weapons"));
    }

    [Fact]
    public void ImportFromOutput_ReadsPatchedFields()
    {
        var root = new JsonObject
        {
            ["item-1"] = new JsonObject
            {
                ["ItemID"] = "item-1",
                ["Name"] = "Output Item",
                ["Ergonomics"] = 25,
                ["RecoilAngle"] = 11,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "weapons", "test_realism_patch.json"),
            root.ToJsonString());

        var imported = ItemExceptionImportService.ImportFromOutput(
            Path.Combine(basePath, "output"),
            "item-1",
            "weapons/test.json");

        Assert.NotNull(imported);
        Assert.Equal("item-1", imported!.ItemId);
        Assert.Equal("Output Item", imported.Name);
        Assert.Equal("weapons/test_realism_patch.json", imported.SourceFile);
        Assert.Equal(25, imported.Fields["Ergonomics"]!.GetValue<int>());
        Assert.False(imported.Fields.ContainsKey("ItemID"));
    }

    [Fact]
    public void ImportFromInput_PrefersOverrideProperties()
    {
        var root = new JsonObject
        {
            ["item-2"] = new JsonObject
            {
                ["ItemID"] = "item-2",
                ["Name"] = "Input Item",
                ["overrideProperties"] = new JsonObject
                {
                    ["Ergonomics"] = 31,
                    ["Prefab"] = new JsonObject { ["path"] = "assets/test.bundle" },
                },
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "input", "weapons", "test.json"),
            root.ToJsonString());

        var imported = ItemExceptionImportService.ImportFromInput(basePath, "item-2", "weapons/test.json");

        Assert.NotNull(imported);
        Assert.Equal("Input Item", imported!.Name);
        Assert.Equal("weapons/test.json", imported.SourceFile);
        Assert.Equal(31, imported.Fields["Ergonomics"]!.GetValue<int>());
        Assert.Equal("assets/test.bundle", imported.Fields["Prefab"]!["path"]!.GetValue<string>());
    }

    [Fact]
    public void SearchFromOutputByName_ReturnsNameMatches()
    {
        var root = new JsonObject
        {
            ["item-3"] = new JsonObject
            {
                ["ItemID"] = "item-3",
                ["Name"] = "Artem Helmet Alpha",
                ["Ergonomics"] = 12,
            },
            ["item-4"] = new JsonObject
            {
                ["ItemID"] = "item-4",
                ["Name"] = "Other Item",
                ["Ergonomics"] = 4,
            },
        };

        File.WriteAllText(
            Path.Combine(basePath, "output", "weapons", "helmets_realism_patch.json"),
            root.ToJsonString());

        var results = ItemExceptionImportService.SearchFromOutputByName(
            Path.Combine(basePath, "output"),
            "helmet");

        var match = Assert.Single(results);
        Assert.Equal("item-3", match.ItemId);
        Assert.Equal("Artem Helmet Alpha", match.Name);
        Assert.Equal("weapons/helmets_realism_patch.json", match.SourceFile);
        Assert.Equal(12, match.Fields["Ergonomics"]!.GetValue<int>());
    }

    [Fact]
    public void SearchFromInputByName_ReturnsOverrideFieldsAndRelativePath()
    {
        var root = new JsonObject
        {
            ["item-5"] = new JsonObject
            {
                ["ItemID"] = "item-5",
                ["Name"] = "Searchable Rig",
                ["overrideProperties"] = new JsonObject
                {
                    ["ReloadSpeedMulti"] = 1.18,
                },
            },
        };

        Directory.CreateDirectory(Path.Combine(basePath, "input", "gear"));
        File.WriteAllText(
            Path.Combine(basePath, "input", "gear", "rig.json"),
            root.ToJsonString());

        var results = ItemExceptionImportService.SearchFromInputByName(basePath, "rig");

        var match = Assert.Single(results);
        Assert.Equal("item-5", match.ItemId);
        Assert.Equal("Searchable Rig", match.Name);
        Assert.Equal("gear/rig.json", match.SourceFile);
        Assert.Equal(1.18, match.Fields["ReloadSpeedMulti"]!.GetValue<double>(), 3);
    }

    public void Dispose()
    {
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }
    }
}
