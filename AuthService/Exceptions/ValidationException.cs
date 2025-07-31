namespace AuthService.Exceptions
{
    public class ValidationException : ApiException
    {
        public IDictionary<string, string[]> Errors {  get; } 

        public ValidationException(string message, IDictionary<string, string[]> errors) : base(message, StatusCodes.Status400BadRequest, "VALIDATION_ERROR")
        {
            
        }
    }
}
