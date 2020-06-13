using Ardent_API.Errors;
using Ardent_API.Models;
using Ardent_API.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Ardent_API.Services
{
    public class ProjectService
    {
        private readonly ILogger<ProjectService> _logger;
        private readonly ProjectRepository _projectRepository;
        private readonly UserRepository _userRepository;

        private const string PROJECTS_BASE_DIRECTORY = @".\Projects";

        public ProjectService(ILogger<ProjectService> logger, ProjectRepository projectRepository, UserRepository userRepository)
        {
            _logger = logger;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        public async Task<Project> CreateProject(ProjectUploadModel newProject, string designerId)
        {
            // Validate the creator
            Guid creatorId = Guid.Parse(designerId);
            User creator = await _userRepository.GetUserById(creatorId);
            if (creator == null)
            {
                _logger.LogError("User with Id {0} not found, cannot create a new project", creatorId.ToString());
                throw new ApiException(404, "User with Id " + creatorId.ToString() + " not found");
            }
            if (creator.Role != 0 && creator.Role != 1)
            {
                _logger.LogError("User with Id {0} does not have permission to upload projects", creator.Id.ToString());
                throw new ApiException(403, "Insufficient permisions to create a new project");
            }

            // Validate the project file
            long fileSize = newProject.ProjectArchive.Length;
            if (fileSize <= 0)
            {
                _logger.LogError("The project file is empty");
                throw new ApiException(400, "Empty project file");
            }

            // Check if the file is actually a zip archive
            byte[] fileType = new byte[3];
            try
            {
                using (Stream fileStream = newProject.ProjectArchive.OpenReadStream())
                {
                    await fileStream.ReadAsync(fileType, 0, 2);
                    if (fileType[0] != 'P' || fileType[1] != 'K')
                    {
                        throw new ApiException(400, "Project file is not a zip archive");
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApiException)
                    throw e;
                else
                    throw new ApiException(500, e.Message);
            }

            string fileHash;
            using (SHA256 Sha256 = SHA256.Create())
            {
                using (Stream stream = newProject.ProjectArchive.OpenReadStream())
                {
                    fileHash = BytesToString(Sha256.ComputeHash(stream));
                }
            }

            // Create the Project entry in the database
            Project databaseProject = Project.Create(newProject.ProjectName, fileHash);
            databaseProject.Designer = creator;

            Project createdProject = await _projectRepository.CreateProject(databaseProject);
            if (createdProject == null)
                throw new ApiException(500, "Project could not be created");

            // Save the file into the filesystem
            string projectDirectoryPath = Path.Combine(PROJECTS_BASE_DIRECTORY, createdProject.Id.ToString());
            try
            {
                Directory.CreateDirectory(projectDirectoryPath);
                _logger.LogInformation("Created new project directory at {0}\n\n", projectDirectoryPath);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw new ApiException(500, "Project directory could not be created");
            }

            string projectArchivePath = Path.Combine(projectDirectoryPath, createdProject.Id.ToString());
            try
            {
                using(Stream stream = new FileStream(projectArchivePath, FileMode.CreateNew))
                {
                    await newProject.ProjectArchive.CopyToAsync(stream);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw new ApiException(500, "Project file could not written to the filesystem");
            }

            return createdProject;

            /*
             * TODO: 
             * Prevent invalid Database entries when filesystem writing raises an exception 
             * Prevent writing the same project for the same user multiple times
             * Decrease the number of open streams for the project archive file
            */
        }

        public async Task<Project> UpdateProjectData(Guid projectId, ProjectUpdateFieldsModel updatedFields, string designerId)
        {
            Project project = await _projectRepository.GetProjectById(projectId);
            if(project == null)
            {
                _logger.LogError("Project with Id {0} not found", projectId.ToString());
                throw new ApiException(404, "Project with Id " + projectId.ToString() + " not found");
            }
            if (project.Designer.Id != Guid.Parse(designerId))
            {
                _logger.LogError("User with id {0} is unauthorized to modify project with id {1}", designerId, project.Id);
                throw new ApiException(401, $"Unauthorized to modify the project with id {project.Id}");
            }

            try
            {
                Project updatedProject = await _projectRepository.UpdateProjectData(projectId, updatedFields);
                return updatedProject;
            }
            catch(Exception e)
            {
                throw new ApiException(500, e.Message);
            }
        }

        public async Task<Project> GetProjectById(Guid id)
        {
            Project project = await _projectRepository.GetProjectById(id);
            if(project == null)
            {
                _logger.LogError("Project with Id {0} not found", id.ToString());
                throw new ApiException(404, "Project with Id " + id.ToString() + " not found");
            }
            return project;
        }

        private string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
    }
}
