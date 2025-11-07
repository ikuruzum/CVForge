using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Playwright;

public enum OutputFormat
{
    HTML,
    PDF
}

public class TemplateEngine
{


    public TemplateEngine()
    {

    }
    private CVForgeValue _data;

    public async Task<byte[]> Render(string templatePath, CVForgeValue data, OutputFormat format = OutputFormat.HTML)
    {
        _data = data;
        var doc = new HtmlDocument();
        doc.Load(templatePath);

        ProcessNode(doc.DocumentNode, _data, null);

        using (var writer = new StringWriter())
        {
            doc.Save(writer);
            var html = writer.ToString();

            return format switch
            {
                OutputFormat.PDF => await GeneratePdf(html),
                _ => System.Text.Encoding.UTF8.GetBytes(html)
            };
        }
    }

    private void ProcessNode(HtmlNode node, CVForgeValue context, int? index)
    {
        // Process repeat-for first (it might contain value-of nodes inside)
        var repeatFor = node.GetAttributeValue("repeat-for", null);
        if (!string.IsNullOrEmpty(repeatFor))
        {
            ProcessRepeatFor(node, context, repeatFor);
            return; // The node is replaced by the repeated nodes, so we're done
        }

        // Process value-of for the current node
        var valueOf = node.GetAttributeValue("value-of", null);
        if (!string.IsNullOrEmpty(valueOf))
        {
            var cvForgeValue = GetCVForgeValueFromPath(context, valueOf);
            if (cvForgeValue != null && cvForgeValue.Value != null)
            {
                var content = cvForgeValue.Value.ToString();

                // If the CVForgeValue has a URL, wrap content in an anchor tag
                if (!string.IsNullOrEmpty(cvForgeValue.URL))
                {
                    node.InnerHtml = $"<a href=\"{cvForgeValue.URL}\">{content}</a>";
                }
                else
                {
                    node.InnerHtml = content;
                }
            }
            node.Attributes.Remove("value-of");
        }

        // Process child nodes with the current context
        if (node.HasChildNodes)
        {
            // Create a copy of child nodes to avoid modification during iteration
            var childNodes = node.ChildNodes.ToList();
            foreach (var child in childNodes)
            {
                ProcessNode(child, context, null);
            }
        }
    }
    private void ProcessRepeatFor(HtmlNode node, CVForgeValue context, string repeatPath)
    {
        var parent = node.ParentNode;
        if (parent == null)
        {
            node.Remove();
            return;
        }

        // Get the value at the repeat path
        var value = GetValueFromPath(context, repeatPath);

        // If not found and path contains dots, try just the last part (for nested contexts)
        if (value == null && repeatPath.Contains('.'))
        {
            var lastPart = repeatPath.Substring(repeatPath.LastIndexOf('.') + 1);
            value = GetValueFromPath(context, lastPart);
        }

        List<CVForgeValue> collection = null;

        // Handle different types of values
        if (value is List<CVForgeValue> list)
        {
            collection = list;
        }
        else if (value is string str && !string.IsNullOrEmpty(str))
        {
            // If it's a comma-separated string, split it into items
            collection = str.Split(',')
                .Select(s => new CVForgeValue { Value = s.Trim() })
                .ToList();
        }
        else if (value != null)
        {
            // Single value - wrap it in a list
            collection = new List<CVForgeValue> { new CVForgeValue { Value = value } };
        }

        if (collection == null || collection.Count == 0)
        {
            node.Remove();
            return;
        }

        // Store the node to clone for each item
        var templateNode = node.CloneNode(true);
        // Remove the repeat-for attribute from the template
        templateNode.Attributes.Remove("repeat-for");

        // Store the next sibling before removing the original node
        var nextSibling = node.NextSibling;

        // Remove the original node
        node.Remove();

        // For each item in the collection, create a clone and process it
        foreach (var item in collection)
        {
            var clone = templateNode.CloneNode(true);

            // Find all value-of attributes in the clone
            var valueNodes = clone.DescendantsAndSelf()
                .Where(n => n.Attributes["value-of"] != null)
                .ToList();

            foreach (var valueNode in valueNodes)
            {
                var valueOf = valueNode.Attributes["value-of"].Value;

                // Extract last parts of both paths for comparison
                var valueOfLastPart = valueOf.Contains('.') ? valueOf.Substring(valueOf.LastIndexOf('.') + 1) : valueOf;
                var repeatLastPart = repeatPath.Contains('.') ? repeatPath.Substring(repeatPath.LastIndexOf('.') + 1) : repeatPath;

                // If the last parts match, use the current item's value directly
                if (valueOfLastPart == repeatLastPart)
                {
                    var itemValue = GetItemValue(item);
                    valueNode.InnerHtml = itemValue;
                    valueNode.Attributes.Remove("value-of");
                }
                // If the path starts with the repeat path, resolve relative to current item
                else if (valueOf.StartsWith(repeatPath + "."))
                {
                    var propertyPath = valueOf.Substring(repeatPath.Length + 1);
                    valueNode.SetAttributeValue("value-of", propertyPath);
                }
                // Otherwise, keep the value-of attribute for ProcessNode to handle
                else
                {
                    valueNode.SetAttributeValue("value-of", valueOf);
                }
            }

            // Process the clone with the current item as context
            ProcessNode(clone, item, null);
            parent.InsertBefore(clone, nextSibling);
        }
    }

    private string GetItemValue(CVForgeValue item)
    {
        if (item == null || item.Value == null)
            return "";

        // If value is a string, return it directly
        if (item.Value is string str)
            return str;

        // If value is a list, shouldn't happen in this context but handle it
        if (item.Value is List<CVForgeValue> list)
            return string.Join(", ", list.Select(x => GetItemValue(x)));

        // For other types, convert to string
        return item.Value.ToString() ?? "";
    }

    private CVForgeValue GetCVForgeValueFromPath(CVForgeValue data, string path)
    {
        if (data == null || string.IsNullOrEmpty(path))
            return null;

        var parts = path.Split('.');
        var current = data;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (current == null)
                return null;

            // Handle array index
            if (int.TryParse(part, out int index))
            {
                if (current.Value is List<CVForgeValue> list && index >= 0 && index < list.Count)
                {
                    current = list[index];
                    continue;
                }
                return null;
            }

            // Handle dictionary access
            if (current.data.TryGetValue(part, out var nextValue))
            {
                if (i == parts.Length - 1 && !string.IsNullOrEmpty(current.URL) && string.IsNullOrEmpty(nextValue.URL))
                {
                    nextValue.URL = current.URL;
                }

                current = nextValue;
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private object GetValueFromPath(CVForgeValue data, string path)
    {
        var cvForgeValue = GetCVForgeValueFromPath(data, path);
        return cvForgeValue?.Value;
    }
    public static async Task<byte[]> HtmlToPdf_PlaywrightAsync(string html)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        // pdf options: format, margin, displayHeaderFooter, headerTemplate, footerTemplate
        var pdf = await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            Margin = new Margin { Top = "10mm", Bottom = "10mm", Left = "10mm", Right = "10mm" },
            PrintBackground = true
        });
        return pdf;
    }
    private async Task<byte[]> GeneratePdf(string html)
    {
        return await HtmlToPdf_PlaywrightAsync(html);
    }
}