using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using JsonResumeSharp.Core;

namespace JsonResumeSharp
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("JSON Resume Generator");
            
            var inputOption = new Option<FileInfo>(
                "--input",
                "Path to the JSON resume file");
                
            var outputOption = new Option<FileInfo?>(
                "--output",
                "Output file path (default: resume.html or resume.pdf)");
                
            var formatOption = new Option<string>(
                "--format",
                () => "html",
                "Output format (html or pdf)");
                
            var templateOption = new Option<string>(
                "--template",
                () => "modern",
                "Template to use for generation");
                
            var listTemplatesOption = new Option<bool>(
                "--templates",
                "List available templates and exit");

            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(formatOption);
            rootCommand.AddOption(templateOption);
            rootCommand.AddOption(listTemplatesOption);
            
            rootCommand.SetHandler(async (input, output, format, template, listTemplates) =>
            {
                if (listTemplates)
                {
                    await ListTemplatesAsync();
                    return;
                }

                if (input == null)
                {
                    Console.Error.WriteLine("Error: Input file is required. Use --help for usage information.");
                    return;
                }

                if (!input.Exists)
                {
                    Console.Error.WriteLine($"Error: Input file '{input.FullName}' not found.");
                    return;
                }

                var jsonData = await File.ReadAllTextAsync(input.FullName);
                var templateRenderer = new TemplateRenderer();
                
                // Parse JSON dynamically
                var jsonNode = JsonNode.Parse(jsonData) ?? throw new InvalidOperationException("Invalid JSON data");
                
                var outputPath = output?.FullName ?? $"resume.{format.ToLower()}";
                
                if (string.Equals(format, "html", StringComparison.OrdinalIgnoreCase))
                {
                    var html = await templateRenderer.RenderHtmlAsync(jsonNode, template);
                    await File.WriteAllTextAsync(outputPath, html);
                }
                else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
                {
                    var pdfBytes = await templateRenderer.RenderPdfAsync(jsonNode, template);
                    await File.WriteAllBytesAsync(outputPath, pdfBytes);
                }
                else
                {
                    Console.Error.WriteLine($"Error: Unsupported output format '{format}'. Supported formats: html, pdf");
                    return;
                }

                Console.WriteLine($"Resume successfully generated: {outputPath}");
            },
            inputOption, outputOption, formatOption, templateOption, listTemplatesOption);
            
            return await rootCommand.InvokeAsync(args);
        }
        
        private static Task ListTemplatesAsync()
        {
            Console.WriteLine("Available templates:");
            Console.WriteLine("- modern (default)");
            Console.WriteLine("- example");
            return Task.CompletedTask;
        }
    }
}