using System;

namespace Ardent_API.Errors
{
    public class ApiException : Exception
    {
        public int StatusCode { get; private set; }

        public ApiException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
