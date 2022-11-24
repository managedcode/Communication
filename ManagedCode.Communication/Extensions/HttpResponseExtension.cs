#if NET6_0_OR_GREATER
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

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

        return Result<T>.Fail(Error.Create(content, responseMessage.StatusCode));
    }

    public static async Task<Result> FromJsonToResult(this HttpResponseMessage responseMessage)
    {
        if (responseMessage.IsSuccessStatusCode)
        {
            return Result.Succeed();
        }

        var content = await responseMessage.Content.ReadAsStringAsync();
        return Result.Fail(Error.Create(content, responseMessage.StatusCode));
    }
}
#endif