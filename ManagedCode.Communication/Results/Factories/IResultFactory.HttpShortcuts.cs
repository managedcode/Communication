using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    /// <summary>
    ///     Creates a 400 Bad Request failure.
    /// </summary>
    static virtual TSelf FailBadRequest(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    /// <summary>
    ///     Creates a 401 Unauthorized failure.
    /// </summary>
    static virtual TSelf FailUnauthorized(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    /// <summary>
    ///     Creates a 403 Forbidden failure.
    /// </summary>
    static virtual TSelf FailForbidden(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    /// <summary>
    ///     Creates a 404 Not Found failure.
    /// </summary>
    static virtual TSelf FailNotFound(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }
}
