using System.ComponentModel.DataAnnotations;

namespace WebAPIAutores.DTOs;

public class CrearRestriccionIPDto
{
    public int LlaveId { get; set; }
    [Required] public string IP { get; set; }
}