using System.Text.Json;
using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

public sealed class ExclusionManifestService : IExclusionManifestService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IMetadataStore _metadataStore;

    public ExclusionManifestService(IMetadataStore metadataStore)
    {
        _metadataStore = metadataStore;
    }

    public async Task<Result<ExclusionManifest>> LoadAsync(string workspaceRoot)
    {
        var manifestPath = ManifestPath(workspaceRoot);
        if (!File.Exists(manifestPath))
            return Result<ExclusionManifest>.Ok(new ExclusionManifest());

        ExclusionManifest? manifest;
        try
        {
            var json = await File.ReadAllTextAsync(manifestPath).ConfigureAwait(false);
            manifest = JsonSerializer.Deserialize<ExclusionManifest>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            return Result<ExclusionManifest>.Fail(
                $"Manifest load failed for {manifestPath} during JSON read/parse: {ex.Message}");
        }

        if (manifest is null)
        {
            return Result<ExclusionManifest>.Fail(
                $"Manifest load failed for {manifestPath} during JSON parse: manifest deserialized to null.");
        }

        if (manifest.SchemaVersion != ExclusionManifest.CurrentSchemaVersion)
        {
            return Result<ExclusionManifest>.Fail(
                $"Manifest load failed for {manifestPath} during schema validation: unsupported schemaVersion {manifest.SchemaVersion}.");
        }

        var normalized = NormalizeAndValidateMany(workspaceRoot, manifest.ExcludedPaths, "load", pruneStale: true);
        if (!normalized.IsSuccess)
            return Result<ExclusionManifest>.Fail(normalized.Error!);

        return Result<ExclusionManifest>.Ok(new ExclusionManifest
        {
            SchemaVersion = ExclusionManifest.CurrentSchemaVersion,
            ExcludedPaths = normalized.Value!.ToArray()
        });
    }

    public async Task<Result<ExclusionManifest>> SaveAsync(string workspaceRoot, ExclusionManifest manifest)
    {
        var normalized = NormalizeAndValidateMany(workspaceRoot, manifest.ExcludedPaths, "save", pruneStale: true);
        if (!normalized.IsSuccess)
            return Result<ExclusionManifest>.Fail(normalized.Error!);

        var prunedManifest = new ExclusionManifest
        {
            SchemaVersion = ExclusionManifest.CurrentSchemaVersion,
            ExcludedPaths = normalized.Value!.ToArray()
        };

        var manifestPath = ManifestPath(workspaceRoot);
        var json = JsonSerializer.Serialize(prunedManifest, JsonOptions);

        try
        {
            await _metadataStore.WriteTextAtomicAsync(manifestPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<ExclusionManifest>.Fail(
                $"Manifest save failed for {manifestPath} during atomic write: {ex.Message}");
        }

        return Result<ExclusionManifest>.Ok(prunedManifest);
    }

    public async Task<Result<ExclusionManifest>> ExcludeAsync(string workspaceRoot, string relativePath)
    {
        var requested = NormalizeAndValidate(workspaceRoot, relativePath, "exclude");
        if (!requested.IsSuccess)
            return Result<ExclusionManifest>.Fail(requested.Error!);

        var loaded = await LoadAsync(workspaceRoot).ConfigureAwait(false);
        if (!loaded.IsSuccess)
            return loaded;

        var next = new HashSet<string>(loaded.Value!.ExcludedPaths, StringComparer.OrdinalIgnoreCase)
        {
            requested.Value!
        };

        return await SaveAsync(workspaceRoot, new ExclusionManifest { ExcludedPaths = next.ToArray() }).ConfigureAwait(false);
    }

    public async Task<Result<ExclusionManifest>> IncludeAsync(string workspaceRoot, string relativePath)
    {
        var requested = NormalizeAndValidate(workspaceRoot, relativePath, "include");
        if (!requested.IsSuccess)
            return Result<ExclusionManifest>.Fail(requested.Error!);

        var loaded = await LoadAsync(workspaceRoot).ConfigureAwait(false);
        if (!loaded.IsSuccess)
            return loaded;

        var next = loaded.Value!.ExcludedPaths
            .Where(path => !string.Equals(path, requested.Value!, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return await SaveAsync(workspaceRoot, new ExclusionManifest { ExcludedPaths = next }).ConfigureAwait(false);
    }

    private static Result<IReadOnlyList<string>> NormalizeAndValidateMany(
        string workspaceRoot,
        IEnumerable<string>? paths,
        string operation,
        bool pruneStale)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths ?? Array.Empty<string>())
        {
            var candidate = NormalizeAndValidate(workspaceRoot, path, operation);
            if (!candidate.IsSuccess)
                return Result<IReadOnlyList<string>>.Fail(candidate.Error!);

            if (pruneStale && !RelativeFileExists(workspaceRoot, candidate.Value!))
                continue;

            normalized.Add(candidate.Value!);
        }

        var ordered = normalized
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<IReadOnlyList<string>>.Ok(ordered);
    }

    private static Result<string> NormalizeAndValidate(string workspaceRoot, string path, string operation)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
        {
            return Result<string>.Fail(
                $"Manifest {operation} failed for workspace root during path validation: workspace root is required.");
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return Result<string>.Fail(
                $"Manifest {operation} failed for exclusion path during path validation: path is required.");
        }

        var normalized = path.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized)
            || normalized.StartsWith('/')
            || (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':'))
        {
            return Result<string>.Fail(
                $"Manifest {operation} failed for {normalized} during path validation: absolute paths are not allowed.");
        }

        var segments = normalized.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
        {
            return Result<string>.Fail(
                $"Manifest {operation} failed for {normalized} during path validation: traversal or empty path segments are not allowed.");
        }

        var workspaceFull = Path.GetFullPath(workspaceRoot);
        var absolute = Path.GetFullPath(ToAbsolutePath(workspaceFull, normalized));
        if (!absolute.StartsWith(workspaceFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail(
                $"Manifest {operation} failed for {normalized} during path validation: path escapes the workspace root.");
        }

        return Result<string>.Ok(normalized);
    }

    private static string ManifestPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot, ".hymnal-data", "exclusions.json");

    private static bool RelativeFileExists(string workspaceRoot, string normalizedRelativePath)
    {
        var exactPath = ToAbsolutePath(workspaceRoot, normalizedRelativePath);
        if (File.Exists(exactPath))
            return true;

        var current = workspaceRoot;
        foreach (var segment in normalizedRelativePath.Split('/'))
        {
            if (!Directory.Exists(current))
                return false;

            var next = Directory.EnumerateFileSystemEntries(current)
                .FirstOrDefault(entry => string.Equals(Path.GetFileName(entry), segment, StringComparison.OrdinalIgnoreCase));

            if (next is null)
                return false;

            current = next;
        }

        return File.Exists(current);
    }

    private static string ToAbsolutePath(string workspaceRoot, string normalizedRelativePath) =>
        Path.Combine(workspaceRoot, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
}
