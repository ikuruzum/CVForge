using System;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JsonResumeSharp.Core
{
    public static class DynamicJsonHelper
    {
        public static object? ResolvePath(JsonNode node, string path)
        {
            var current = node;
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                if (current == null) return null;

                // Handle array index [n]
                var arrayMatch = Regex.Match(part, "^(.*?)(?:\\((\\d+)\\))?$");
                var property = arrayMatch.Groups[1].Value;
                var index = arrayMatch.Groups.Count > 2 && !string.IsNullOrEmpty(arrayMatch.Groups[2].Value) 
                    ? int.Parse(arrayMatch.Groups[2].Value) 
                    : -1;

                if (!current.AsObject().TryGetPropertyValue(property, out var value))
                    return null;

                if (index >= 0 && value is JsonArray array)
                {
                    current = array.Count > index ? array[index] : null;
                }
                else
                {
                    current = value;
                }
            }

            return current?.GetValue<object>();
        }

        public static bool HasTag(JsonNode node, string tag)
        {
            if (node is JsonObject obj)
            {
                // Check for direct tag property
                if (obj.TryGetPropertyValue("tag", out var tagValue))
                {
                    if (tagValue is JsonArray tagArray)
                    {
                        return tagArray.Any(t => t?.ToString() == tag);
                    }
                    return tagValue?.ToString() == tag;
                }

                // Check if any property value matches the tag
                return obj.AsObject().Any(pair => pair.Value?.ToString() == tag);
            }
            
            return node?.ToString() == tag;
        }
    }
}
