using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPIAutores.DTOs;
using WebAPIAutores.Entidades;

namespace WebAPIAutores.Controllers;

[ApiController]
[Route("api/restriccionesdominio")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RestriccionesDominioController : CustomBaseController
{
    private readonly ApplicationDbContext _context;

    public RestriccionesDominioController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult> Post(CrearRestriccionesDominioDto crearRestriccionesDominioDto)
    {
        var llaveDb = await _context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == crearRestriccionesDominioDto.LlaveId);
        if (llaveDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (llaveDb.UsuarioId != usuarioId) return Forbid();

        var restriccionDominio = new RestriccionDominio
        {
            LlaveId = crearRestriccionesDominioDto.LlaveId,
            Dominio = crearRestriccionesDominioDto.Dominio
        };

        _context.Add(restriccionDominio);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, ActualizarRestriccionDominioDto actualizarRestriccionDominioDto)
    {
        var restriccionDb =
            await _context.RestriccionesDominio.Include(x => x.Llave).FirstOrDefaultAsync(x => x.Id == id);
        if (restriccionDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (restriccionDb.Llave.UsuarioId != usuarioId) return Forbid();

        restriccionDb.Dominio = actualizarRestriccionDominioDto.Dominio;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var restriccionDb =
            await _context.RestriccionesDominio.Include(x => x.Llave).FirstOrDefaultAsync(x => x.Id == id);
        if (restriccionDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (usuarioId != restriccionDb.Llave.UsuarioId) return Forbid();

        _context.Remove(restriccionDb);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}