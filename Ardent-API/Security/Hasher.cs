using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ardent_API.Security
{
    public static class Hasher
    {
        public static string HashString(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // plain text to hash
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // return the hashed string  
                return BitConverter.ToString(hashedBytes).Replace("-", "");
            }
        }
    }
}
