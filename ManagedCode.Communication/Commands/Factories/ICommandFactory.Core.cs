using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandFactory<TSelf>
    where TSelf : class, ICommandFactory<TSelf>
{
    static abstract TSelf Create(Guid commandId, string commandType);
}
