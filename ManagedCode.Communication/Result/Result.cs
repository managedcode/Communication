using System;
using System.Collections.Generic;

namespace ManagedCode.Communication;

public sealed partial class Result : BaseResult<ErrorCode>
{
    public Result(bool isSuccess, List<Error<ErrorCode>> errors) : base(isSuccess, errors)
    {
    }

    internal Result(bool isSuccess) : base(isSuccess)
    {
    }

    internal Result(Error<ErrorCode> error) : base(error)
    {
    }

    internal Result(List<Error<ErrorCode>> errors) : base(errors)
    {
    }

    public static implicit operator Result(Error<ErrorCode> error)
    {
        return new Result(error);
    }

    public static implicit operator Result(List<Error<ErrorCode>> errors)
    {
        return new Result(errors);
    }

    public static implicit operator Result(Exception? exception)
    {
        return new Result(Error<ErrorCode>.FromException(exception));
    }

    public static Result Succeed()
    {
        return new Result(true);
    }

    public static Result Fail()
    {
        return new Result(false);
    }

    public static Result Fail(Error<ErrorCode> error)
    {
        return new Result(error);
    }

    public static Result Fail(List<Error<ErrorCode>> errors)
    {
        return new Result(errors);
    }

    public static Result Fail(Exception? exception)
    {
        return new Result(Error<ErrorCode>.FromException(exception));
    }
}