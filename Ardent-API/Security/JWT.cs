using System;

namespace Ardent_API.Security
{
    public class JWT
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }

        public JWT(string token, Guid id, string username)
        {
            Id = id;
            Username = username;
            Token = token;
        }
    }
}
