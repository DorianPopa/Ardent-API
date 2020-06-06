using System;
using System.ComponentModel.DataAnnotations;

namespace Ardent_API.Models
{
    public class User
    {
        private User() { }

        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(10, ErrorMessage = "Username length can't be more than 10.")]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [Range(0, 2, ErrorMessage = "Role value must be 0, 1 or 2")]
        public int Role { get; set; }
        // 0 = admin, 1 = designer, 2 = client

        //public virtual ICollection<Project> Projects { get; set; }
    }
}
