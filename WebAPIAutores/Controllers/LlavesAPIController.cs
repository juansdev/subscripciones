using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPIAutores.DTOs;
using WebAPIAutores.Servicios;

namespace WebAPIAutores.Controllers;

[ApiController]
[Route("api/llavesapi")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class LlavesAPIController: CustomBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ServicioLlaves _servicioLlaves;

    public LlavesAPIController(ApplicationDbContext context, IMapper mapper, ServicioLlaves servicioLlaves)
    {
        _context = context;
        _mapper = mapper;
        _servicioLlaves = servicioLlaves;
    }

    [HttpGet]
    public async Task<List<LlaveDto>> MisLlaves()
    {
        var usuarioId = ObtenerUsuarioId();
        var llaves = await _context.LlavesAPI.Where(x => x.UsuarioId == usuarioId).ToListAsync();
        return _mapper.Map<List<LlaveDto>>(llaves);
    }

    [HttpPost]
    public async Task<ActionResult> CrearLlave(CrearLlaveDto crearLlaveDto)
    {
        var usuarioId = ObtenerUsuarioId();
        if (crearLlaveDto.TipoLlave == Entidades.TipoLlave.Gratuita)
        {
            var elUsuarioYaTieneUnaLlaveGratuita = await _context.LlavesAPI.AnyAsync(x =>
                x.UsuarioId == usuarioId && x.TipoLlave == Entidades.TipoLlave.Gratuita);
            if (elUsuarioYaTieneUnaLlaveGratuita)
            {
                return BadRequest("El usuario ya tiene una llave gratuita");
            }
        }

        await _servicioLlaves.CrearLlave(usuarioId, crearLlaveDto.TipoLlave);
        return NoContent();
    }

    [HttpPut]
    public async Task<ActionResult> ActualizarLlave(ActualizarLlaveDto actualizarLlaveDto)
    {
        var usuarioId = ObtenerUsuarioId();
        var llaveDB = await _context.LlavesAPI.FirstOrDefaultAsync(x=>x.Id==actualizarLlaveDto.LlaveId);
        if (llaveDB == null)
        {
            return NotFound();
        }

        if (usuarioId != llaveDB.UsuarioId)
        {
            return Forbid();
        }

        if (actualizarLlaveDto.ActualizarLlave)
        {
            llaveDB.Llave = _servicioLlaves.GenerarLlave();
        }

        llaveDB.Activa = actualizarLlaveDto.Activa;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}