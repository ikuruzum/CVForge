using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class CVForgeValue
{
    private static readonly ISerializer serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public CVForgeValue() { }

    public CVForgeValue(string yamlContent)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var yamlObject = deserializer.Deserialize<dynamic>(yamlContent);

        // If the YAML is a simple value (string, number, bool, etc.)
        if (yamlObject is string || yamlObject is int || yamlObject is bool || yamlObject is decimal)
        {
            Value = yamlObject;
            return;
        }

        // If the YAML is a dictionary
       else if (yamlObject is IDictionary<object, object> yamlDict)
        {
            // Handle CVForgeValue properties
            if (yamlDict.TryGetValue("value", out var valueObj))
            {
                Value = valueObj?.ToString();
            }

            if (yamlDict.TryGetValue("tags", out var tagsObj) && tagsObj is IList<object> tagsList)
            {
                Tags = tagsList.OfType<string>().ToList();
            }

            if (yamlDict.TryGetValue("url", out var urlObj))
            {
                URL = urlObj?.ToString();
            }

            if (yamlDict.TryGetValue("explicit", out var explicitObj))
            {
                Explicit = explicitObj as bool? ?? false;
            }


            // Process all key-value pairs in the dictionary
            foreach (var kvp in yamlDict)
            {
                if (!(kvp.Key is string key)) continue;

                // Skip CVForgeValue special properties to avoid duplication
                if (key == "value" || key == "tags" || key == "url" || key == "explicit")
                    continue;

                if (kvp.Value is IDictionary<object, object> nestedDict)
                {
                    // Recursively create CVForgeValue for nested dictionaries
                    var nestedYaml = serializer.Serialize(nestedDict);
                    data[key] = new CVForgeValue(nestedYaml);
                }
                else if (kvp.Value is IList<object> list)
                {
                    // Convert list to List<CVForgeValue>
                    var cvList = new List<CVForgeValue>();
                    foreach (var item in list)
                    {
                        if (item is IDictionary<object, object> itemDict)
                        {
                            var itemValue = new CVForgeValue();
                            foreach (var itemKvp in itemDict)
                            {
                                if (itemKvp.Key is string itemKey)
                                {
                                    itemValue[itemKey] = new CVForgeValue { Value = itemKvp.Value };
                                }
                            }
                            cvList.Add(itemValue);
                        }
                        else if (item is IList<object> nestedList)
                        {
                            var nestedCvList = new List<CVForgeValue>();
                            foreach (var nestedItem in nestedList)
                            {
                                nestedCvList.Add(new CVForgeValue { Value = nestedItem });
                            }
                            cvList.Add(new CVForgeValue { Value = nestedCvList });
                        }
                        else
                        {
                            cvList.Add(new CVForgeValue { Value = item });
                        }
                    }
                    data[key] = new CVForgeValue { Value = cvList };
                }
                else
                {
                    // Simple value
                    data[key] = new CVForgeValue { Value = kvp.Value };
                }
            }
        }

        else
        {
            // If it's any other type, just store it as is
            Value = yamlObject;
        }
    }


    public bool Explicit { get; set; }


    public dynamic? Value { get; set; }

    public List<string> Tags  = new List<string>();


    public string? URL { get; set; }

    private Dictionary<string, CVForgeValue> data { get; set; } = new Dictionary<string, CVForgeValue>();
   

    private List<CVForgeValue>? ListValue
    {
        get => Value as List<CVForgeValue>;
        set { if (value != null) Value = value; }
    }

    // Indexer to access data dictionary
    public CVForgeValue? this[string key]
    {
        get => data.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value != null)
                data[key] = value;
            else
                data.Remove(key);
        }
    }

    public static CVForgeValue FromYaml(string yamlContent)
    {
        return new CVForgeValue(yamlContent);
    }

    public static CVForgeValue FromYamlFile(string filePath)
    {
        var yamlContent = File.ReadAllText(filePath);
        return new CVForgeValue(yamlContent);
    }

    public string ToYaml()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        // For simple values with no additional metadata, just serialize the value
        if (Tags == null && string.IsNullOrEmpty(URL))
        {
            return serializer.Serialize(Value);
        }

        // Use the serializer with the attributes
        return serializer.Serialize(this);
    }

    public void SaveToYamlFile(string filePath)
    {
        var yaml = ToYaml();
        File.WriteAllText(filePath, yaml);
    }

    /// <summary>
    /// Recursively removes empty CVForgeValue objects from the hierarchy.
    /// A CVForgeValue is considered empty if it has no value, no data, and no list value.
    /// </summary>
    public CVForgeValue PruneEmptyValues()
    {
        // Handle empty or null Value
        if (Value == null && data.Count == 0 && (ListValue == null || ListValue.Count == 0))
        {
            return this;
        }

        // Process dictionary values
        if (data.Count > 0)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in data.ToList())
            {
                if (kvp.Value != null)
                {
                    var pruned = kvp.Value.PruneEmptyValues();
                    if (pruned.IsEmpty())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                    else
                    {
                        data[kvp.Key] = pruned;
                    }
                }
                else
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                data.Remove(key);
            }
        }

        // Process list values
        if (ListValue is List<CVForgeValue> listValue)
        {
            var newList = new List<CVForgeValue>();
            foreach (var item in listValue)
            {
                if (item != null)
                {
                    var prunedItem = item.PruneEmptyValues();
                    if (!prunedItem.IsEmpty())
                    {
                        newList.Add(prunedItem);
                    }
                }
            }
            
            if (newList.Count > 0)
            {
                Value = newList;
            }
            else
            {
                Value = null;
            }
        }

        return this;
    }

    /// <summary>
    /// Checks if this CVForgeValue is empty (has no value, no data, and no list value).
    /// </summary>
    public bool IsEmpty()
    {
        return Value == null && 
               data.Count == 0 && 
               (ListValue == null || ListValue.Count == 0) &&
               string.IsNullOrEmpty(URL) &&
               !Explicit &&
               (Tags == null || Tags.Count == 0);
    }
    public CVForgeValue FilterTags(List<string> tags)
    {
        // Separate included and excluded tags
        var includedTags = tags.Where(t => !t.StartsWith("!")).ToList();
        var excludedTags = tags
            .Where(t => t.StartsWith("!"))
            .Select(t => t.Substring(1))
            .ToList();

        var result = new CVForgeValue();

        // If this is an explicit item, it should only be included if explicitly requested
        if (Explicit)
        {
            // If no included tags are specified, exclude this item
            if (includedTags.Count == 0)
                return new CVForgeValue();

            // If this item has no tags, exclude it
            if (Tags == null || Tags.Count == 0)
                return new CVForgeValue();

            // Only include if it has at least one of the included tags
            if (!includedTags.Any(tag => Tags.Contains(tag)))
                return new CVForgeValue();
        }

        // If we have excluded tags and any tag matches, exclude this item
        if (excludedTags.Count > 0 && Tags != null && Tags.Any(tag => excludedTags.Contains(tag)))
            return new CVForgeValue();

        // Process the data dictionary
        foreach (var item in data)
        {
            if (item.Value == null) continue;

            // Recursively filter nested values
            var filteredNested = item.Value.FilterTags(tags);
            if (filteredNested.data.Count > 0 || filteredNested.Value != null)
            {
                result[item.Key] = filteredNested;
            }
        }

        // Process list values
        if (ListValue is List<CVForgeValue> listValue)
        {
            var filteredList = new List<CVForgeValue>();
            foreach (var item in listValue)
            {
                if (item == null) continue;
                
                var filteredItem = item.FilterTags(tags);
                if (filteredItem.data.Count > 0 || filteredItem.Value != null)
                {
                    filteredList.Add(filteredItem);
                }
            }
            
            if (filteredList.Count > 0)
            {
                result.Value = filteredList;
            }
        }

        // If this is a simple value with no children, return a copy if it matches the filter
        if (data.Count == 0 && Value != null)
        {
            // If we have included tags, this item must have at least one matching tag
            if (includedTags.Count > 0 && !Tags!.Any(tag => includedTags.Contains(tag) ) && Tags!.Count != 0)
                return new CVForgeValue();

            // If we have excluded tags and this item has any of them, exclude it
            if (excludedTags.Count > 0 && Tags != null && Tags.Any(tag => excludedTags.Contains(tag)))
                return new CVForgeValue();

            return new CVForgeValue
            {
                Value = this.Value,
                Tags = this.Tags != null ? new List<string>(this.Tags) : null,
                URL = this.URL,
                Explicit = this.Explicit
            };
        }

        return result.PruneEmptyValues();
    }
}