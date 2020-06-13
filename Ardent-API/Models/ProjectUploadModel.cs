using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ardent_API.Models
{
    public class ProjectUploadModel
    {
        [Required]
        [StringLength(20, ErrorMessage = "Project name length can't be more than 20.")]
        public string ProjectName { get; set; }

        [Required]
        public IFormFile ProjectArchive { get; set; }
    }
}
