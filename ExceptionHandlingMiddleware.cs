using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Proceed with the request
            await _next(context);
        }
        catch (Exception ex)
        {
            // Handle exceptions and return a friendly error response
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception
        Console.WriteLine($"Exception caught by middleware: {exception.Message}");

        // Ensure the response hasn't been started
        if (!context.Response.HasStarted)
        {
            context.Response.Clear(); // Clear any existing response
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "An internal server error occurred. Please try again later.",
                Detailed = exception.Message // Only for development; remove in production
            };

            try
            {
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Stream was already disposed while handling the exception.");
            }
        }
        else
        {
            // Log and avoid writing to response if it's already started
            Console.WriteLine("The response has already started, unable to write the error response.");
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
