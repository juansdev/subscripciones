using System.ComponentModel.DataAnnotations;

namespace WebAPIAutores.DTOs;

public class ActualizarRestriccionIPDto
{
    [Required] public string IP { get; set; }
}