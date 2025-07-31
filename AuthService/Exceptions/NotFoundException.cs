namespace AuthService.Exceptions
{
    public class NotFoundException : ApiException
    {
        public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound, "NOT_FOUND")
        {
        }
    }
}
