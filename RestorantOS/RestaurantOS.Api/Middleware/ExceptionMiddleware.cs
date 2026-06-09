using System.Net;
using System.Text.Json;
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
            _logger.LogError(ex, "API hatası");
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            BusinessException be => (HttpStatusCode.BadRequest, be.Message),
            ConcurrencyException => (HttpStatusCode.Conflict, "Kayıt başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyin."),
            UnauthorizedAccessException ue => (HttpStatusCode.Unauthorized, ue.Message),
            _ => (HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
    }
}
