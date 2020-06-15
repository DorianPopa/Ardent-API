using Ardent_API.Errors;
using Ardent_API.Models;
using Ardent_API.Repositories;
using Microsoft.AspNetCore.Http;
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

        public async Task<Project> CreateProject(ProjectUploadModel newProject, Guid designerId)
        {
            // Validate the creator
            User creator = await _userRepository.GetUserById(designerId);
            if (creator == null)
            {
                _logger.LogError("User with Id {0} not found, cannot create a new project", designerId.ToString());
                throw new ApiException(404, "User with Id " + designerId.ToString() + " not found");
            }
            if (creator.Role != 0 && creator.Role != 1)
            {
                _logger.LogError("User with Id {0} does not have permission to upload projects", creator.Id.ToString());
                throw new ApiException(403, "Insufficient permisions to create a new project");
            }

            // Validate the file
            string fileHash;
            try
            {
                fileHash = await ValidateFile(newProject.ProjectArchive);
            }
            catch(ApiException e)
            {
                throw e;
            }

            // Create the Project entry in the database
            Project databaseProject = Project.Create(newProject.ProjectName, fileHash);
            databaseProject.Designer = creator;

            Project createdProject = await _projectRepository.CreateProject(databaseProject);
            if (createdProject == null)
                throw new ApiException(500, "Project could not be created");

            try
            {
                await WriteFileIntoFilesystem(newProject.ProjectArchive, createdProject.Id);
            }
            catch(ApiException e)
            {
                throw e;
            }

            return createdProject;

            /*
             * TODO: 
             * Prevent invalid Database entries when filesystem writing raises an exception 
             * Prevent writing the same project for the same user multiple times
             * Decrease the number of open streams for the project archive file
            */
        }

        public async Task<Project> UpdateProjectData(Guid projectId, ProjectUpdateFieldsModel updatedFields, Guid designerId)
        {
            Project project = await _projectRepository.GetProjectById(projectId);
            if (project == null)
            {
                _logger.LogError("Project with Id {0} not found", projectId.ToString());
                throw new ApiException(404, "Project with Id " + projectId.ToString() + " not found");
            }
            if (project.Designer.Id != designerId)
            {
                _logger.LogError("User with id {0} is unauthorized to modify project with id {1}", designerId.ToString(), project.Id);
                throw new ApiException(401, $"Unauthorized to modify the project with id {project.Id}");
            }

            Project updatedProject = null;
            User client;
            if (updatedFields.ClientUsername != null)
            {
                client = await _userRepository.GetUserByUsername(updatedFields.ClientUsername);
                if(client == null)
                {
                    _logger.LogError("User with Username {0} not found", updatedFields.ClientUsername);
                    throw new ApiException(404, "User with Username " + updatedFields.ClientUsername + " not found");
                }

                try
                {
                    updatedProject = await _projectRepository.UpdateProjectClient(projectId, client);
                }
                catch (Exception e)
                {
                    throw new ApiException(500, e.Message);
                }
            }
            if (updatedFields.Name != null)
            {
                try
                {
                    updatedProject = await _projectRepository.UpdateProjectData(projectId, updatedFields);
                }
                catch (Exception e)
                {
                    throw new ApiException(500, e.Message);
                }
            }
            if(updatedProject == null)
            {
                throw new ApiException(400, "Client username and project fields are both null");
            }

            return updatedProject;
        }

        public async Task<Project> UpdateProjectFiles(Guid projectId, Guid designerId, IFormFile updatedArchive)
        {
            Project project = await _projectRepository.GetProjectById(projectId);
            if (project == null)
            {
                _logger.LogError("Project with Id {0} not found", projectId.ToString());
                throw new ApiException(404, "Project with Id " + projectId.ToString() + " not found");
            }
            if (project.Designer.Id != designerId)
            {
                _logger.LogError("User with id {0} is unauthorized to modify project with id {1}", designerId.ToString(), project.Id);
                throw new ApiException(401, $"Unauthorized to modify the project with id {project.Id}");
            }

            // Validate the file and write the new archive into the filesystem
            // Update the archive checksum into the database
            string fileHash;
            try
            {
                fileHash = await ValidateFile(updatedArchive);
                await WriteFileIntoFilesystem(updatedArchive, project.Id);
                project = await _projectRepository.UpdateProjectHash(projectId, fileHash);
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new ApiException(500, e.Message);
            }

            return project;
        }

        public async Task<byte[]> GetProjectFilesById(Guid projectId, Guid requesterId)
        {
            Project project = await _projectRepository.GetProjectById(projectId);
            if (project == null)
            {
                _logger.LogError("Project with Id {0} not found", projectId.ToString());
                throw new ApiException(404, "Project with Id " + projectId.ToString() + " not found");
            }

            bool hasAccess = false;
            if(project.Designer != null)
            {
                if (project.Designer.Id == requesterId)
                    hasAccess = true;
            }
            if(project.Client != null)
            {
                if (project.Client.Id == requesterId)
                    hasAccess = true;
            }
            if (!hasAccess)
            {
                _logger.LogError("User with id {0} is unauthorized to access project with id {1}", requesterId.ToString(), project.Id);
                throw new ApiException(401, $"Unauthorized to access the project with id {project.Id}");
            }

            try
            {
                byte[] fileBytes = await ReadProjectFiles(projectId);
                return fileBytes;
            }
            catch(Exception e)
            {
                throw new ApiException(500, e.Message);
            }
        }

        public async Task<Project> GetProjectById(Guid projectId)
        {
            Project project = await _projectRepository.GetProjectById(projectId);
            if (project == null)
            {
                _logger.LogError("Project with Id {0} not found", projectId.ToString());
                throw new ApiException(404, "Project with Id " + projectId.ToString() + " not found");
            }
            return project;
        }

        private string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }

        private async Task<string> ValidateFile(IFormFile file)
        {
            // Check if the file is not empty
            long fileSize = file.Length;
            if (fileSize <= 0)
            {
                _logger.LogError("The project file is empty");
                throw new ApiException(400, "Empty project file");
            }

            // Check if the file is actually a zip archive
            byte[] fileType = new byte[3];
            try
            {
                using (Stream fileStream = file.OpenReadStream())
                {
                    await fileStream.ReadAsync(fileType, 0, 2);
                    if (fileType[0] != 'P' || fileType[1] != 'K')
                    {
                        _logger.LogError("The project file is not a zip archive");
                        throw new ApiException(400, "Project file is not a zip archive");
                    }
                }
            }
            catch (ApiException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new ApiException(500, e.Message);
            }

            // Compute the checksum of the file
            string fileHash;
            using (SHA256 Sha256 = SHA256.Create())
            {
                using (Stream stream = file.OpenReadStream())
                {
                    fileHash = BytesToString(Sha256.ComputeHash(stream));
                }
            }
            return fileHash;
        }

        private async Task WriteFileIntoFilesystem(IFormFile ProjectArchive, Guid projectId)
        {
            // Create a new directory for the project archive
            string projectDirectoryPath = Path.Combine(PROJECTS_BASE_DIRECTORY, projectId.ToString());
            try
            {
                Directory.CreateDirectory(projectDirectoryPath);
                _logger.LogInformation("Created new project directory at {0}\n\n", projectDirectoryPath);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw new ApiException(500, "Project directory could not be created");
            }

            // Create the final path of the archive based on the project id and write the archived data 
            // into the filesystem
            string projectArchivePath = Path.Combine(projectDirectoryPath, projectId.ToString());
            try
            {
                using (Stream stream = new FileStream(projectArchivePath, FileMode.Create))
                {
                    await ProjectArchive.CopyToAsync(stream);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw new ApiException(500, "Project file could not written to the filesystem");
            }
        }

        private async Task<byte[]> ReadProjectFiles(Guid projectId)
        {
            string projectFilePath = Path.Combine(PROJECTS_BASE_DIRECTORY, projectId.ToString(), projectId.ToString());
            if (File.Exists(projectFilePath))
            {
                byte[] b = await File.ReadAllBytesAsync(projectFilePath);
                return b;
            }
            else
            {
                throw new ApiException(500, "Project file not found in the filesystem");
            }
        }
    }
}
