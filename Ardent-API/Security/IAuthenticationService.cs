using System;
using System.Collections.Generic;

namespace Ardent_API.Security
{
    public interface IAuthenticationService
    {
        JWT GenerateJWT(Guid userId, string username);
        IDictionary<string, object> ValidateJWT(string token);
    }
}
