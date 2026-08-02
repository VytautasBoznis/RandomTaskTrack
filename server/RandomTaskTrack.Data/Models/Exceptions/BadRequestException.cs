using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class BadRequestException : BaseAppException
{
    public BadRequestException(string message, string errorCode, string description = "") : base(message, description, HttpStatusCode.BadRequest, errorCode)
    {
    }
}
