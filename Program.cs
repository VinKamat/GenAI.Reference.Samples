using GenAI.Reference.Samples.Apps;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>();
var configuration = builder.Build();

string weatherApiKey = configuration["AI:WeatherApi:ApiKey"]
                  ?? throw new ArgumentNullException(nameof(weatherApiKey), "The weatherApi Key is not set as a user secret.");

string endpoint = configuration["AI:EASub:AzureOpenAI:Endpoint"]
                  ?? throw new ArgumentNullException(nameof(endpoint), "The Azure OpenAI endpoint is not set as a user secret.");

string apiKey = configuration["AI:EASub:AzureOpenAI:ApiKey"]
                ?? throw new ArgumentNullException(nameof(apiKey), "The Azure OpenAI API key is not set as a user secret.");

var kernelBuilder = Kernel.CreateBuilder();

// Uncomment the following line to enable Console logging to see how Semantic Kernel observability works
// kernelBuilder.Services.AddLogging(c => c.SetMinimumLevel(LogLevel.Trace).AddConsole());

kernelBuilder.Services.AddAzureOpenAIChatCompletion("gpt-4o", endpoint, apiKey);
Kernel kernel = kernelBuilder.Build();

// #1 Wade, the Meteorologist
// await WadeTheMeteorologist.Execute(weatherApiKey, kernel);

// #2 TaxImpactAnalyzer - AgentGroupChat
await TaxImpactAnalyzer.Execute(kernel);

