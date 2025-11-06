using System.Net;
using FluentValidation;

namespace CampusEats.Api.Infrastructure.Validation;

public sealed class ValidationExceptionMiddleware(RequestDelegate next, ILogger<ValidationExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogInformation("Validation failed: {Errors}",
                string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await context.Response.WriteAsJsonAsync(new
            {
                title = "One or more validation errors occurred.",
                errors
            });
        }
    }
}