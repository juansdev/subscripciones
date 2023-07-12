using System.ComponentModel.DataAnnotations;

namespace WebAPIAutores.DTOs;

public class ActualizarRestriccionDominioDto
{
    [Required] public string Dominio { get; set; }
}