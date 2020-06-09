using System;
using System.Collections.Generic;
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
            _logger.LogInformation("Upload request from user with id {0} for a new project with name {1}\n\n", 
                project.DesignerId, project.ProjectName);

            try
            {
                var accessToken = Request.Headers["Bearer"];
                var payload = Authorize(accessToken);
            }
            catch(ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            try
            {
                Project createdProject = await _projectService.CreateProject(project);
                return Created("/", createdProject);
            }
            catch(ApiException e)
            {
                if (e.StatusCode == 400)
                    return BadRequest(new BadRequestError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProjects()
        {
            _logger.LogInformation("GET request for all projects\n\n");

            try
            {
                var accessToken = Request.Headers["Bearer"];
                var payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            return Ok();
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
