using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace JsonResumeSharp.Core
{
    public class HtmlTemplateProcessor
    {
        private readonly string _templatesRoot;

        public HtmlTemplateProcessor(string? customTemplatesPath = null)
        {
            _templatesRoot = customTemplatesPath ?? 
                           Path.Combine(AppContext.BaseDirectory, "Templates");

            if (!Directory.Exists(_templatesRoot))
            {
                Directory.CreateDirectory(_templatesRoot);
            }
        }

        public async Task<string> ProcessTemplateAsync(JsonNode data, string templateName = "modern")
        {
            var templatePath = Path.Combine(_templatesRoot, templateName, "template.html");
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template not found at: {templatePath}");
            }

            var template = await File.ReadAllTextAsync(templatePath);
            return ProcessTemplate(template, data);
        }

        public string ProcessTemplate(string template, JsonNode data)
        {
            // Process each: data-bind="path.to.property"
            var result = Regex.Replace(template, 
                @"data-bind=""([^""]*)""", 
                match => ProcessBinding(match, data) ?? match.Value);

            // Process each: {{path.to.property}}
            result = Regex.Replace(result, 
                @"\{\{\s*([^}]+?)\s*\}\}", 
                match => GetValueFromPath(data, match.Groups[1].Value)?.ToString() ?? string.Empty);

            return result;
        }

        private string? ProcessBinding(Match match, JsonNode data)
        {
            var path = match.Groups[1].Value.Trim();
            var value = GetValueFromPath(data, path);
            return match.Value.Replace(match.Groups[0].Value, value?.ToString() ?? string.Empty);
        }

        private static JsonNode? GetValueFromPath(JsonNode node, string path)
        {
            try
            {
                var current = node;
                foreach (var part in path.Split('.'))
                {
                    if (current == null) return null;
                    if (current is JsonObject obj && obj.TryGetPropertyValue(part, out var value))
                    {
                        current = value;
                    }
                    else
                    {
                        return null;
                    }
                }
                return current;
            }
            catch
            {
                return null;
            }
        }
    }
}