using System.Net;

namespace RandomTaskTrack.Data.Models.Exceptions;

public class RecipeSourceException : BaseAppException
{
    public RecipeSourceException(string message, string errorCode, string description = "") : base(message, description, HttpStatusCode.BadGateway, errorCode)
    {
    }
}
