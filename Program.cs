using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Spectre.Console;

using SKAgentApp.Core;
using Spectre.Console.Rendering;
using System.Text;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. 

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

// Keyboard Shortcut for GitHub Copilot in-place interaction: Alt + /

//await RunWadeTheMeteorologist(weatherApiKey, kernel);
await RunTaxImpactAnalyzer(kernel);


static async Task RunTaxImpactAnalyzer(Kernel kernel)
{
    var rule = new Rule("[bold white on green4]::Keep Or Discard (Demo only) :: Upload your receipt, letter or document for Tax Impact::[/]");
    AnsiConsole.Write(rule);
    Console.WriteLine();

    var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
    {
        // We want to be very conservative with the temperature, to be precise, for a use case like this.
        Temperature = 0.1,
        TopP = 0.95,
        MaxTokens = 2048,
        // ToolCallBehavior.AutoInvokeKernelFunctions is one of the many high value capabilities of SK,
        // without which managing function calling is not as simple
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // We may want to manage System Prompts and their variants in a backend db and load them dynamically on app startup,
    // for better Prompt Management design and maintenance. Loading from a text file is for demo purposes only.
    var systemPromptInstructions = File.ReadAllText("SystemPrompts/TaxImpactAnalyzer.txt");
    ChatCompletionAgent taxImpactAnalyzerAgent = new()
    {
        Name = "TaxImpactAnalyzerAgent",
        Description =
        """
        This agent analyzes the tax implications of an image, focusing on US tax law. 
        Agent does not answer questions by the TaxImpactReviewerAgent
    """,
        Instructions = systemPromptInstructions,
        Kernel = kernel,
        ExecutionSettings = openAIPromptExecutionSettings
    };

    systemPromptInstructions = File.ReadAllText("SystemPrompts/TaxImpactReviewer.txt");
    ChatCompletionAgent taxImpactReviewer = new()
    {
        Name = "TaxImpactReviewerAgent",
        Description =
        """
        This agent reviews the Tax Impact Analysis provided by the TaxImpactAnalyzerAgent, 
        and then asks 1 key question to the user, based on the user's input, responds whether to keep the doc/image file or not.
    """,
        Instructions = systemPromptInstructions,
        Kernel = kernel,
        ExecutionSettings = openAIPromptExecutionSettings
    };

    AgentGroupChat groupChat =
        new(taxImpactAnalyzerAgent, taxImpactReviewer)
        {
            ExecutionSettings =
                new()
                {
                    // TerminationStrategy subclass called "RecoTerminationStrategy" is used that will terminate when
                    // an agent message contains the term "recommendation".
                    TerminationStrategy =
                        new RecoTerminationStrategy()
                        {
                            Agents = [taxImpactReviewer],
                            // Limit total number of turns
                            MaximumIterations = 3,
                        }
                }
        };

    // bool indicator to check if user entered a valid file path and proceeded to the next step
    bool isUserPastFileEntry = false;

    while (true)
    {
        if (!isUserPastFileEntry) AnsiConsole.Markup("[gray]Enter \"data/donation01.jpg\" to test the app with the provided image[/]\n");
        AnsiConsole.Markup("[bold yellow on green]User > [/] ");

        // Get user input
        // Enter "data/donation01.jpg" to test the app with the provided image
        var userInput = Console.ReadLine()!;
        //var userInput = "data/donation01.jpg";

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots12)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync("Thinking...", async ctx =>
            {
                if (File.Exists(userInput))
                {
                    isUserPastFileEntry = true;
                    // Because we are sending both the image and the text to LLM at once, we need a ChatMessageContentItemCollection
                    var userChatMessage = new ChatMessageContentItemCollection();

                    // load up the file in the data folder in to chatMessage as ImageContent
                    var imageBytes = File.ReadAllBytes(userInput);
                    var uploadedImage = new ImageContent(new ReadOnlyMemory<byte>(imageBytes), "image/jpeg");
                    userChatMessage.Add(uploadedImage);

                    // This can be called a UserPrompt : Notice here, it may contribute to better reliability and quality of results,
                    // if we augment User inputs like this, if we have a good understanding of the domain and the user's intent.
                    var userPrompt = "Should I keep this document or discard it?. Analyze and recommend based on it's tax impact on my taxreturn.";
                    userChatMessage.Add(new TextContent(userPrompt));
                    groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userChatMessage));
                }
                else
                {
                    if (!isUserPastFileEntry)
                    {
                        AnsiConsole.Markup("[bold red]File does not exist, enter the path for your tax document/receipt[/]\n");
                        // exit from this await block
                        return;
                    }
                    groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userInput));
                }

                AnsiConsole.Write(new Rule());
                AnsiConsole.Markup("[bold white on green4]Agent > [/]\n");
                string agentResponse = string.Empty;

                await foreach (ChatMessageContent response in groupChat.InvokeAsync())
                {
                    var rule = new Rule($"[skyblue1] {response.AuthorName} [/]");
                    AnsiConsole.Write(rule);
                    agentResponse = response.Content!;

                    IRenderable renderableResponse = response.AuthorName == "TaxImpactReviewerAgent" ? agentResponse.RenderMarkdown() : agentResponse.RenderLog();
                    AnsiConsole.Write(renderableResponse);

                    // TODO: reset isUserPastFileEntry back to false, for another doc and continue the loop, or terminate the program
                }
                AnsiConsole.Write(new Rule());
                groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.Assistant, agentResponse));
            });
    }
}

static async Task RunWadeTheMeteorologist(string weatherApiKey, Kernel kernel)
{
    var rule = new Rule("[bold white on green4]:: Wade - the AI Meteorlogist ::[/]");
    AnsiConsole.Write(rule);
    Console.WriteLine();

    var weatherService = new WeatherService(weatherApiKey);
    kernel.ImportPluginFromObject(weatherService);

    var settings = new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };

    var chatService = kernel.GetRequiredService<IChatCompletionService>();
    ChatHistory chat = new();

    // let's pick up the system prompt text from "WadeMeteorologist.txt" file in the root of this project, and add it as system message.
    var systemPrompt = File.ReadAllText("SystemPrompts/WadeTheMeteorologist.txt");
    chat.AddSystemMessage(systemPrompt);

    while (true)
    {
        AnsiConsole.Markup("[bold yellow on green]Wade > [/] ");
        var response = new StringBuilder();
        await foreach (var update in chatService.GetStreamingChatMessageContentsAsync(chat, settings, kernel))
        {
            if (update.Content != null)
            {
                foreach (var item in update.Content)
                {
                    response.Append(item);
                    Console.Write(item);
                }
            }
        }
        Console.WriteLine();
        AnsiConsole.Write(new Rule());

        AnsiConsole.Markup("[bold white on steelblue3]User > [/] ");
        chat.AddUserMessage(content: Console.ReadLine());
        AnsiConsole.Write(new Rule());
    }
}

#pragma warning restore SKEXP0101
#pragma warning restore SKEXP0001

