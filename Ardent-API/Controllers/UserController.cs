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
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IAuthenticationService _authenticationService;
        private readonly UserService _userService;

        public UserController(ILogger<UserController> logger, IAuthenticationService authenticationService, UserService userService)
        {
            _logger = logger;
            _authenticationService = authenticationService;
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            _logger.LogInformation("POST request for saving user with Username {0}\n\n", user.Username);

            try
            {
                User createdUser = await _userService.CreateUser(user);
                return Created("/", createdUser);
            }
            catch(ApiException e)
            {
                if (e.StatusCode == 400)
                    return BadRequest(new BadRequestError(e.Message));

                return StatusCode(StatusCodes.Status500InternalServerError, new InternalServerError(e.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("GET request for all users\n\n");

            try
            {
                var accessToken = Request.Headers["Bearer"];
                var payload = Authorize(accessToken);
            }
            catch (ApiException e)
            {
                return Unauthorized(new UnauthorizedError(e.Message));
            }

            List<User> allUsers = await _userService.GetAllUsers();
            return Ok(allUsers);
        }

        [HttpPost]
        [Route("/login")]
        public IActionResult Login([FromBody] AuthenticationModel authenticationData)
        {
            _logger.LogInformation("POST request for login with Username {0}\n\n", authenticationData.Username);

            try
            {
                User validUser = _userService.ValidateCredentials(authenticationData);
                Security.JWT jwt = _authenticationService.GenerateJWT(validUser.Id, validUser.Username);
                return Ok(jwt);
            }
            catch(ApiException e)
            {
                return BadRequest(new BadRequestError(e.Message));
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
