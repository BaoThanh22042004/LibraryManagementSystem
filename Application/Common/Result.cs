namespace Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Success result cannot have an error message");
        
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Failure result must have an error message");
        
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);

    public static Result Failure(string error) => new(false, error);
    public static Result<T> Failure<T>(string error) => new(default!, false, error);

    public static Result<T> Failure<T>(string message, T value)
    {
        return new Result<T>(value, false, message);
    }
}

public class Result<T> : Result
{
    private readonly T _value;
    
    public T Value 
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException("Cannot access Value of a failure result");
            
            return _value;
        }
    }

    protected internal Result(T value, bool isSuccess, string error) 
        : base(isSuccess, error)
    {
        _value = value;
    }
}