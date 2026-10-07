using System.ComponentModel.DataAnnotations;

namespace PandaAPI.DTOs.Validation;

public class ValidateCpfDto
{
    [Required]
    [StringLength(14)]
    public string Cpf { get; set; } = string.Empty;
}
