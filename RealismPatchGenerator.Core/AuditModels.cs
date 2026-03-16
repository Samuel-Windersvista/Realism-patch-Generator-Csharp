using System.Text.Json.Nodes;

namespace RealismPatchGenerator.Core;

public sealed class WarningBreakdown
{
    public Dictionary<string, int> ByGroup { get; init; } = [];
    public Dictionary<string, int> ByCategory { get; init; } = [];
}

public sealed class AuditWarningDetail
{
    public required string Group { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
}

public sealed class AuditViolation
{
    public required string Field { get; init; }
    public JsonNode? Value { get; init; }
    public JsonNode? Expected { get; init; }
    public double? ExpectedMin { get; init; }
    public double? ExpectedMax { get; init; }
    public required string Rule { get; init; }
    public required string Message { get; init; }
}

public sealed class AuditItemReport
{
    public required string ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public required string Status { get; init; }
    public List<string> Warnings { get; init; } = [];
    public List<AuditWarningDetail> WarningDetails { get; init; } = [];
    public List<AuditViolation> Violations { get; init; } = [];
    public JsonObject Context { get; init; } = [];
}

public sealed class AuditFileReport
{
    public required string File { get; init; }
    public required string SourceFile { get; init; }
    public int ItemCount { get; init; }
    public int FlaggedItemCount { get; init; }
    public int ViolationCount { get; init; }
    public int WarningCount { get; init; }
    public WarningBreakdown WarningBreakdown { get; init; } = new();
    public List<string> Warnings { get; init; } = [];
    public List<AuditItemReport> Items { get; init; } = [];
}

public sealed class AuditReport
{
    public required string OutputDir { get; init; }
    public required string ScanMode { get; init; }
    public int FileCount { get; init; }
    public int ItemCount { get; init; }
    public int ViolationCount { get; init; }
    public int WarningCount { get; init; }
    public WarningBreakdown WarningBreakdown { get; init; } = new();
    public List<AuditFileReport> Files { get; init; } = [];
}

public sealed class OutputAuditOptions
{
    public string? OutputDirectory { get; init; }
    public bool IncludeOk { get; init; }
    public bool IncludeTemplateExports { get; init; }
}