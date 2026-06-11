using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RestaurantOS.Application.Exceptions;

namespace RestaurantOS.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API hatası: {Path}", context.Request.Path);
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            BusinessException be => (HttpStatusCode.BadRequest, be.Message),
            ConcurrencyException => (HttpStatusCode.Conflict, "Kayıt başka biri tarafından güncellendi. Sayfayı yenileyin."),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Kayıt başka biri tarafından güncellendi. Lütfen tekrar deneyin."),
            UnauthorizedAccessException ue => (HttpStatusCode.Unauthorized, ue.Message),
            _ => (HttpStatusCode.InternalServerError, "Sunucu hatası oluştu.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
