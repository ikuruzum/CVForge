using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace JsonResumeSharp.Core
{
    public static class DynamicJsonHelper
    {
        public static JsonNode? FilterByTag(JsonNode node, string? targetTag)
        {
            if (node == null) return null;

            // Handle arrays
            if (node is JsonArray array)
            {
                var filteredArray = new JsonArray();
                foreach (var item in array)
                {
                    if (item == null) continue;

                    bool shouldInclude;
                    if (string.IsNullOrEmpty(targetTag) || targetTag == "*")
                    {
                        // When no tag or "*" is specified, exclude items with "!*" tag
                        shouldInclude = !HasTag(item, "!*");
                    }
                    else
                    {
                        // For specific tags, ignore the "!*" tag and use normal tag matching
                        shouldInclude = HasTag(item, targetTag);
                    }

                    if (shouldInclude)
                    {
                        var filtered = FilterByTag(item, targetTag);
                        if (filtered != null)
                        {
                            filteredArray.Add(filtered);
                        }
                    }
                }
                return filteredArray.Count > 0 ? filteredArray : null;
            }

            // Handle objects
            if (node is JsonObject obj)
            {
                // When no tag or "*" is specified, exclude objects with "!*" tag
                if ((string.IsNullOrEmpty(targetTag) || targetTag == "*") && HasTag(node, "!*"))
                {
                    return null;
                }

                // For specific tags, only exclude if the object has tags but none match
                if (!string.IsNullOrEmpty(targetTag) && targetTag != "*" && HasTags(node) && !HasTag(node, targetTag))
                {
                    return null;
                }

                var newObj = new JsonObject();
                foreach (var prop in obj)
                {
                    // Skip the tag property itself
                    if (prop.Key.Equals("tag", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var filteredValue = FilterByTag(prop.Value, targetTag);
                    if (filteredValue != null)
                    {
                        newObj[prop.Key] = filteredValue;
                    }
                }
                return newObj.Count > 0 ? newObj : null;
            }

            // Primitives
            return node.DeepClone();
        }

        private static bool HasTag(JsonNode node, string targetTag)
        {
            if (node is JsonObject obj)
            {
                // Skip the "!*" tag when checking for other tags
                if (obj.TryGetPropertyValue("tag", out var tagValue))
                {
                    if (tagValue is JsonArray tagArray)
                    {
                        // Ignore "!*" when checking for other tags
                        if (targetTag != "!*" && tagArray.Any(t => t?.ToString() == "!*"))
                        {
                            return false;
                        }
                        return tagArray.Any(t => t != null &&
                            string.Equals(t.ToString(), targetTag, StringComparison.OrdinalIgnoreCase));
                    }
                    return string.Equals(tagValue?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase);
                }

                // Check if any direct property is a tag
                return obj.AsObject().Any(p =>
                    string.Equals(p.Key, targetTag, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Value?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase));
            }

            return string.Equals(node?.ToString(), targetTag, StringComparison.OrdinalIgnoreCase);
        }
        private static bool HasTags(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                // Check for direct tag property
                if (obj.TryGetPropertyValue("tag", out _))
                {
                    return true;
                }

                // Check if any direct property is a tag
                return obj.AsObject().Any(p => p.Key.Equals("tag", StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        private static bool HasAnyTags(JsonNode? node)
        {
            if (node == null)
            {
                return false;
            }
            if (node is JsonObject obj)
            {
                // Check if this object has any tag properties
                if (obj.Any(p => p.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }

                // Recursively check child objects
                return obj.AsObject().Any(p => HasAnyTags(p.Value));
            }

            if (node is JsonArray array)
            {
                return array.Any(HasAnyTags);
            }

            return false;
        }

      
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
    }
}