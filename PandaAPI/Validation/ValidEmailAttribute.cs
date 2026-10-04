using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace PandaAPI.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValidEmailAttribute : ValidationAttribute
{
    public ValidEmailAttribute()
    {
        ErrorMessage = "Informe um endereço de email válido.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string email || string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            var parsedEmail = new MailAddress(email);
            return string.Equals(parsedEmail.Address, email, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}