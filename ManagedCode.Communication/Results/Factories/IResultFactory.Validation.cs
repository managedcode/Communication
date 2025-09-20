namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    static virtual TSelf FailValidation(params (string field, string message)[] errors)
    {
        return TSelf.Fail(Problem.Validation(errors));
    }
}
