using Ardent_API.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ardent_API.Repositories
{
    public class UserRepository
    {
        private readonly ILogger<UserRepository> _logger;
        private readonly DatabaseContext _context;

        public UserRepository(ILogger<UserRepository> logger, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
        }

        public virtual async Task<User> CreateUser(User user) 
        {
            await _context.Users.AddAsync(user);

            var result = await _context.SaveChangesAsync();
            if (result == 0)
            {
                _logger.LogError("Server error! User with Id {0} not saved into database\n\n", user.Id);
                throw new Exception($"Server error! User {user.Id} not saved into database");
            }
            _logger.LogInformation("User with Id {0} saved into database\n\n", user.Id);
            return user;
        }

        public virtual async Task<User> GetUserById(Guid id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public virtual async Task<User> GetUserByUsername(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public virtual async Task<List<User>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }
    }
}
