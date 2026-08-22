using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication;

public partial class Problem
{
    /// <summary>
    ///     Creates a 400 failure for a required value that was null.
    /// </summary>
    public static Problem Null(string? detail = null)
    {
        return Primitive(
            ProblemConstants.Titles.NullValue,
            detail ?? ProblemConstants.Messages.NullValue,
            HttpStatusCode.BadRequest,
            ProblemConstants.ErrorCodes.Null);
    }

    /// <summary>
    ///     Creates a 400 failure for an invalid argument.
    /// </summary>
    public static Problem Argument(string? detail = null)
    {
        return Primitive(
            ProblemConstants.Titles.InvalidArgument,
            detail ?? ProblemConstants.Messages.InvalidArgument,
            HttpStatusCode.BadRequest,
            ProblemConstants.ErrorCodes.InvalidArgument);
    }

    /// <summary>
    ///     Creates a 400 failure for an argument outside its allowed range.
    /// </summary>
    public static Problem OutOfRange(string? detail = null)
    {
        return Primitive(
            ProblemConstants.Titles.ArgumentOutOfRange,
            detail ?? ProblemConstants.Messages.ArgumentOutOfRange,
            HttpStatusCode.BadRequest,
            ProblemConstants.ErrorCodes.ArgumentOutOfRange);
    }

    /// <summary>
    ///     Creates a 409 failure for an operation that conflicts with the current state.
    /// </summary>
    public static Problem InvalidState(string? detail = null)
    {
        return Primitive(
            ProblemConstants.Titles.InvalidState,
            detail ?? ProblemConstants.Messages.InvalidState,
            HttpStatusCode.Conflict,
            ProblemConstants.ErrorCodes.InvalidState);
    }

    private static Problem Primitive(string title, string detail, HttpStatusCode statusCode, string errorCode)
    {
        var problem = Create(title, detail, statusCode);
        problem.ErrorCode = errorCode;
        return problem;
    }
}
