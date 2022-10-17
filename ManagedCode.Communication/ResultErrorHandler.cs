using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.Extensions;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication;

public class ResultErrorHandler
{
    private readonly ILogger<ResultErrorHandler>? _logger;

    public ResultErrorHandler(ILogger<ResultErrorHandler>? logger)
    {
        _logger = logger;
    }

    public async Task<Result> ExecuteAsync(Func<Task<Result>> func,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        try
        {
            var result = await func.Invoke();

            if (result.IsFail)
            {
                _logger?.LogError($"{memberName} {result.Error} in {sourceFilePath}:line {sourceLineNumber} ");
            }

            return result;
        }
        catch (Exception e)
        {
            _logger?.LogException(e);

            return Result.Fail(Error<ErrorCode>.FromException(e));
        }
    }

    public async Task<Result<TValue>> ExecuteAsync<TValue>(Func<Task<Result<TValue>>> func,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        try
        {
            var result = await func.Invoke();

            if (result.IsFail)
            {
                _logger?.LogError($"{memberName} {result.Error?.ErrorCode} {result.Error?.Message} in {sourceFilePath}:line {sourceLineNumber} ");
            }

            return result;
        }
        catch (Exception e)
        {
            _logger?.LogException(e);

            return Result<TValue>.Fail(Error<ErrorCode>.FromException(e));
        }
    }

    public async Task<TResult> ExecuteAsync<TResult, TErrorCode>(Func<Task<TResult>> func,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0) where TErrorCode : Enum where TResult : BaseResult<TErrorCode>
    {
        try
        {
            var result = await func.Invoke();

            if (result.IsFail)
            {
                _logger?.LogError(
                    $"{memberName} {result.Error?.ErrorCode?.ToString()} {result.Error?.Message} {result.Error} in {sourceFilePath}:line {sourceLineNumber} ");
            }

            return result;
        }
        catch (Exception e)
        {
            _logger?.LogException(e);

            var constructor = typeof(TResult).GetConstructor(new[] { typeof(Error<TErrorCode>) })!;

            var obj = constructor.Invoke(new object[] { Error<TErrorCode>.FromException(e) }) as TResult;

            return obj!;
        }
    }
}