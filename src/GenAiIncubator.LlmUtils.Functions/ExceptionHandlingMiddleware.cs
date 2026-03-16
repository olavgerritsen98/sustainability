using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;      
using Microsoft.Azure.Functions.Worker.Http;             

namespace GenAiIncubator.LlmUtils_Functions;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var httpReq = await context.GetHttpRequestDataAsync();
            Console.WriteLine($"HEREHERERE Exception: {ex.Message}");
            if (httpReq is null)
                throw;

            var resp = httpReq.CreateResponse(HttpStatusCode.InternalServerError);
            await resp.WriteAsJsonAsync(new ErrorResponse {
                ErrorMessage = "An internal error occurred processing your request. " +
                               $"Error: {ex.Message}"
            });

            context.GetInvocationResult().Value = resp;
        }
    }
}