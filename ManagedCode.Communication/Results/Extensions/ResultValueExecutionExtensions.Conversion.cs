namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultValueExecutionExtensions
{
    /// <summary>
    ///     Discards the value and keeps only success or failure.
    /// </summary>
    public static Result ToResult<T>(this IResult<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : Result.Fail(result.Problem ?? Problem.GenericError());
    }
}
