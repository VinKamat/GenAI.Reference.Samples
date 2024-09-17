using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using SKAgentApp.Core;

using Spectre.Console;
using Spectre.Console.Rendering;
#pragma warning disable SKEXP0001, SKEXP0110

namespace GenAI.Reference.Samples.Apps
{
    internal class TaxImpactAnalyzer
    {
        internal static async Task Execute(Kernel kernel)
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
                Arguments = new KernelArguments
                {
                    ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
                        {
                            { "default", openAIPromptExecutionSettings }
                        }
                }
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
                Arguments = new KernelArguments
                {
                    ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
                        {
                            { "default", openAIPromptExecutionSettings }
                        }
                }
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
    }
}
#pragma warning restore SKEXP0001, SKEXP0101