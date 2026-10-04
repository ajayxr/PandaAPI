using PandaAPI.Services;
using Xunit;

namespace PandaAPI.Tests.Services;

public class CpfValidatorTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void IsValid_ReturnsTrueForValidCpf(string cpf)
    {
        Assert.True(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("5299822472")]
    [InlineData("5299822472A")]
    public void IsValid_ReturnsFalseForInvalidCpf(string cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalseForNullOrWhitespace(string? cpf)
    {
        Assert.False(CpfValidator.IsValid(cpf!));
    }

    [Fact]
    public void Normalize_RemovesFormatting()
    {
        Assert.Equal("52998224725", CpfValidator.Normalize("529.982.247-25"));
    }

    [Fact]
    public void Generate_ReturnsValidCpf()
    {
        for (var i = 0; i < 25; i++)
        {
            var cpf = CpfValidator.Generate();

            Assert.Equal(11, cpf.Length);
            Assert.True(CpfValidator.IsValid(cpf));
        }
    }
}
