using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class UnauthorizedException : BaseAppException
{
    public UnauthorizedException(string message, string description = "", HttpStatusCode statusCode = HttpStatusCode.Unauthorized, string errorCode = "") : base(message, description, statusCode, errorCode)
    {
    }
}
