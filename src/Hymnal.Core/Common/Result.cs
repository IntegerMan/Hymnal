namespace Hymnal.Core.Common;

public readonly record struct Result<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess { get; init; }

    public static Result<T> Ok(T value) => new() { Value = value, IsSuccess = true };
    public static Result<T> Fail(string error) => new() { Error = error, IsSuccess = false };
}
