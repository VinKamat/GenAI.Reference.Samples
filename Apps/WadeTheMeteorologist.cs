using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using SKAgentApp.Core;

using Spectre.Console;

using System.Text;

namespace GenAI.Reference.Samples.Apps
{
    internal class WadeTheMeteorologist
    {
        internal static async Task Execute(string weatherApiKey, Kernel kernel)
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
                chat.AddUserMessage(content: Console.ReadLine()!);
                AnsiConsole.Write(new Rule());
            }
        }
    }
}
