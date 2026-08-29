using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class PriceSourceException : BaseAppException
{
    public PriceSourceException(string message, string errorCode, string description = "") : base(message, description, HttpStatusCode.BadGateway, errorCode)
    {
    }
}
