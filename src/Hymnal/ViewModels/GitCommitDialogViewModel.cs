using System;
using System.Collections.Generic;
using System.Linq;
using Hymnal.Core.Models;
using Hymnal.Core.Services;

namespace Hymnal.ViewModels;

public sealed class GitCommitDialogViewModel
{
    public GitCommitDialogViewModel(string initialMessage, IReadOnlyList<GitChangedFile>? changedFiles)
    {
        InitialMessage = initialMessage;
        ChangedFileLabels = (changedFiles ?? Array.Empty<GitChangedFile>())
            .Select(GitChangedFileDisplay.FormatLabel)
            .ToList();
    }

    public string InitialMessage { get; }

    public IReadOnlyList<string> ChangedFileLabels { get; }

    public bool HasChangedFiles => ChangedFileLabels.Count > 0;
}
