using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using WebAPIAutores.DTOs;

namespace WebAPIAutores.Middlewares;

public static class LimitarPeticionesMiddlewareExtensions
{
    public static IApplicationBuilder UseLimitarPeticiones(this IApplicationBuilder app)
    {
        return app.UseMiddleware<LimitarPeticionesMiddleware>();
    }
}

public class LimitarPeticionesMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly IConfiguration _configuration;
    
    public LimitarPeticionesMiddleware(RequestDelegate siguiente, IConfiguration configuration)
    {
        _siguiente = siguiente;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext httpContext, ApplicationDbContext context)
    {
        var limitarPeticionesConfiguracion = new LimitarPeticionesConfiguracion();
        _configuration.GetRequiredSection("limitarPeticiones").Bind(limitarPeticionesConfiguracion);

        var llaveStringValues = httpContext.Request.Headers["X-Api-Key"];

        if (llaveStringValues.Count == 0)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("Debe proveer la llave en la cabecera X-Api-Key");
            return;
        }

        await _siguiente(httpContext);
    }
}