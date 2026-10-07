using System.ComponentModel.DataAnnotations;

namespace PandaAPI.DTOs.Validation;

public class ValidateCnpjDto
{
    [Required]
    [StringLength(18)]
    public string Cnpj { get; set; } = string.Empty;
}
