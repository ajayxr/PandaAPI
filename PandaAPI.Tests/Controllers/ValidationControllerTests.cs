using Microsoft.AspNetCore.Mvc;
using PandaAPI.Controllers;
using PandaAPI.DTOs.Validation;
using Xunit;

namespace PandaAPI.Tests.Controllers;

public class ValidationControllerTests
{
    [Fact]
    public void ValidateCpf_DoesNotReturnCpf()
    {
        var controller = new ValidationController();

        var result = controller.ValidateCpf(new ValidateCpfDto { Cpf = "529.982.247-25" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("CPF válido.", okResult.Value!.GetType().GetProperty("Message")!.GetValue(okResult.Value));
        Assert.Null(okResult.Value.GetType().GetProperty("Cpf"));
    }
}
