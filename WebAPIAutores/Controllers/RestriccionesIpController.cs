using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPIAutores.DTOs;
using WebAPIAutores.Entidades;

namespace WebAPIAutores.Controllers;

[ApiController]
[Route("api/restriccionesip")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class RestriccionesIpController : CustomBaseController
{
    private readonly ApplicationDbContext _context;

    public RestriccionesIpController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult> Post(CrearRestriccionIPDto crearRestriccionesDominioDto)
    {
        var llaveDb = await _context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == crearRestriccionesDominioDto.LlaveId);
        if (llaveDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (llaveDb.UsuarioId != usuarioId) return Forbid();

        var restriccionIp = new RestriccionIP
        {
            LlaveId = llaveDb.Id,
            IP = crearRestriccionesDominioDto.IP
        };

        _context.Add(restriccionIp);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, ActualizarRestriccionIPDto actualizarRestriccionIpDto)
    {
        var restriccionDb = await _context.RestriccionesIps.Include(x => x.Llave).FirstOrDefaultAsync(x => x.Id == id);
        if (restriccionDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (restriccionDb.Llave.UsuarioId != usuarioId) return Forbid();

        restriccionDb.IP = actualizarRestriccionIpDto.IP;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletE(int id)
    {
        var restriccionDb = await _context.RestriccionesIps.Include(x => x.Llave).FirstOrDefaultAsync(x => x.Id == id);
        if (restriccionDb == null) return NotFound();

        var usuarioId = ObtenerUsuarioId();
        if (usuarioId != restriccionDb.Llave.UsuarioId) return Forbid();

        _context.Remove(restriccionDb);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}