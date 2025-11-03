using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JsonResumeSharp.Core.Models
{
    public class ResumeData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("titles")]
        public Dictionary<string, TitleData> Titles { get; set; } = new();

        [JsonPropertyName("description")]
        public Dictionary<string, string> Descriptions { get; set; } = new();

        [JsonPropertyName("experience")]
        public List<Experience> Experiences { get; set; } = new();

        [JsonPropertyName("education")]
        public List<Education> Education { get; set; } = new();

        [JsonPropertyName("skills")]
        public List<Skill> Skills { get; set; } = new();
    }

    public class TitleData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    public class Experience
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; set; }

        [JsonPropertyName("responsibilities")]
        public List<string> Responsibilities { get; set; } = new();

        [JsonPropertyName("tag")]
        [JsonConverter(typeof(TagConverter))]
        public List<string> Tags { get; set; } = new();
    }

    public class Education
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("startDate")]
        public string StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string EndDate { get; set; }

        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; set; }

        [JsonPropertyName("responsibilities")]
        public List<string> Responsibilities { get; set; } = new();

        [JsonPropertyName("tag")]
        [JsonConverter(typeof(TagConverter))]
        public List<string> Tags { get; set; } = new();
    }

    public class Skill
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("isCurrent")]
        public bool IsCurrent { get; set; }

        [JsonPropertyName("tag")]
        [JsonConverter(typeof(TagConverter))]
        public List<string> Tags { get; set; } = new();
    }
}
