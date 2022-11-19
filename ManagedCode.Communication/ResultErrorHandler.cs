using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ManagedCode.Communication.ZALIPA.Extensions;
using ManagedCode.Communication.ZALIPA.Result;
using Microsoft.Extensions.Logging;

namespace ManagedCode.Communication.ZALIPA;

public sealed class ResultErrorHandler
{
    private readonly ILogger<ResultErrorHandler>? _logger;

    public ResultErrorHandler(ILogger<ResultErrorHandler>? logger)
    {
        _logger = logger;
    }

    public async Task<Result.Result> ExecuteAsync(Func<Task<Result.Result>> func,
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
        catch (Exception? e)
        {
            _logger?.LogException(e);

            return Result.Result.Fail(Error.FromException(e));
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
        catch (Exception? e)
        {
            _logger?.LogException(e);

            return Result<TValue>.Fail(Error.FromException(e));
        }
    }

    public async Task<TResult> ExecuteAsync<TResult, TErrorCode>(Func<Task<TResult>> func,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0) where TErrorCode : Enum where TResult : Result.Result
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
        catch (Exception? e)
        {
            _logger?.LogException(e);

            var constructor = typeof(TResult).GetConstructor(new[] { typeof(Error) })!;

            var obj = constructor.Invoke(new object[] { Error.FromException(e) }) as TResult;

            return obj!;
        }
    }
}