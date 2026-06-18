namespace Hymnal.Core.Models;

/// <summary>
/// Schema-versioned list of intentionally excluded manuscript-relative markdown paths.
/// Paths are stored forward-slash normalized and compared case-insensitively by the service.
/// </summary>
public sealed record ExclusionManifest
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string[] ExcludedPaths { get; init; } = Array.Empty<string>();
}
