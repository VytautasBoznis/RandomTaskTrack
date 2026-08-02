using System.Net;
using RandomTaskTrack.Data.Models.Constants;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class SessionExpiredException : BaseAppException
{
    public SessionExpiredException(string message) : base(message, statusCode: HttpStatusCode.Unauthorized, errorCode: ExceptionCodes.USER_UNAUTHORIZED)
    {
    }
}
