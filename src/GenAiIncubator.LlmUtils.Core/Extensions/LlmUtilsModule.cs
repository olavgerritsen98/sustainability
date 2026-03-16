using System.Reflection;
using Azure;
using Azure.AI.TextAnalytics;
using GenAiIncubator.LlmUtils.Core.Services;
using GenAiIncubator.LlmUtils.Core.Options;
using GenAiIncubator.LlmUtils.Core.Services.ContractTextExtraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using GenAiIncubator.LlmUtils.Core.Services.UnwantedDataClassification;
using GenAiIncubator.LlmUtils.Core.Services.DocumentParsing;
using GenAiIncubator.LlmUtils.Core.Services.EthicalClauseContractValidation;
using GenAiIncubator.LlmUtils.Core.Services.Email;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims;
using GenAiIncubator.LlmUtils.Core.Services.SustainabilityClaims.TopicComplianceChecks;

namespace GenAiIncubator.LlmUtils.Core.Extensions;

/// <summary>
/// Provides extension methods for adding LlmUtils services to the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds LlmUtils services to the IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configureOptions">An optional action to configure KernelOptions.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddLlmUtils(this IServiceCollection services, Action<KernelOptions>? configureOptions = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("GenAiIncubator.LlmUtils.Core.appsettings.json");
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException("Default appsettings.json not found."));
        var defaultConfiguration = new ConfigurationBuilder()
            .AddJsonStream(reader.BaseStream)
            .Build();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        services.Configure<AzureKeyVaultOptions>(configuration.GetSection("AzureKeyVault"));
        services.Configure<AzureLanguageOptions>(configuration.GetSection("AzureLanguage"));
        services.Configure<GraphOptions>(configuration.GetSection("GraphOptions"));

        services.Configure<KernelOptions>(configuration.GetSection("KernelOptions"));

        services.Configure<KernelOptions>(options =>
        {
            configureOptions?.Invoke(options);
        });

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<KernelOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureAiHubOptions>>().Value);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureLanguageOptions>>().Value);

        services.AddSingleton(sp =>
        {
            AzureLanguageOptions options = sp.GetRequiredService<IOptions<AzureLanguageOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
                throw new ArgumentException("AzureLanguage:Endpoint cannot be null or whitespace.");
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new ArgumentException("AzureLanguage:ApiKey cannot be null or whitespace.");

            return new TextAnalyticsClient(new Uri(options.Endpoint), new AzureKeyCredential(options.ApiKey));
        });

        services.AddSingleton(sp =>
        {
            KernelOptions options = sp.GetRequiredService<IOptions<KernelOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.DeploymentName))
            {
                throw new ArgumentException("DeploymentName cannot be null or whitespace.");
            }
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new ArgumentException("Endpoint cannot be null or whitespace.");
            }
            if (string.IsNullOrWhiteSpace(options.AzureOpenAIApiKey))
            {
                throw new ArgumentException("ApiKey cannot be null or whitespace.");
            }

            var customHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: options.DeploymentName,
                endpoint: options.Endpoint,
                apiKey: options.AzureOpenAIApiKey,
                httpClient: customHttpClient
            );

            return builder.Build();
        });

        services.AddTransient<PIIRemovalService>();
        services.AddTransient<ConversationSummarisationService>();
        services.AddTransient<CustomerJourneyClassificationService>();
        services.AddTransient<HeaterRecognitionService>();
        services.AddTransient<WhisperTranscriptionService>();
        services.AddTransient<UserStoryCreationService>();
        services.AddTransient<IDocumentParser, DocumentParser>();
        services.AddTransient<IContractEthicalClauseExtractor, ContractEthicalClauseExtractor>();
        services.AddTransient<UnwantedDataClassificationService>();
        services.AddTransient<UnwantedDataClassificationSemanticService>();
        services.AddTransient<EthicalClauseContractValidationSemanticService>();
        services.AddTransient<EthicalClauseContractValidationService>();
        services.AddTransient<ISustainabilityClaimsSemanticService, SustainabilityClaimsSemanticService>();
        services.AddTransient<IImageValidationService, ImageValidationService>();
        services
            .AddHttpClient<Services.WebContentNormalization.IWebContentNormalizationService, Services.WebContentNormalization.WebContentNormalizationService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("GenAiIncubator.LlmUtils/1.0");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xhtml+xml");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/xml;q=0.9");
                client.DefaultRequestHeaders.Accept.ParseAdd("*/*;q=0.8");
            });

        // Register individual topic-specific requirement checks
        services.AddTransient<GreenElectricityRequirementCheck>();
        services.AddTransient<GreenGasRequirementCheck>();
        services.AddTransient<HeatRequirementCheck>();
        services.AddTransient<FossilFreeLivingRequirementCheck>();
        services.AddTransient<ComparisonRequirementCheck>();
        services.AddTransient<SuperlativesRequirementCheck>();
        services.AddTransient<EnergyLabelRequirementCheck>();
        services.AddTransient<ParisClimateTargetsRequirementCheck>();

        // Register the list of topic-specific requirement checks
        services.AddTransient(provider =>
        {
            return new List<TopicSpecificRequirementCheck>
            {
                provider.GetRequiredService<GreenElectricityRequirementCheck>(),
                provider.GetRequiredService<GreenGasRequirementCheck>(),
                provider.GetRequiredService<HeatRequirementCheck>(),
                provider.GetRequiredService<FossilFreeLivingRequirementCheck>(),
                provider.GetRequiredService<ComparisonRequirementCheck>(),
                provider.GetRequiredService<SuperlativesRequirementCheck>(),
                provider.GetRequiredService<EnergyLabelRequirementCheck>(),
                provider.GetRequiredService<ParisClimateTargetsRequirementCheck>()
            };
        });

        // Register orchestration service via interface for DI consumers
        services.AddTransient<ISustainabilityClaimsService, SustainabilityClaimsService>();

        // Email services
        services.AddSingleton<IEmailService, GraphEmailService>();
        services.AddTransient<SustainabilityClaimsEmailNotificationService>();

        return services;
    }
}