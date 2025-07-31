namespace AuthService.Exceptions
{
    public class TooManyRequestsException : ApiException
    {
        public TooManyRequestsException(string message) : base(message, StatusCodes.Status429TooManyRequests, "TOO_MANY_REQUESTS")
        {
        }
    }
}
