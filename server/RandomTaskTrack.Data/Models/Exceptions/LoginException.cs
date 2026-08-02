using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class LoginException : BaseAppException
{
    public LoginException(string message, string errorCode) : base(message, statusCode: HttpStatusCode.Unauthorized, errorCode: errorCode)
    {
    }
}
