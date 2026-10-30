using AddressBook.Core.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AddressBook.Api.Middleware;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return true;

        var status = exception switch
        {
            InputException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            BadHttpRequestException bad => bad.StatusCode,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == 500)
            logger.LogError("Unhandled request failure ({ExceptionType}); trace {TraceId}", exception.GetType().Name, context.TraceIdentifier);

        ProblemDetails details = exception is InputException input
            ? new ValidationProblemDetails(new Dictionary<string, string[]> { [input.Field] = [input.Message] })
            : new ProblemDetails();
        details.Status = status;
        details.Title = status switch { 400 => "Invalid request", 404 => "Not found", 409 => "Conflict", 413 => "File too large", _ => "Server error" };
        details.Detail = status == 500 ? "The request could not be completed. Please try again later." : exception.Message;
        details.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        if (!await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details }))
            await context.Response.WriteAsJsonAsync((object)details, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
