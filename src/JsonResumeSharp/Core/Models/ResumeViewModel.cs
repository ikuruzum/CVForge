using System.Collections.Generic;
using System.Linq;

namespace JsonResumeSharp.Core.Models
{
    public class ResumeViewModel
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Experience> Experiences { get; set; } = new();
        public List<Education> Education { get; set; } = new();
        public List<Skill> Skills { get; set; } = new();

        public static ResumeViewModel CreateFrom(ResumeData resume, ResumeFilter filter)
        {
            return new ResumeViewModel
            {
                Name = resume.Name,
                Title = filter.GetTitle(resume),
                Description = filter.GetDescription(resume),
                Experiences = resume.Experiences,
                Education = resume.Education,
                Skills = resume.Skills
            };
        }
    }
}
