using System.Net;
using ManagedCode.Communication.Constants;

namespace ManagedCode.Communication.Results;

public partial interface IResultFactory<TSelf>
    where TSelf : struct, IResultFactory<TSelf>
{
    static virtual TSelf FailBadRequest(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.BadRequest,
            detail ?? ProblemConstants.Messages.BadRequest,
            (int)HttpStatusCode.BadRequest));
    }

    static virtual TSelf FailUnauthorized(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Unauthorized,
            detail ?? ProblemConstants.Messages.UnauthorizedAccess,
            (int)HttpStatusCode.Unauthorized));
    }

    static virtual TSelf FailForbidden(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.Forbidden,
            detail ?? ProblemConstants.Messages.ForbiddenAccess,
            (int)HttpStatusCode.Forbidden));
    }

    static virtual TSelf FailNotFound(string? detail = null)
    {
        return TSelf.Fail(Problem.Create(
            ProblemConstants.Titles.NotFound,
            detail ?? ProblemConstants.Messages.ResourceNotFound,
            (int)HttpStatusCode.NotFound));
    }
}
