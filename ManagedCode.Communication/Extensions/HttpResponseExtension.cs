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

        var content = await responseMessage.Content.ReadAsStringAsync();
        
        return Result<T>.Fail(new Error(content, responseMessage.StatusCode));
    }
    
    public static async Task<Result> FromJsonToResult(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return Result.Succeed();
        }
        
        var content = await responseMessage.Content.ReadAsStringAsync();
        return Result.Fail(new Error(content, responseMessage.StatusCode));
    }

#endif
   
}