using System.IO;
using Hymnal.Core.Common;

namespace Hymnal.Core.Tests.Common;

public class PathHelperTests
{
    // (a) Same path with different casing — must match
    [Fact]
    public void IsSamePath_SamePathDifferentCasing_ReturnsTrue()
    {
        var dir = Path.GetTempPath().TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var a = dir + Path.DirectorySeparatorChar + "Workspace" + Path.DirectorySeparatorChar + "Chapter1.txt";
        var b = dir + Path.DirectorySeparatorChar + "workspace" + Path.DirectorySeparatorChar + "chapter1.txt";

        Assert.True(PathHelper.IsSamePath(a, b));
    }

    // (b) Same path using backslash vs forward slash — must match
    [Fact]
    public void IsSamePath_BackslashVsForwardSlash_ReturnsTrue()
    {
        var dir = Path.GetTempPath().TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var a = dir + '\\' + "Workspace" + '\\' + "Chapter1.txt";
        var b = dir + '/' + "Workspace" + '/' + "Chapter1.txt";

        Assert.True(PathHelper.IsSamePath(a, b));
    }

    // (c) Path with a redundant ".." segment — must match the direct form
    [Fact]
    public void IsSamePath_RedundantDotDotSegment_ReturnsTrue()
    {
        var dir = Path.GetTempPath().TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var a = dir + Path.DirectorySeparatorChar + "Workspace" + Path.DirectorySeparatorChar + "Chapter1.txt";
        var b = dir + Path.DirectorySeparatorChar + "Workspace" + Path.DirectorySeparatorChar
              + "sub" + Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar + "Chapter1.txt";

        Assert.True(PathHelper.IsSamePath(a, b));
    }

    // (d) Two genuinely different chapter paths — must not match
    [Fact]
    public void IsSamePath_GenuinelyDifferentPaths_ReturnsFalse()
    {
        var dir = Path.GetTempPath().TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var a = dir + Path.DirectorySeparatorChar + "Workspace" + Path.DirectorySeparatorChar + "Chapter1.txt";
        var b = dir + Path.DirectorySeparatorChar + "Workspace" + Path.DirectorySeparatorChar + "Chapter2.txt";

        Assert.False(PathHelper.IsSamePath(a, b));
    }

    // Extra: null/empty inputs — must return false without throwing
    [Theory]
    [InlineData(null, "/some/path")]
    [InlineData("/some/path", null)]
    [InlineData(null, null)]
    [InlineData("", "/some/path")]
    [InlineData("/some/path", "")]
    public void IsSamePath_NullOrEmptyInput_ReturnsFalse(string? a, string? b)
    {
        Assert.False(PathHelper.IsSamePath(a, b));
    }
}
