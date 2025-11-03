using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.DependencyInjection;
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
                "Path to the JSON resume file")
                .ExistingOnly();
                
            var outputOption = new Option<FileInfo?>(
                "--output",
                "Output file path (default: resume.html or resume.pdf)");
                
            var formatOption = new Option<string>(
                "--format",
                () => "html",
                "Output format (html or pdf)");
                
            var templateOption = new Option<string>(
                "--template",
                "Path to the template directory");

            rootCommand.AddOption(inputOption);
            rootCommand.AddOption(outputOption);
            rootCommand.AddOption(formatOption);
            rootCommand.AddOption(templateOption);
            
            rootCommand.SetHandler(async (input, output, format, template) =>
            {
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
                
                // Parse JSON dynamically
                var jsonNode = JsonNode.Parse(jsonData) ?? throw new InvalidOperationException("Invalid JSON data");
                
                var outputPath = output?.FullName ?? $"resume.{format.ToLower()}";
                
                if (string.Equals(format, "html", StringComparison.OrdinalIgnoreCase))
                {
                    var templateProcessor = new HtmlTemplateProcessor(template);
                    var html = await templateProcessor.ProcessTemplateAsync(jsonNode);
                    await File.WriteAllTextAsync(outputPath, html);
                }
                else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // For PDF, first generate HTML then convert to PDF
                    var templateProcessor = new HtmlTemplateProcessor(template);
                    var html = await templateProcessor.ProcessTemplateAsync(jsonNode);
                    
                    var converter = new SynchronizedConverter(new PdfTools());
                    var doc = new HtmlToPdfDocument()
                    {
                        GlobalSettings = {
                            ColorMode = ColorMode.Color,
                            Orientation = Orientation.Portrait,
                            PaperSize = PaperKind.A4,
                            Out = outputPath
                        },
                        Objects = {
                            new ObjectSettings() {
                                PagesCount = true,
                                HtmlContent = html,
                                WebSettings = { DefaultEncoding = "utf-8" },
                                HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                            }
                        }
                    };
                    
                    var pdfBytes = converter.Convert(doc);
                    await File.WriteAllBytesAsync(outputPath, pdfBytes);
                }
                else
                {
                    Console.Error.WriteLine($"Error: Unsupported output format '{format}'. Supported formats: html, pdf");
                    return;
                }

                Console.WriteLine($"Resume successfully generated: {outputPath}");
            },
            inputOption, outputOption, formatOption, templateOption);
            
            return await rootCommand.InvokeAsync(args);
        }
        
    }
}