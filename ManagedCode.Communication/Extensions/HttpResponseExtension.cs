using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ManagedCode.Communication.Extensions;

public static class HttpResponseExtension
{
    public static async Task<Result<T>> FromJsonToResult<T>(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<Result<T>>(await responseMessage.Content.ReadAsStreamAsync());
        }

        var content = await responseMessage.Content.ReadAsStringAsync();
        return Result<T>.Fail(content, content, responseMessage.StatusCode);
    }

    public static async Task<Result> FromRequestToResult(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return Result.Succeed();
        }

        var content = await responseMessage.Content.ReadAsStringAsync();
        return Result.Fail(content, content, responseMessage.StatusCode);
    }
}