namespace AuthService.Exceptions
{
    public class BadRequestException : ApiException
    {
        public BadRequestException(string message) : base(message, StatusCodes.Status400BadRequest, "BAD_REQUEST")
        {
        }
    }
}
