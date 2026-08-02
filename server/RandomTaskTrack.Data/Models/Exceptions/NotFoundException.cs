using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class NotFoundException : BaseAppException
{
    public NotFoundException(string message, string errorCode) : base(message, statusCode: HttpStatusCode.NotFound, errorCode: errorCode)
    {
    }
}
