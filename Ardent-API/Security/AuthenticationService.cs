using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;

namespace Ardent_API.Security
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILogger<AuthenticationService> _logger;
        private const string Secret = "THIS IS USED TO SIGN AND VERIFY JWT TOKENS. IT CAN BE ANY STRING";

        public AuthenticationService(ILogger<AuthenticationService> logger)
        {
            _logger = logger;
        }

        public JWT GenerateJWT(Guid userId, string username)
        {
            var token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Secret)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds())
                .AddClaim("userId", userId)
                .Encode();

            return new JWT(token, userId, username);
        }

        public IDictionary<string, object> ValidateJWT(string token)
        {
            try
            {
                IDictionary<string, object> payload = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(Secret)
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(token);

                return payload;
            }
            catch (TokenExpiredException e)
            {
                _logger.LogError("Token has expired\n\n");
                throw e;
            }
            catch (SignatureVerificationException e)
            {
                _logger.LogError("Token has invalid signature\n\n");
                throw e;
            }
            catch (InvalidTokenPartsException e)
            {
                _logger.LogError("Token has invalid format\n\n");
                throw e;
            }
        }
    }
}
