namespace ES2_SistemaPedidos.Api;

public sealed class Result<TSuccess>
{
    private Result(TSuccess? value, ValidationErrorResponse? error)
    {
        Value = value;
        Error = error;
    }

    private TSuccess? Value { get; }

    private ValidationErrorResponse? Error { get; }

    public static Result<TSuccess> Success(TSuccess value) => new(value, null);

    public static Result<TSuccess> ValidationFailed(ValidationErrorResponse error) => new(default, error);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<ValidationErrorResponse, TResult> onValidationFailed)
    {
        return Error is null ? onSuccess(Value!) : onValidationFailed(Error);
    }
}
