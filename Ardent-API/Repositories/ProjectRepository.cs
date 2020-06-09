using Ardent_API.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Ardent_API.Repositories
{
    public class ProjectRepository
    {
        private readonly ILogger<ProjectRepository> _logger;
        private readonly DatabaseContext _context;

        public ProjectRepository(ILogger<ProjectRepository> logger, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
        }

        public virtual async Task<Project> CreateProject(Project project)
        {
            await _context.Projects.AddAsync(project);

            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                _logger.LogError("Server error! Project with Id {0} not saved into database\n\n", project.Id);
                throw new Exception($"Server error! Project {project.Id} not saved into database");
            }
            _logger.LogInformation("Project with Id {0} saved into database\n\n", project.Id);
            return project;
        }
    }
}
