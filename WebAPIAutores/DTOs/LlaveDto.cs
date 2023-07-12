using System.Collections.Generic;

namespace WebAPIAutores.DTOs;

public class LlaveDto
{
    public int Id { get; set; }
    public string Llave { get; set; }
    public bool Activa { get; set; }
    public string TipoLlave { get; set; }
    public List<RestriccionDominioDto> RestriccionesDominio { get; set; }
}