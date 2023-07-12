﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebAPIAutores.DTOs;
using WebAPIAutores.Entidades;

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
    private readonly IConfiguration _configuration;
    private readonly RequestDelegate _siguiente;

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

        if (llaveStringValues.Count > 1)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("Solo una llave debe de estar presente");
            return;
        }

        var llave = llaveStringValues[0];

        var llaveDb = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Llave == llave);

        if (llaveDb == null)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("La llave no existe");
            return;
        }

        if (!llaveDb.Activa)
        {
            httpContext.Response.StatusCode = 400;
            await httpContext.Response.WriteAsync("La llave se encuentra inactiva");
            return;
        }

        if (llaveDb.TipoLlave == TipoLlave.Gratuita)
        {
            var hoy = DateTime.Today;
            var mañana = hoy.AddDays(1);
            var cantidadPeticionesRealizadasHoy = await context.Peticiones.CountAsync(x =>
                x.LlaveId == llaveDb.Id && x.FechaPeticion >= hoy && x.FechaPeticion < mañana);
            if (cantidadPeticionesRealizadasHoy >= limitarPeticionesConfiguracion.PeticionesPorDiaGratuito)
            {
                httpContext.Response.StatusCode = 429; //Too many requests
                await httpContext.Response.WriteAsync(
                    "Ha excedido el límite de peticiones por día. Si desea realizar más peticiones, actualice su suscripción a una cuenta profesional");
                return;
            }
        }

        var peticion = new Peticion { LlaveId = llaveDb.Id, FechaPeticion = DateTime.UtcNow };
        context.Add(peticion);
        await context.SaveChangesAsync();

        await _siguiente(httpContext);
    }
}