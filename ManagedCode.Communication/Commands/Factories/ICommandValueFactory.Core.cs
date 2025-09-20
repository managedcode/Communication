using System;

namespace ManagedCode.Communication.Commands;

public partial interface ICommandValueFactory<TSelf, TValue>
    where TSelf : class, ICommandValueFactory<TSelf, TValue>
{
    static abstract TSelf Create(Guid commandId, string commandType, TValue value);
}
