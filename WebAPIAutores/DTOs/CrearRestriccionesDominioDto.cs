using System.ComponentModel.DataAnnotations;

namespace WebAPIAutores.DTOs;

public class CrearRestriccionesDominioDto
{
    public int LlaveId { get; set; }
    [Required] public string Dominio { get; set; }
}