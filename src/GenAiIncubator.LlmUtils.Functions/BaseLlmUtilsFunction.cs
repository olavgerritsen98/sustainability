using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace GenAiIncubator.LlmUtils_Functions;

/// <summary>
/// Azure Function that exposes functionality to classify an uploaded image
/// into one of several possible heater types using a recognition service.
/// </summary>
/// <remarks>
/// <para>
/// This abstract class encapsulates the common boilerplate for HTTP-triggered functions:
/// <list type="bullet">
///   <item><description>Detecting payload type (JSON vs multipart) and delegating accordingly.</description></item>
///   <item><description>Deserializing JSON into <typeparamref name="TReq" />, if the request is JSON.</description></item>
///   <item><description>Validating the JSON request via <see cref="Validate(TReq)" />, if applicable.</description></item>
///   <item><description>Invoking core JSON logic in <see cref="ExecuteRequestAsync(TReq,CancellationToken)" />.</description></item>
///   <item><description>Parsing multipart/form-data, extracting file bytes, and invoking
///   <see cref="ExecuteFileAsync(byte[],CancellationToken)" />.</description></item>
///   <item><description>Turning validation failures into 400 Bad Request responses, and
///   unhandled exceptions into 500 Internal Server Error responses.</description></item>
/// </list>
/// </para>
/// <para>
/// Subclasses should override one of:
/// <list type="bullet">
///   <item><description><see cref="ExecuteRequestAsync" /> for JSON-only inputs.</description></item>
///   <item><description><see cref="ExecuteFileAsync" /> for endpoints that process file uploads.</description></item>
/// </list>
/// </para>
/// <para>
/// Each concrete class is meant to implement a set of public functions, decorated with the 
/// Azure Functions <c>[Function]</c> and OpenAPI attributes to define the HTTP route,
/// schema, and documentation, while delegating plumbing back to this base.
/// </para>
/// </remarks>
public abstract class BaseLlmUtilsFunction<TReq, TRes>
    where TReq : class
    where TRes : class
{
    /// <summary>
    /// Override this for JSON-driven endpoints.
    /// </summary>
    protected virtual Task<TRes> ExecuteRequestAsync(TReq request, CancellationToken ct)
        => throw new NotSupportedException($"{GetType().Name} does not support JSON payloads.");

    /// <summary>
    /// For multipart flows: the **only** method your subclass must override.
    /// You get a raw byte[] for the first file, return your TRes (usually a DTO).
    /// </summary>
    protected virtual Task<TRes> ExecuteFileAsync(byte[] fileContents, string fileType, CancellationToken ct)
        => throw new NotSupportedException($"{GetType().Name} does not support file payloads.");

    /// <summary>
    /// Optional validation for JSON endpoints.
    /// </summary>
    protected virtual string? Validate(TReq request) => null;

    public virtual Task<HttpResponseData> RunAsync(
        HttpRequestData req,
        FunctionContext context,
        CancellationToken ct)
    {
        var functionName = context.FunctionDefinition.Name;
        var logger = context.GetLogger(functionName);
        logger.LogInformation("Processing {Operation} request.", functionName);

        if (IsMultipartContentType(req.Headers))
            return HandleMultipartAsync(req, context, ct);

        return HandleJsonAsync(req, context, ct);
    }

    private async Task<HttpResponseData> HandleJsonAsync(
        HttpRequestData req,
        FunctionContext context,
        CancellationToken ct)
    {
        var functionName = context.FunctionDefinition.Name;
        var logger = context.GetLogger(functionName);

        TReq? model;
        try
        {
            model = await req.ReadJsonAsync<TReq>();
            if (model is null) throw new JsonException("Empty or invalid JSON");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON for {Operation}.", functionName);
            return await req.CreateBadRequestResponseAsync("Invalid JSON format.");
        }

        var validationError = Validate(model!);
        if (validationError is not null)
            return await req.CreateBadRequestResponseAsync(validationError);

        try
        {
            var result = await ExecuteRequestAsync(model!, ct);
            return await req.WriteJsonAsync(result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Operation}.", functionName);
            return await req.CreateInternalServerErrorResponseAsync(
                $"An error occurred while {functionName}."
            );
        }
    }

    private async Task<HttpResponseData> HandleMultipartAsync(
        HttpRequestData req,
        FunctionContext context,
        CancellationToken ct)
    {
        var functionName = context.FunctionDefinition.Name;
        var logger = context.GetLogger(functionName);
        var boundary = GetBoundary(req.Headers);
        var reader = new MultipartReader(boundary, req.Body);

        try
        {
            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync(ct)) != null)
            {
                if (section.GetContentDispositionHeader()?.IsFileDisposition() == true)
                {
                    var contentDisposition = section.GetContentDispositionHeader();
                    var fileName = contentDisposition?.FileName.Value ?? contentDisposition?.FileNameStar.Value;
                    if (string.IsNullOrEmpty(fileName))
                        throw new InvalidDataException("File name is missing in the Content-Disposition header.");

                    var fileExtension = Path.GetExtension(fileName).TrimStart('.');

                    using var ms = new MemoryStream();
                    await section.Body.CopyToAsync(ms, ct);
                    var bytes = ms.ToArray();

                    var dto = await ExecuteFileAsync(bytes, fileExtension, ct); 
                    return await req.WriteJsonAsync(dto, HttpStatusCode.OK);
                }
            }
            throw new InvalidDataException("No file was provided in the request.");

        }
        catch (InvalidDataException bad)
        {
            logger.LogWarning(bad, "Validation error in {Operation}.", functionName);
            return await req.CreateBadRequestResponseAsync(bad.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Operation}.", functionName);
            return await req.CreateInternalServerErrorResponseAsync(
                $"An error occurred while {functionName}."
            );
        }
    }

    private static bool IsMultipartContentType(HttpHeadersCollection headers)
        => headers.TryGetValues("Content-Type", out var vs)
            && vs.FirstOrDefault()?.Contains("multipart/", StringComparison.OrdinalIgnoreCase) == true;

    private static string GetBoundary(HttpHeadersCollection headers)
    {
        if (!headers.TryGetValues("Content-Type", out var vs))
            throw new InvalidDataException("Missing Content-Type header.");
        var contentType = Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(vs.First());
        return HeaderUtilities.RemoveQuotes(contentType.Boundary).Value!
            ?? throw new InvalidDataException("Missing boundary in Content-Type.");
    }
}