using Ardent_API.Errors;
using Ardent_API.Models;
using Ardent_API.Repositories;
using Ardent_API.Security;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ardent_API.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserRepository _userRepository;

        public UserService(ILogger<UserService> logger, UserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<User> CreateUser(User newUser)
        {
            if(_userRepository.GetUserByUsername(newUser.Username).Result != null)
            {
                _logger.LogError("Username {0} already in the database\n\n", newUser.Username);
                throw new ApiException(400, "Username " + newUser.Username + " already in database");
            }
            newUser.PasswordHash = Hasher.HashString(newUser.PasswordHash);

            User createdUser = await _userRepository.CreateUser(newUser);
            if(createdUser == null)
                throw new ApiException(500, "User could not be created");

            return createdUser;
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _userRepository.GetAllUsers();
        }

        public User ValidateCredentials(AuthenticationModel authenticationData)
        {
            User storedUser = _userRepository.GetUserByUsername(authenticationData.Username).Result;
            string hashedPassword = Hasher.HashString(authenticationData.PasswordPlain);

            if (storedUser == null || !(hashedPassword.Equals(storedUser.PasswordHash)))
            {
                _logger.LogError("Invalid credentials\n\n");
                throw new ApiException(400, "Invalid credentials");
            }


            return storedUser;
        }
    }
}
