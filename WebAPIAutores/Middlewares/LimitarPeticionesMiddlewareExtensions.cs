using System;
using System.Collections.Generic;
using System.Linq;
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

        var ruta = httpContext.Request.Path.ToString();
        var estaLaRutaEnListaBlanca = limitarPeticionesConfiguracion.ListaBlancaRutas.Any(x => ruta.Contains(x));

        if (estaLaRutaEnListaBlanca)
        {
            await _siguiente(httpContext);
            return;
        }

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

        var llaveDb = await context.LlavesAPI
            .Include(x => x.RestriccionesDominio)
            .Include(x => x.RestriccionesIP)
            .FirstOrDefaultAsync(x => x.Llave == llave);

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

        var superaRestricciones = PeticionSuperaAlgunaDeLasRestricciones(llaveDb, httpContext);

        if (!superaRestricciones)
        {
            httpContext.Response.StatusCode = 403;
            return;
        }

        var peticion = new Peticion { LlaveId = llaveDb.Id, FechaPeticion = DateTime.UtcNow };
        context.Add(peticion);
        await context.SaveChangesAsync();

        await _siguiente(httpContext);
    }

    private bool PeticionSuperaAlgunaDeLasRestricciones(LlaveAPI llaveApi, HttpContext httpContext)
    {
        var hayRestricciones = llaveApi.RestriccionesDominio.Any() || llaveApi.RestriccionesIP.Any();
        if (!hayRestricciones) return true;

        var peticionSuperaLasRestriccionesDeDominio =
            PeticionSuperaLasRestriccionesDeDominio(llaveApi.RestriccionesDominio, httpContext);

        return peticionSuperaLasRestriccionesDeDominio;
    }

    private bool PeticionSuperaLasRestriccionesDeDominio(List<RestriccionDominio> restricciones,
        HttpContext httpContext)
    {
        if (restricciones == null || restricciones.Count == 0) return false;

        var referer = httpContext.Request.Headers["Referer"].ToString();
        if (referer == string.Empty) return false;

        var myUri = new Uri(referer);
        var host = myUri.Host;

        var superaRestriccion = restricciones.Any(x => x.Dominio == host);
        return superaRestriccion;
    }
}