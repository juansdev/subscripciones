using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPIAutores.DTOs;

namespace WebAPIAutores.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FacturasController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult> Pagar(PagarFacturaDto pagarFacturaDto)
    {
        var facturaDb = await _context.Facturas
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == pagarFacturaDto.FacturaId);

        if (facturaDb == null) return NotFound();

        if (facturaDb.Pagada) return BadRequest("La factura ya fue saldada");
        // Logica para pagar la factura
        // Nosotros vamos a pretender que el pago fue exitoso ;v
        facturaDb.Pagada = true;
        await _context.SaveChangesAsync();

        var hayFacturasPendientesVencidas = await _context.Facturas.AnyAsync(x =>
            x.UsuarioId == facturaDb.UsuarioId && !x.Pagada && x.FechaLimiteDePago < DateTime.Today);
        if (!hayFacturasPendientesVencidas)
        {
            facturaDb.Usuario.MalaPaga = false;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }
}