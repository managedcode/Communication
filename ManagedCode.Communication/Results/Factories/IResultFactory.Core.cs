using ManagedCode.Communication.Results;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    static abstract TSelf Succeed();
    static abstract TSelf Fail(Problem problem);
}
