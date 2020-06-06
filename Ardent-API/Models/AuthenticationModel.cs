using System.ComponentModel.DataAnnotations;

namespace Ardent_API.Models
{
    public class AuthenticationModel
    {
        [Required]
        [StringLength(10, ErrorMessage = "Username length can't be more than 10.")]
        public string Username { get; set; }

        [Required]
        public string PasswordPlain { get; set; }
    }
}
