using System;
using System.IO;
using Xunit;

namespace Hymnal.Core.Tests.Views;

public sealed class ResearchViewSmokeTests
{
    [Fact]
    public void ResearchView_Axaml_WiresSharedDocsEditorAndAiChat()
    {
        var axaml = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/ResearchView.axaml"));
        Assert.Contains("x:Class=\"Hymnal.Views.ResearchView\"", axaml);
        Assert.Contains("DataContext=\"{Binding SupplementalDocsViewModel}\"", axaml);
        Assert.Contains("DataContext=\"{Binding EditorViewModel}\"", axaml);
        Assert.Contains("DataContext=\"{Binding AiChatViewModel}\"", axaml);
        Assert.Contains("AiChatView", axaml);
        Assert.Contains("Text=\"DOCS\"", axaml);

        var mainWindowAxaml = File.ReadAllText(FindRepositoryFile("src/Hymnal/Views/MainWindow.axaml"));
        Assert.Contains("SecondaryCentreContent", mainWindowAxaml);
        Assert.Contains("ContentControl", mainWindowAxaml);
        Assert.Contains("IsEditorVisible", mainWindowAxaml);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate repository file '{relativePath}'.");
    }
}
