using Ardent_API.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        public virtual async Task<Project> UpdateProjectData(Guid projectId, ProjectUpdateFieldsModel updatedFields)
        {
            Project projectToBeUpdated = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if(projectToBeUpdated == null)
            {
                _logger.LogError("Project with Id {0} could not be found in the database", projectId);
                throw new Exception($"Project with Id {projectId} could not be found in the database");
            }

            projectToBeUpdated.Name = updatedFields.Name;
            projectToBeUpdated.UpdatedAt = DateTime.UtcNow;          

            var result = await _context.SaveChangesAsync();
            if(result == 0)
            {
                _logger.LogError("Server error! Project with Id {0} could not be updated\n\n", projectToBeUpdated.Id);
                throw new Exception($"Server error! Project {projectToBeUpdated.Id} could not be updated");
            }
            _logger.LogInformation("Project with Id {0} updated into database\n\n", projectToBeUpdated.Id);
            return projectToBeUpdated;
        }

        public virtual async Task<Project> UpdateProjectHash(Guid projectId, string newHash)
        {
            Project projectToBeUpdated = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (projectToBeUpdated == null)
            {
                _logger.LogError("Project with Id {0} could not be found in the database", projectId);
                throw new Exception($"Project with Id {projectId} could not be found in the database");
            }

            projectToBeUpdated.ProjectHash = newHash;
            projectToBeUpdated.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                _logger.LogError("Server error! Project with Id {0} could not be updated\n\n", projectToBeUpdated.Id);
                throw new Exception($"Server error! Project {projectToBeUpdated.Id} could not be updated");
            }
            _logger.LogInformation("Project with Id {0} updated into database\n\n", projectToBeUpdated.Id);
            return projectToBeUpdated;
        }

        public virtual async Task<Project> UpdateProjectClient(Guid projectId, User newClient)
        {
            Project projectToBeUpdated = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (projectToBeUpdated == null)
            {
                _logger.LogError("Project with Id {0} could not be found in the database", projectId);
                throw new Exception($"Project with Id {projectId} could not be found in the database");
            }

            projectToBeUpdated.Client = newClient;
            projectToBeUpdated.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                _logger.LogError("Server error! Project with Id {0} could not be updated\n\n", projectToBeUpdated.Id);
                throw new Exception($"Server error! Project {projectToBeUpdated.Id} could not be updated");
            }
            _logger.LogInformation("Project with Id {0} updated into database\n\n", projectToBeUpdated.Id);
            return projectToBeUpdated;
        }

        public virtual async Task<Project> GetProjectById(Guid id)
        {
            Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
                return null;

            EntityEntry<Project> entry = _context.Entry(project);
            entry.Reference(p => p.Designer).Load();
            entry.Reference(p => p.Client).Load();

            return project;
        }

    }
}
