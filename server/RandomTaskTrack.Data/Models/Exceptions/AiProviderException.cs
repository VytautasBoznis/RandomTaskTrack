using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class AiProviderException : BaseAppException
{
    public AiProviderException(string message, string errorCode, string description = "") : base(message, description, HttpStatusCode.BadGateway, errorCode)
    {
    }
}
