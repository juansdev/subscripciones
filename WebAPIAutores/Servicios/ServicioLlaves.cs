using System;
using System.Threading.Tasks;
using WebAPIAutores.Entidades;

namespace WebAPIAutores.Servicios;

public class ServicioLlaves
{
    private readonly ApplicationDbContext _context;

    public ServicioLlaves(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CrearLlave(string usuarioId, TipoLlave tipoLlave)
    {
        var llave = Guid.NewGuid().ToString().Replace("-", "");
        var llaveAPI = new LlaveAPI()
        {
            Activa = true,
            Llave = llave,
            TipoLlave = tipoLlave,
            UsuarioId = usuarioId
        };
        _context.Add(llaveAPI);
        await _context.SaveChangesAsync();
    }
}