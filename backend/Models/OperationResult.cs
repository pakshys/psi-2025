namespace backend.Models;

public class OperationResult<TValue>
    where TValue : class, new() 
{
    public bool IsSuccess { get; }
    public TValue Value { get; }
    public string? Error { get; }

    private OperationResult(bool isSuccess, TValue value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static OperationResult<TValue> FromInput<TInput>(
        TInput input,
        Func<TInput, TValue> mapper)
        where TInput : class 
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var value = mapper(input) ?? new TValue();
        return new OperationResult<TValue>(true, value, null);
    }

    public static OperationResult<TValue> Failure(string error)
    {
        return new OperationResult<TValue>(false, new TValue(), error);
    }
}
