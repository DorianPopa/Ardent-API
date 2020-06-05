using System;
using System.ComponentModel.DataAnnotations;

namespace Ardent_API.Models
{
    public class Project
    {
        private Project() { }

        public static Project Create(string name, string projectHash)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(projectHash))
                throw new ArgumentException();

            return new Project
            {
                Id = Guid.NewGuid(),
                Name = name,
                UpdatedAt = DateTime.UtcNow,
                ProjectHash = projectHash
            };
        }

        [Required]
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public string ProjectHash { get; set; }

        public virtual User Designer { get; set; }
        public virtual User Client { get; set; }
    }
}
