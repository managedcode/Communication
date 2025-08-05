using System;

namespace ManagedCode.Communication;

public partial struct Result
{
    /// <summary>
    /// Creates an exception from the result's problem.
    /// </summary>
    /// <returns>ProblemException if result has a problem, null otherwise.</returns>
    public Exception? ToException()
    {
        return Problem != null ? new ProblemException(Problem) : null;
    }
    
    /// <summary>
    /// Throws a ProblemException if the result has a problem.
    /// </summary>
    public void ThrowIfProblem()
    {
        if (Problem != null)
            throw new ProblemException(Problem);
    }
}