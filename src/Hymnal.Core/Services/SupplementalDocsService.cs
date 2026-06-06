using Hymnal.Core.Common;
using Hymnal.Core.Interfaces;
using Hymnal.Core.Models;

namespace Hymnal.Core.Services;

/// <summary>
/// File-backed supplemental documents service rooted at .hymnal-data/docs/ within a workspace.
/// </summary>
public sealed class SupplementalDocsService : ISupplementalDocsService
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    private readonly IMetadataStore _metadataStore;

    public SupplementalDocsService(IMetadataStore metadataStore)
    {
        _metadataStore = metadataStore;
    }

    public Task<Result<IReadOnlyList<SupplementalDocNode>>> LoadTreeAsync(string workspaceRoot)
    {
        try
        {
            var docsRootResult = EnsureDocsRoot(workspaceRoot);
            if (!docsRootResult.IsSuccess)
                return Task.FromResult(Result<IReadOnlyList<SupplementalDocNode>>.Fail(docsRootResult.Error!));

            var docsRoot = docsRootResult.Value!;
            var root = new DirectoryInfo(docsRoot);
            IReadOnlyList<SupplementalDocNode> nodes = LoadChildren(root, docsRoot);
            return Task.FromResult(Result<IReadOnlyList<SupplementalDocNode>>.Ok(nodes));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<SupplementalDocNode>>.Fail($"Failed to load supplemental docs: {ex.Message}"));
        }
    }

    public Task<Result<SupplementalDocNode>> CreateFolderAsync(string workspaceRoot, string? parentRelativePath, string folderName)
    {
        try
        {
            var docsRootResult = EnsureDocsRoot(workspaceRoot);
            if (!docsRootResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(docsRootResult.Error!));

            var parentResult = ResolveParentDirectory(docsRootResult.Value!, parentRelativePath);
            if (!parentResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(parentResult.Error!));

            var nameResult = ValidateLeafName(folderName, "Folder name");
            if (!nameResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(nameResult.Error!));

            var absolutePathResult = ResolveChildPath(docsRootResult.Value!, parentResult.Value!, nameResult.Value!);
            if (!absolutePathResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(absolutePathResult.Error!));

            var absolutePath = absolutePathResult.Value!;
            if (File.Exists(absolutePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail($"Cannot create supplemental docs folder '{absolutePath}' because a file already exists at that path."));

            Directory.CreateDirectory(parentResult.Value!);
            Directory.CreateDirectory(absolutePath);

            return Task.FromResult(Result<SupplementalDocNode>.Ok(ProjectNode(docsRootResult.Value!, absolutePath, SupplementalDocNodeKind.Folder)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<SupplementalDocNode>.Fail($"Failed to create supplemental docs folder '{folderName}': {ex.Message}"));
        }
    }

    public Task<Result<SupplementalDocNode>> ImportFileAsync(string workspaceRoot, string? parentRelativePath, string sourceAbsolutePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceAbsolutePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail("Source file path is required."));

            var sourcePath = Path.GetFullPath(sourceAbsolutePath);
            if (!File.Exists(sourcePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail($"Source file '{sourcePath}' was not found."));

            var docsRootResult = EnsureDocsRoot(workspaceRoot);
            if (!docsRootResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(docsRootResult.Error!));

            if (IsUnderDocsRoot(docsRootResult.Value!, sourcePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail("Cannot import a file that is already inside the supplemental docs folder."));

            var parentResult = ResolveParentDirectory(docsRootResult.Value!, parentRelativePath);
            if (!parentResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(parentResult.Error!));

            var fileName = Path.GetFileName(sourcePath);
            var nameResult = ValidateLeafName(fileName, "File name");
            if (!nameResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(nameResult.Error!));

            var absolutePathResult = ResolveChildPath(docsRootResult.Value!, parentResult.Value!, nameResult.Value!);
            if (!absolutePathResult.IsSuccess)
                return Task.FromResult(Result<SupplementalDocNode>.Fail(absolutePathResult.Error!));

            var absolutePath = absolutePathResult.Value!;
            if (Directory.Exists(absolutePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail($"Cannot import supplemental docs file '{absolutePath}' because a folder already exists at that path."));

            if (File.Exists(absolutePath))
                return Task.FromResult(Result<SupplementalDocNode>.Fail($"Supplemental docs file '{absolutePath}' already exists."));

            Directory.CreateDirectory(parentResult.Value!);
            File.Copy(sourcePath, absolutePath, overwrite: false);

            return Task.FromResult(Result<SupplementalDocNode>.Ok(ProjectNode(docsRootResult.Value!, absolutePath, SupplementalDocNodeKind.File)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<SupplementalDocNode>.Fail($"Failed to import supplemental docs file '{sourceAbsolutePath}': {ex.Message}"));
        }
    }

    public async Task<Result<SupplementalDocNode>> CreateFileAsync(string workspaceRoot, string? parentRelativePath, string fileName, string initialContent = "")
    {
        try
        {
            var docsRootResult = EnsureDocsRoot(workspaceRoot);
            if (!docsRootResult.IsSuccess)
                return Result<SupplementalDocNode>.Fail(docsRootResult.Error!);

            var parentResult = ResolveParentDirectory(docsRootResult.Value!, parentRelativePath);
            if (!parentResult.IsSuccess)
                return Result<SupplementalDocNode>.Fail(parentResult.Error!);

            var nameResult = ValidateLeafName(fileName, "File name");
            if (!nameResult.IsSuccess)
                return Result<SupplementalDocNode>.Fail(nameResult.Error!);

            var absolutePathResult = ResolveChildPath(docsRootResult.Value!, parentResult.Value!, nameResult.Value!);
            if (!absolutePathResult.IsSuccess)
                return Result<SupplementalDocNode>.Fail(absolutePathResult.Error!);

            var absolutePath = absolutePathResult.Value!;
            if (Directory.Exists(absolutePath))
                return Result<SupplementalDocNode>.Fail($"Cannot create supplemental docs file '{absolutePath}' because a folder already exists at that path.");

            if (File.Exists(absolutePath))
                return Result<SupplementalDocNode>.Fail($"Supplemental docs file '{absolutePath}' already exists.");

            Directory.CreateDirectory(parentResult.Value!);
            await _metadataStore.WriteTextAtomicAsync(absolutePath, initialContent).ConfigureAwait(false);

            return Result<SupplementalDocNode>.Ok(ProjectNode(docsRootResult.Value!, absolutePath, SupplementalDocNodeKind.File));
        }
        catch (Exception ex)
        {
            return Result<SupplementalDocNode>.Fail($"Failed to create supplemental docs file '{fileName}': {ex.Message}");
        }
    }

    private static Result<string> EnsureDocsRoot(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return Result<string>.Fail("Workspace root is required to load supplemental docs.");

        try
        {
            var docsRoot = Path.GetFullPath(ISupplementalDocsService.DeriveDocsRoot(workspaceRoot));
            Directory.CreateDirectory(docsRoot);
            return Result<string>.Ok(docsRoot);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to create supplemental docs root for workspace '{workspaceRoot}': {ex.Message}");
        }
    }

    private static Result<string> ResolveParentDirectory(string docsRoot, string? parentRelativePath)
    {
        if (string.IsNullOrWhiteSpace(parentRelativePath))
            return Result<string>.Ok(docsRoot);

        var parent = NormalizeRelativePath(parentRelativePath, "Parent path");
        if (!parent.IsSuccess)
            return Result<string>.Fail(parent.Error!);

        var absoluteParent = Path.GetFullPath(Path.Combine(docsRoot, parent.Value!.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderDocsRoot(docsRoot, absoluteParent))
            return Result<string>.Fail($"Parent path '{parent.Value}' resolves outside the supplemental docs root.");

        if (File.Exists(absoluteParent))
            return Result<string>.Fail($"Parent path '{parent.Value}' is a file, not a folder.");

        return Result<string>.Ok(absoluteParent);
    }

    private static Result<string> ValidateLeafName(string name, string label)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Fail($"{label} is required.");

        var trimmed = name.Trim();
        if (trimmed is "." or "..")
            return Result<string>.Fail($"{label} '{name}' is not allowed.");

        if (Path.IsPathRooted(trimmed) || trimmed.Contains('/') || trimmed.Contains('\\'))
            return Result<string>.Fail($"{label} '{name}' must be a name, not a path.");

        if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Result<string>.Fail($"{label} '{name}' contains invalid file name characters.");

        return Result<string>.Ok(trimmed);
    }

    private static Result<string> NormalizeRelativePath(string relativePath, string label)
    {
        var trimmed = relativePath.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result<string>.Fail($"{label} is required.");

        var normalized = trimmed.Replace('\\', '/');
        if (Path.IsPathRooted(trimmed))
            return Result<string>.Fail($"{label} '{relativePath}' must be relative to the supplemental docs root.");

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return Result<string>.Fail($"{label} is required.");

        if (segments.Any(segment => segment is "." or ".."))
            return Result<string>.Fail($"{label} '{relativePath}' must not contain path traversal segments.");

        if (segments.Any(segment => segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            return Result<string>.Fail($"{label} '{relativePath}' contains invalid path characters.");

        return Result<string>.Ok(string.Join('/', segments));
    }

    private static Result<string> ResolveChildPath(string docsRoot, string parentAbsolutePath, string name)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(parentAbsolutePath, name));
        if (!IsUnderDocsRoot(docsRoot, absolutePath))
            return Result<string>.Fail($"Supplemental docs path '{name}' resolves outside the supplemental docs root.");

        return Result<string>.Ok(absolutePath);
    }

    private static bool IsUnderDocsRoot(string docsRoot, string absolutePath)
    {
        var normalizedRoot = Path.GetFullPath(docsRoot);
        var normalizedPath = Path.GetFullPath(absolutePath);

        if (PathComparer.Equals(normalizedRoot, normalizedPath))
            return true;

        var rootWithSeparator = Path.EndsInDirectorySeparator(normalizedRoot)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<SupplementalDocNode> LoadChildren(DirectoryInfo directory, string docsRoot)
    {
        var folders = directory
            .EnumerateDirectories()
            .Where(info => !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Select(info => ProjectFolder(docsRoot, info))
            .ToList();

        var files = directory
            .EnumerateFiles()
            .Where(info => !info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
            .Select(info => ProjectNode(docsRoot, info.FullName, SupplementalDocNodeKind.File))
            .ToList();

        return folders.Concat(files).ToList();
    }

    private static SupplementalDocNode ProjectFolder(string docsRoot, DirectoryInfo folder)
        => ProjectNode(docsRoot, folder.FullName, SupplementalDocNodeKind.Folder, LoadChildren(folder, docsRoot));

    private static SupplementalDocNode ProjectNode(
        string docsRoot,
        string absolutePath,
        SupplementalDocNodeKind kind,
        IReadOnlyList<SupplementalDocNode>? children = null)
    {
        var relativePath = Path.GetRelativePath(docsRoot, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
        var displayName = Path.GetFileName(absolutePath);
        return new SupplementalDocNode(
            relativePath,
            displayName,
            relativePath,
            absolutePath,
            kind,
            children ?? Array.Empty<SupplementalDocNode>());
    }
}
