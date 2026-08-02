using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class BaseAppException : Exception
{
    public BaseAppException(string message, string description = "", HttpStatusCode statusCode = HttpStatusCode.InternalServerError, string errorCode = "") : base(message)
    {
        StatusCode = statusCode;
        Description = description;
        ErrorCode = errorCode;
    }

    public HttpStatusCode StatusCode { get; set; }
    public string Description { get; set; }
    public string ErrorCode { get; set; }
}
