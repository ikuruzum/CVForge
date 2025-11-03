using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.DependencyInjection;
using RazorLight;
using System.Text.Json;

namespace JsonResumeSharp.Core
{
    public class TemplateRenderer
    {  private readonly IRazorLightEngine _razorEngine;
    private readonly IConverter _pdfConverter;
    private readonly string _templatesRoot;

    public TemplateRenderer(string? customTemplatesPath = null)
    {
        // Determine templates directory
        _templatesRoot = customTemplatesPath ?? 
                        Path.Combine(AppContext.BaseDirectory, "Templates");

        // Create templates directory if it doesn't exist
        if (!Directory.Exists(_templatesRoot))
        {
            Directory.CreateDirectory(_templatesRoot);
        }

        // Initialize RazorLight
        _razorEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatesRoot)
            .UseMemoryCachingProvider()
            .Build();

        // Initialize PDF converter
        var services = new ServiceCollection()
            .AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()))
            .BuildServiceProvider();

        _pdfConverter = services.GetRequiredService<IConverter>();
    }

    public async Task<string> RenderHtmlAsync(JsonNode data, string templateName = "modern")
    {
        try
        {
            // If templateName is a full path, use it directly
            string templatePath = Path.IsPathRooted(templateName) 
                ? templateName 
                : Path.Combine(_templatesRoot, templateName, "template.cshtml");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found at: {templatePath}");
            }

            var templateContent = await File.ReadAllTextAsync(templatePath);
            return await _razorEngine.CompileRenderStringAsync(
                Guid.NewGuid().ToString(),
                templateContent,
                new { CV = data });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to render HTML template: {ex.Message}", ex);
        }
    }

        public async Task<byte[]> RenderPdfAsync(JsonNode data, string templateName = "modern")
        {
            try
            {
                // First render HTML
                var html = await RenderHtmlAsync(data, templateName);

                // Then convert to PDF
                var doc = new HtmlToPdfDocument()
                {
                    GlobalSettings = {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
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

                return _pdfConverter.Convert(doc);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to generate PDF", ex);
            }
        }
    }
}
