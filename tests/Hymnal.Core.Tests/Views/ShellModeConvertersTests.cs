using Hymnal.ViewModels;
using Hymnal.Views.Converters;

namespace Hymnal.Core.Tests.Views;

public class ShellModeConvertersTests
{
    [Theory]
    [InlineData(ShellMode.Write, "Write", true)]
    [InlineData(ShellMode.Manage, "Manage", true)]
    [InlineData(ShellMode.Plan, "Plan", true)]
    [InlineData(ShellMode.Write, "Manage", false)]
    [InlineData(ShellMode.Manage, "Write", false)]
    [InlineData(ShellMode.Plan, "Write", false)]
    public void ShellModeEqualsConverter_ReportsMatchCorrectly(ShellMode mode, string parameter, bool expected)
    {
        var converter = new ShellModeEqualsConverter();

        var result = converter.Convert(mode, typeof(bool), parameter, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }
}
