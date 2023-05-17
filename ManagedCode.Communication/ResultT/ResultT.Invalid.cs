using System.Collections.Generic;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public static Result<T> Invalid()
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { "message", nameof(Invalid) } });
    }

    public static Result<T> Invalid(string message)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { nameof(message), message } });
    }

    public static Result<T> Invalid(string key, string value)
    {
        return new Result<T>(false, default, default, new Dictionary<string, string> { { key, value } });
    }

    public static Result<T> Invalid(Dictionary<string, string> values)
    {
        return new Result<T>(false, default, default, values);
    }
}