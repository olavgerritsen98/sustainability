using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace GenAiIncubator.LlmUtils_Functions;

public static class FunctionRunner
{
    /// <summary>
    /// Provides a generic Azure Function boilerplate for handling HTTP requests.
    /// </summary>
    /// <typeparam name="TReq">The type of the request model.</typeparam>
    /// <typeparam name="TRes">The type of the response model.</typeparam>
    /// <param name="req">The HTTP request data.</param>
    /// <param name="context">The function execution context.</param>
    /// <param name="operationName">The name of the operation being executed.</param>
    /// <param name="handler">The business logic handler to process the request.</param>
    /// <param name="validator">
    /// An optional validator function that returns an error message if validation fails.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="HttpResponseData"/> representing the result of the operation:
    /// <list type="bullet">
    /// <item><description>Returns 400 Bad Request for invalid JSON or validation errors.</description></item>
    /// <item><description>Returns 200 OK with the serialized response model on success.</description></item>
    /// <item><description>Returns 500 Internal Server Error for unhandled exceptions.</description></item>
    /// </list>
    /// </returns>
    public static async Task<HttpResponseData> RunJson<TReq, TRes>(
        this HttpRequestData req,
        FunctionContext context,
        string operationName,
        Func<TReq, CancellationToken, Task<TRes>> handler,
        Func<TReq, string?>? validator = null,
        CancellationToken cancellationToken = default)
        where TReq : class
        where TRes : class
    {
        var logger = context.GetLogger(operationName);
        logger.LogInformation("Processing {Operation} request.", operationName);

        TReq? model;
        try
        {
            model = await req.ReadJsonAsync<TReq>();
            if (model is null) throw new JsonException("Body was empty or invalid");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON format for {Operation}.", operationName);
            return await req.CreateBadRequestResponseAsync("Invalid JSON format.");
        }

        if (validator is not null)
        {
            var error = validator(model);
            if (error is not null)
                return await req.CreateBadRequestResponseAsync(error);
        }

        try
        {
            var resultDto = await handler(model, cancellationToken);
            return await req.CreateOkResponseAsync(resultDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {Operation}.", operationName);
            return await req.CreateInternalServerErrorResponseAsync(
                $"An error occurred while {operationName.ToLowerInvariant()}."
            );
        }
    }

    public static async Task<HttpResponseData> RunMultipart<TRes>(
        this HttpRequestData req,
        FunctionContext context,
        string operationName,
        Func<MultipartReader, CancellationToken, Task<TRes>> handler,
        CancellationToken cancellationToken = default)
        where TRes : class
    {
        var logger = context.GetLogger(operationName);
        logger.LogInformation("Processing {Operation} request.", operationName);

        if (!IsMultipartContentType(req.Headers))
            return await req.CreateBadRequestResponseAsync(
                "Content-Type must be multipart/form-data.");

        var boundary = GetBoundary(req.Headers);
        var reader   = new MultipartReader(boundary, req.Body);

        try
        {
            var resultDto = await handler(reader, cancellationToken);
            return await req.WriteJsonAsync(resultDto, HttpStatusCode.OK);
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Validation failed in {Operation}.", operationName);
            return await req.CreateBadRequestResponseAsync(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {Operation}.", operationName);
            return await req.CreateInternalServerErrorResponseAsync(
                $"An error occurred while {operationName.ToLowerInvariant()}.");
        }
    }

    private static bool IsMultipartContentType(HttpHeadersCollection headers)
    {
        if (headers.TryGetValues("Content-Type", out var values))
        {
            var contentType = values.FirstOrDefault();
            return !string.IsNullOrEmpty(contentType)
                && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string GetBoundary(HttpHeadersCollection headers)
    {
        if (headers.TryGetValues("Content-Type", out var values))
        {
            var contentType = values.FirstOrDefault()!;
            var parsed      = MediaTypeHeaderValue.Parse(contentType);
            return HeaderUtilities.RemoveQuotes(parsed.Boundary).Value!;
        }
        throw new InvalidDataException("Missing Content-Type header with boundary.");
    }
}