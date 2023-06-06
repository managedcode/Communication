using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
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
            Type type;
            if (context.InterfaceMethod.ReturnType.IsAssignableFrom(typeof(IResult)))
                type = typeof(Result);
            else
                type = context.InterfaceMethod.ReturnType.IsGenericType
                    ? context.InterfaceMethod.ReturnType.GetGenericArguments()[0]
                    : context.InterfaceMethod.ReturnType;


            var resultType = Activator.CreateInstance(type, BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { exception }, CultureInfo.CurrentCulture);

            context.Result = resultType;
        }
    }
}