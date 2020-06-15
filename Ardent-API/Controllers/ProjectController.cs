using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Ardent_API.Errors;
using Ardent_API.Models;
using Ardent_API.Security;
using Ardent_API.Services;
using JWT.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ardent_API.Controllers
{
    [ApiController]
    [Route("projects")]
    public class ProjectController : ControllerBase
    {
        private readonly ILogger<ProjectController> _logger;
        private readonly IAuthenticationService _authenticationService;
        private readonly ProjectService _projectService;

        public ProjectController(ILogger<ProjectController> logger, IAuthenticationService authenticationService, ProjectService projectService)
        {
            _logger = logger;
            _authenticationService = authenticationService;
            _projectService = projectService;
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadProject([FromForm] ProjectUploadModel project)
        {
            _logger.LogInformation("Upload request for a new project with name {0}\n\n", project.ProjectName);

            IDictionary<string, object> payload;
            try
            {
                var accessToken = Request.Headers["Bearer"];
                payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            try
            {
                Guid designerId = Guid.Parse(payload["userId"].ToString());
                Project createdProject = await _projectService.CreateProject(project, designerId);
                return Created("/", createdProject);
            }
            catch (ApiException e)
            {
                if (e.StatusCode == 400)
                    return BadRequest(new BadRequestError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        [HttpPatch]
        [Route("{id}/data")]
        public async Task<IActionResult> UpdateProjectData(Guid id, [FromBody] ProjectUpdateFieldsModel updatedFields)
        {
            _logger.LogInformation("Update project data request for project with id {0}\n\n", id.ToString());

            IDictionary<string, object> payload;
            try
            {
                var accessToken = Request.Headers["Bearer"];
                payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            try
            {
                Guid designerId = Guid.Parse(payload["userId"].ToString());
                Project project = await _projectService.UpdateProjectData(id, updatedFields, designerId);
                return Ok(project);
            }
            catch(ApiException e)
            {
                if (e.StatusCode == 404)
                    return NotFound(new NotFoundError(e.Message));
                if (e.StatusCode == 401)
                    return Unauthorized(new UnauthorizedError(e.Message));
                if (e.StatusCode == 400)
                    return BadRequest(new BadRequestError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        [HttpPatch]
        [Route("{id}/files")]
        public async Task<IActionResult> UpdateProjectFiles(Guid id, [FromForm] IFormFile ProjectArchive)
        {
            _logger.LogInformation("Update project files request for project with id {0}\n\n", id.ToString());

            IDictionary<string, object> payload;
            try
            {
                var accessToken = Request.Headers["Bearer"];
                payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            try
            {
                Guid designerId = Guid.Parse(payload["userId"].ToString());
                Project project = await _projectService.UpdateProjectFiles(id, designerId, ProjectArchive);
                return Ok(project);
            }
            catch(ApiException e)
            {
                if (e.StatusCode == 404)
                    return NotFound(new NotFoundError(e.Message));
                if (e.StatusCode == 401)
                    return Unauthorized(new UnauthorizedError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        [HttpGet]
        [Route("{id}/files")]
        public async Task<IActionResult> GetProjectFiles(Guid id)
        {
            _logger.LogInformation("Get project files request for project with id {0}\n\n", id.ToString());

            IDictionary<string, object> payload;
            try
            {
                var accessToken = Request.Headers["Bearer"];
                payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            try
            {
                Guid requesterId = Guid.Parse(payload["userId"].ToString());
                byte[] fileBytes = await _projectService.GetProjectFilesById(id, requesterId);
                return File(fileBytes, "application/octet-stream");
            }
            catch (ApiException e)
            {
                if (e.StatusCode == 404)
                    return NotFound(new NotFoundError(e.Message));
                if (e.StatusCode == 401)
                    return Unauthorized(new UnauthorizedError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        private IDictionary<string, object> Authorize(string accessToken)
        {
            if (String.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Missing or bad authentication\n\n");
                throw new ApiException(401, "Missing or bad authentication");
            }

            try
            {
                return _authenticationService.ValidateJWT(accessToken);
            }
            catch (TokenExpiredException)
            {
                throw new ApiException(401, "Token has expired");
            }
            catch (SignatureVerificationException)
            {
                throw new ApiException(401, "Token has invalid signature");
            }
            catch (InvalidTokenPartsException)
            {
                throw new ApiException(401, "Token has invalid format");
            }
        }
    }
}
