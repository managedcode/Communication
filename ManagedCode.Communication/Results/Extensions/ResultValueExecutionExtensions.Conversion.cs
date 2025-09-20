namespace ManagedCode.Communication.Results.Extensions;

public static partial class ResultValueExecutionExtensions
{
    public static Result ToResult<T>(this IResult<T> result)
    {
        return result.IsSuccess ? Result.Succeed() : Result.Fail(result.Problem ?? Problem.GenericError());
    }
}
