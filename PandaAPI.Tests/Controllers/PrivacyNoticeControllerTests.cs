using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PandaAPI.Controllers;
using Xunit;

namespace PandaAPI.Tests.Controllers;

public class PrivacyNoticeControllerTests
{
    [Fact]
    public void Get_ReturnsPrivacyNoticeAsMarkdownWithoutAuthentication()
    {
        var controller = new PrivacyNoticeController();

        var result = Assert.IsType<ContentResult>(controller.Get());

        Assert.NotNull(typeof(PrivacyNoticeController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.Equal("text/markdown; charset=utf-8", result.ContentType);
        Assert.Contains("Direitos dos titulares", result.Content);
        Assert.Contains("suportepandaapi@gmail.com", result.Content);
    }
}
