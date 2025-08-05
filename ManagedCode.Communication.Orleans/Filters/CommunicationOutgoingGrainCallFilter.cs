using System;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ManagedCode.Communication.Helpers;
using Orleans;

namespace ManagedCode.Communication.Filters;

public class CommunicationOutgoingGrainCallFilter : IOutgoingGrainCallFilter
{
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        try
        {
            await context.Invoke();
        }
        catch (Exception exception)
        {
            var returnType = context.InterfaceMethod.ReturnType;

            if (returnType.IsGenericType)
            {
                var genericDef = returnType.GetGenericTypeDefinition();
                if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
                {
                    var taskResultType = returnType.GenericTypeArguments[0];

                    if (typeof(IResult).IsAssignableFrom(taskResultType))
                    {
                        var statusCode = GetOrleansStatusCode(exception);

                        if (taskResultType == typeof(Result))
                        {
                            context.Result = Result.Fail(exception, statusCode);
                        }
                        else
                        {
                            // Result<T> - use Activator with internal constructor
                            var resultInstance = Activator.CreateInstance(taskResultType, BindingFlags.NonPublic | BindingFlags.Instance, null,
                                [exception], CultureInfo.CurrentCulture);
                            context.Result = resultInstance;
                        }

                        return;
                    }
                }
            }

            throw;
        }
    }

    private static HttpStatusCode GetOrleansStatusCode(Exception exception)
    {
        return OrleansHttpStatusCodeHelper.GetStatusCodeForException(exception);
    }
}