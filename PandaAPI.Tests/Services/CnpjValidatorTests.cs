using PandaAPI.Services;
using Xunit;

namespace PandaAPI.Tests.Services;

public class CnpjValidatorTests
{
    [Theory]
    [InlineData("37318313000100")]
    [InlineData("37.318.313/0001-00")]
    [InlineData("37.318.313%2F0001-00")]
    [InlineData("12ABC34501DE35")]
    [InlineData("12abc34501de35")]
    public void IsValid_ReturnsTrueForValidNumericAndAlphanumericCnpj(string cnpj)
    {
        Assert.True(CnpjValidator.IsValid(cnpj));
    }

    [Theory]
    [InlineData("37318313000101")]
    [InlineData("12ABC34501DE36")]
    [InlineData("11111111111111")]
    [InlineData("3731831300010")]
    [InlineData("12ABC34501DEA5")]
    [InlineData("12ABC34501DE3$")]
    public void IsValid_ReturnsFalseForInvalidCnpj(string cnpj)
    {
        Assert.False(CnpjValidator.IsValid(cnpj));
    }

    [Theory]
    [InlineData("37.318.313/0001-00", "37318313000100")]
    [InlineData("37.318.313%2F0001-00", "37318313000100")]
    [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
    public void Normalize_RemovesFormattingDecodesAndUppercases(string cnpj, string expected)
    {
        Assert.Equal(expected, CnpjValidator.Normalize(cnpj));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalseForNullOrWhitespace(string? cnpj)
    {
        Assert.False(CnpjValidator.IsValid(cnpj!));
    }
}
