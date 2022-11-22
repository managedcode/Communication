using System.Net.Http;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Extensions;

public static class HttpResponseExtension
{
    
#if NET6_0_OR_GREATER

    public static async Task<Result<T>> FromJsonToResult<T>(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return System.Text.Json.JsonSerializer.Deserialize<Result<T>>(await responseMessage.Content.ReadAsStreamAsync());
        }
        
        return Result<T>.Fail(responseMessage.StatusCode);
    }
    
    public static Result FromJsonToResult(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return Result.Succeed(responseMessage.StatusCode);
        }
        
        return Result.Fail(responseMessage.StatusCode);
    }

#endif
   
}