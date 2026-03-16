using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using GenAiIncubator.LlmUtils.Core.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace GenAiIncubator.LlmUtils_Functions.Definitions
{
    public class SendComplianceEmailFunction
    {
        private readonly ILogger<SendComplianceEmailFunction> _logger;
        private readonly GraphOptions _graphOptions;

        public SendComplianceEmailFunction(
            ILogger<SendComplianceEmailFunction> logger,
            IOptions<GraphOptions> graphOptions)
        {
            _logger = logger;
            _graphOptions = graphOptions.Value;
        }

        [Function("SendComplianceEmail_Start")]
        [OpenApiOperation(
            operationId: "SendComplianceEmail_Start",
            Summary = "Sends a test compliance email to a specified user",
            Description = "Authenticates locally via Device Code and sends a non-compliant URL warning email via Microsoft Graph API.",
            Visibility = OpenApiVisibilityType.Important
        )]
        [OpenApiRequestBody(
            "application/json",
            typeof(SendEmailRequest),
            Description = "Provide the 'RecipientEmail' and the non-compliant 'Url'.",
            Required = true
        )]
        [OpenApiResponseWithBody(
            HttpStatusCode.OK,
            "application/json",
            typeof(SendEmailResponse),
            Description = "Email sent successfully."
        )]
        [OpenApiResponseWithBody(
            HttpStatusCode.BadRequest,
            "application/json",
            typeof(ErrorResponse),
            Description = "Bad request if the input JSON is invalid or missing required fields."
        )]
        [OpenApiResponseWithBody(
            HttpStatusCode.InternalServerError,
            "application/json",
            typeof(ErrorResponse),
            Description = "Server error if Graph API authentication or email sending fails."
        )]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "compliance-email/send")] HttpRequestData req,
            FunctionContext context,
            CancellationToken ct)
        {
            _logger.LogInformation("Processing request to send compliance email.");

            // 1. Parse the Request Body
            var model = await req.ReadFromJsonAsync<SendEmailRequest>();
            if (model is null)
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Invalid JSON format.");
            }

            if (string.IsNullOrWhiteSpace(model.RecipientEmail) || string.IsNullOrWhiteSpace(model.Url))
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest, "Both RecipientEmail and Url are required.");
            }

            try
            {
                // Build Graph client using application credentials from GraphOptions
                var credential = new ClientSecretCredential(
                    _graphOptions.TenantId,
                    _graphOptions.ClientId,
                    _graphOptions.ClientSecret);
                var graphClient = new GraphServiceClient(credential);

                // 4. Construct the Email Body
                string emailBody = $"Hello,\n\nThe following URL has been flagged as non-compliant and requires your attention:\n\n{model.Url}\n\nPlease review and fix this issue.";

                var requestBody = new SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = "Action Required: Non-Compliant URL Detected",
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Text,
                            Content = emailBody
                        },
                        ToRecipients = new List<Recipient>
                        {
                            new Recipient { EmailAddress = new EmailAddress { Address = model.RecipientEmail } }
                        }
                    },
                    SaveToSentItems = true
                };

                // 5. Send the Email
                _logger.LogInformation("Sending email to {RecipientEmail}...", model.RecipientEmail);
                await graphClient.Users[_graphOptions.SenderAddress].SendMail.PostAsync(requestBody, cancellationToken: ct);
                
                // 6. Return Success Response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new SendEmailResponse 
                { 
                    Message = "Email sent successfully!", 
                    SentTo = model.RecipientEmail 
                });
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email: {Message}", ex.Message);
                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError, $"Failed to send email: {ex.Message}");
            }
        }

        // Helper method to keep responses clean
        private async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string errorMessage)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(new ErrorResponse { ErrorMessage = errorMessage });
            return response;
        }
    }

    // --- Models for Swagger UI ---

    public class SendEmailRequest
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class SendEmailResponse
    {
        public string Message { get; set; } = string.Empty;
        public string SentTo { get; set; } = string.Empty;
    }


}