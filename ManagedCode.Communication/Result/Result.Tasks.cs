using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManagedCode.Communication;

public partial struct Result
{
    public Task<Result> AsTask()
    {
        return Task.FromResult(this);
    }

#if NET6_0_OR_GREATER

    public ValueTask<Result> AsValueTask()
    {
        return ValueTask.FromResult(this);
    }

#endif
    
}