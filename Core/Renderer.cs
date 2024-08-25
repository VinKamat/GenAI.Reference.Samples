
using Spectre.Console;
using Spectre.Console.Rendering;

using System.Text.RegularExpressions;

namespace SKAgentApp.Core
{
    public static class RendererExtensions
    {
        public static IRenderable RenderLog(this string markdownText)
        {
            // Convert Markdown to plain text (basic conversion)
            string plainText = Regex.Replace(markdownText, @"(\*\*|__)(.*?)\1", "[bold]$2[/]");
            plainText = Regex.Replace(plainText, @"(\*|_)(.*?)\1", "[italic]$2[/]");
            plainText = Regex.Replace(plainText, @"(#+) (.*)", "[underline]$2[/]");

            var panel = new Panel($"[gray]{plainText}[/]")
                .PadLeft(5)
                .PadRight(5)
                .BorderStyle(Style.Parse("black"));
            panel.Border = BoxBorder.Rounded;
            return panel;
        }

        public static IRenderable RenderMarkdown(this string markdownText)
        {
            // Convert Markdown to plain text (basic conversion)
            string plainText = Regex.Replace(markdownText, @"(\*\*|__)(.*?)\1", "[bold]$2[/]");
            plainText = Regex.Replace(plainText, @"(\*|_)(.*?)\1", "[italic]$2[/]");
            plainText = Regex.Replace(plainText, @"(#+) (.*)", "[underline]$2[/]");

            var panel = new Panel($"[white]{plainText}[/]")                
                .PadLeft(3)
                .PadRight(3)
                .BorderStyle(Style.Parse("black"));

            panel.Border = BoxBorder.Rounded;
            return panel;
        }
    }
}
