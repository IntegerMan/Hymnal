using Hymnal.Core.Common;

namespace Hymnal.Core.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Ok_IsSuccess_True()
    {
        var result = Result<int>.Ok(42);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Ok_Value_ReturnsValue()
    {
        var result = Result<int>.Ok(42);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Fail_IsSuccess_False()
    {
        var result = Result<int>.Fail("err");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Fail_Error_ReturnsMessage()
    {
        var result = Result<int>.Fail("err");
        Assert.Equal("err", result.Error);
    }
}
