using System;
using System.Collections.Generic;
using System.Linq;
using JsonResumeSharp.Core.Models;

namespace JsonResumeSharp.Core
{
    public class ResumeFilter
    {
        private readonly HashSet<string> _includedTags;
        private const string UniversalTag = "*";
        private const string GlobalExclusionTag = "!*";

        public ResumeFilter(IEnumerable<string> includedTags)
        {
            _includedTags = new HashSet<string>(
                includedTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)) ?? 
                new[] { UniversalTag },
                StringComparer.OrdinalIgnoreCase);
        }

        public ResumeData Filter(ResumeData resume)
        {
            if (resume == null) throw new ArgumentNullException(nameof(resume));

            var filtered = new ResumeData
            {
                Name = resume.Name,
                Titles = resume.Titles,
                Descriptions = resume.Descriptions
            };

            // Filter experiences
            filtered.Experiences = resume.Experiences
                .Where(exp => ShouldInclude(exp.Tags))
                .ToList();

            // Filter education
            filtered.Education = resume.Education
                .Where(edu => ShouldInclude(edu.Tags))
                .ToList();

            // Filter skills
            filtered.Skills = resume.Skills
                .Where(skill => ShouldInclude(skill.Tags))
                .ToList();

            return filtered;
        }

        public string GetTitle(ResumeData resume)
        {
            if (resume == null) throw new ArgumentNullException(nameof(resume));

            // Try to find the first matching title from included tags
            foreach (var tag in _includedTags)
            {
                if (resume.Titles.TryGetValue(tag, out var titleData))
                {
                    return titleData.Title;
                }
            }

            // Fallback to universal tag
            if (resume.Titles.TryGetValue(UniversalTag, out var universalTitle))
            {
                return universalTitle.Title;
            }

            // Default title if none found
            return "Software Developer";
        }

        public string GetDescription(ResumeData resume)
        {
            if (resume == null) throw new ArgumentNullException(nameof(resume));

            // Try to find the first matching description from included tags
            foreach (var tag in _includedTags)
            {
                if (resume.Descriptions.TryGetValue(tag, out var description))
                {
                    return description;
                }
            }

            // Fallback to universal tag
            if (resume.Descriptions.TryGetValue(UniversalTag, out var universalDescription))
            {
                return universalDescription;
            }

            // Default description if none found
            return string.Empty;
        }

        private bool ShouldInclude(IEnumerable<string> tags)
        {
            if (tags == null) return false;

            var tagList = tags.ToList();
            
            // Always exclude if global exclusion tag is present
            if (tagList.Contains(GlobalExclusionTag, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // If no included tags are specified, only include items with universal tag
            if (!_includedTags.Any())
            {
                return tagList.Contains(UniversalTag, StringComparer.OrdinalIgnoreCase);
            }

            // Include if any tag matches the included tags or is universal
            return tagList.Any(tag =>
                _includedTags.Contains(tag, StringComparer.OrdinalIgnoreCase) ||
                string.Equals(tag, UniversalTag, StringComparison.OrdinalIgnoreCase));
        }
    }
}
