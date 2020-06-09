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

        public ProjectService(ILogger<ProjectService> logger, ProjectRepository projectRepository, UserRepository userRepository)
        {
            _logger = logger;
            _projectRepository = projectRepository;
            _userRepository = userRepository;
        }

        public async Task<Project> CreateProject(ProjectUploadModel newProject)
        {
            // Validate the creator
            User creator = await _userRepository.GetUserById(newProject.DesignerId);
            if (creator == null)
            {
                _logger.LogError("User with Id {0} not found, cannot create a new project", newProject.DesignerId.ToString());
                throw new ApiException(404, "User with Id " + newProject.DesignerId.ToString() + " not found");
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
            SHA256 Sha256 = SHA256.Create();
            using (Stream stream = newProject.ProjectArchive.OpenReadStream())
            {
                fileHash = BytesToString(Sha256.ComputeHash(stream));
            }

            // Create the Project entry in the database
            Project databaseProject = Project.Create(newProject.ProjectName, fileHash);
            databaseProject.Designer = creator;

            Project createdProject = await _projectRepository.CreateProject(databaseProject);
            if (createdProject == null)
                throw new ApiException(500, "Project could not be created");

            // TODO: save the file into the filesystem


            return createdProject;
        }

        private string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
    }
}
