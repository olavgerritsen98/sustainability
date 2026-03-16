using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace GenAiIncubator.LlmUtils_Functions;

internal static class HttpRequestDataExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadJsonAsync<T>(this HttpRequestData req)
    {
        try
        {
            using var sr = new StreamReader(req.Body);
            var body = await sr.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(body, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static async Task<HttpResponseData> WriteJsonAsync<T>(
        this HttpRequestData req,
        T payload,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var resp = req.CreateResponse(status);
        await resp.WriteAsJsonAsync(payload);
        return resp;
    }

    public static async Task<HttpResponseData> CreateBadRequestResponseAsync(this HttpRequestData req, string errorMessage)
    {
        return await req.WriteJsonAsync(
            new ErrorResponse { ErrorMessage = errorMessage },
            HttpStatusCode.BadRequest
        );
    }

    public static async Task<HttpResponseData> CreateOkResponseAsync<T>(this HttpRequestData req, T body)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        
        await response.WriteStringAsync(JsonSerializer.Serialize(body));
        return response;
    }

    public static async Task<HttpResponseData> CreateInternalServerErrorResponseAsync(this HttpRequestData req, string errorMessage)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        var error = new ErrorResponse { ErrorMessage = errorMessage };
        
        await response.WriteStringAsync(JsonSerializer.Serialize(error));
        return response;
    }
}