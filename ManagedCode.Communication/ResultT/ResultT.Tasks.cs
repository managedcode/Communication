using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result<T>
{
    public Task<Result<T>> AsTask()
    {
        return Task.FromResult(this);
    }

#if NET6_0_OR_GREATER

    public ValueTask<Result<T>> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }

#endif
    
}