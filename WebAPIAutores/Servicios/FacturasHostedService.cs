using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebAPIAutores.Servicios;

public class FacturasHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer timer;

    public FacturasHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(ProcesarFacturas, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer.Dispose();
        return Task.CompletedTask;
    }

    private void ProcesarFacturas(object state)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            SetearMalaPaga(context);
            EmitirFacturas(context);
        }

        ;
    }

    private static void SetearMalaPaga(ApplicationDbContext context)
    {
        context.Database.ExecuteSqlRaw("exec SetearMalaPaga");
    }

    private static void EmitirFacturas(ApplicationDbContext context)
    {
        var hoy = DateTime.Today;
        var fechaComparacion = hoy.AddMonths(-1);
        var facturasDelMesYaFueronEmitidas = context.FacturasEmitidas.Any(x =>
            x.Año == fechaComparacion.Year && x.Mes == fechaComparacion.Month);
        if (!facturasDelMesYaFueronEmitidas)
        {
            var fechaInicio = new DateTime(fechaComparacion.Year, fechaComparacion.Month, 1);
            var fechaFin = fechaInicio.AddMonths(1);
            context.Database.ExecuteSqlInterpolated(
                $"exec CreacionFacturas {fechaInicio.ToString("yyyy-MM-dd")}, {fechaFin.ToString("yyyy-MM-dd")}");
        }
    }
}