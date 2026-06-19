using Hymnal.Core.Models.Ai;
using Hymnal.Core.Services.Ai;
using Xunit;

namespace Hymnal.Core.Tests.Services.Ai;

public class RolePromptProviderTests
{
    private readonly RolePromptProvider _sut = new();

    [Theory]
    [InlineData(AiRole.WritingCoach)]
    [InlineData(AiRole.DevelopmentalEditor)]
    [InlineData(AiRole.Proofreader)]
    [InlineData(AiRole.LineEditor)]
    [InlineData(AiRole.BetaReader)]
    public void GetSystemPrompt_EachRole_ContainsCommonPreambleAndRoleBody(AiRole role)
    {
        var prompt = _sut.GetSystemPrompt(role);

        Assert.False(string.IsNullOrWhiteSpace(prompt));
        // Common preamble sentinel
        Assert.Contains("Hymnal", prompt);
        // Role-specific body is present (they're non-empty resources)
        Assert.True(prompt.Length > 50);
    }

    [Fact]
    public void GetSystemPrompt_DevelopmentalEditor_ContainsDevelopmentalBody()
    {
        var prompt = _sut.GetSystemPrompt(AiRole.DevelopmentalEditor);
        Assert.Contains("developmental", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetSystemPrompt_ContainsPreambleSeparatedFromBody()
    {
        var prompt = _sut.GetSystemPrompt(AiRole.WritingCoach);
        // Preamble and body joined with double newline
        Assert.Contains("\n\n", prompt);
    }

    [Theory]
    [InlineData("Write")]
    [InlineData("Plan")]
    [InlineData("Manage")]
    public void GetAvailableRoles_KnownViews_ReturnsNonEmptyList(string view)
    {
        var roles = _sut.GetAvailableRoles(view);
        Assert.NotEmpty(roles);
    }

    [Fact]
    public void GetAvailableRoles_Write_IncludesWritingCoach()
    {
        var roles = _sut.GetAvailableRoles("Write");
        Assert.Contains(AiRole.WritingCoach, roles);
    }

    [Fact]
    public void GetAvailableRoles_Plan_IncludesDevelopmentalEditor()
    {
        var roles = _sut.GetAvailableRoles("Plan");
        Assert.Contains(AiRole.DevelopmentalEditor, roles);
    }

    [Fact]
    public void GetDefaultRole_Write_ReturnsWritingCoach()
    {
        Assert.Equal(AiRole.WritingCoach, _sut.GetDefaultRole("Write"));
    }

    [Fact]
    public void GetDefaultRole_Plan_ReturnsDevelopmentalEditor()
    {
        Assert.Equal(AiRole.DevelopmentalEditor, _sut.GetDefaultRole("Plan"));
    }

    [Fact]
    public void GetAvailableRoles_UnknownView_ReturnsAllRoles()
    {
        var roles = _sut.GetAvailableRoles("SomeUnknownView");
        Assert.Equal(5, roles.Count);
    }
}
