using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RandomTaskTrack.Data.Models.Exceptions;
using Serilog;

namespace RandomTaskTrack.API.ActionFilters;

public class ErrorHandlingFilterAttribute : IExceptionFilter
{
    private const string DEFAULT_ERROR_CODE = "UNHANDLED_EXCEPTION";

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        ApiError apiError = new ApiError();
        int statusCode = (int)HttpStatusCode.InternalServerError;

        switch (exception)
        {
            case BaseAppException e:
                apiError.Message = e.Message;
                apiError.ErrorCode = e.ErrorCode;
                apiError.Description = e.Description;
                statusCode = (int)e.StatusCode;
                break;
            case KeyNotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                break;
            default:
                apiError.Message = exception.Message;
                statusCode = (int)HttpStatusCode.InternalServerError;
                apiError.ErrorCode = DEFAULT_ERROR_CODE;
                break;
        }

        Log.Error($"Original error message: {exception.Message} \n\n stacktrace: {exception.StackTrace}");

        context.Result = new ObjectResult(apiError)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }
}
