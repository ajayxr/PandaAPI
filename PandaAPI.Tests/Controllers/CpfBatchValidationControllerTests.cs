using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PandaAPI.Controllers;
using Xunit;

namespace PandaAPI.Tests.Controllers;

public class CpfBatchValidationControllerTests
{
    [Fact]
    public void Controller_HasBatchRateLimitingAndAuthorization()
    {
        var attributes = typeof(CpfBatchValidationController).GetCustomAttributes(true);
        var methodAttributes = typeof(CpfBatchValidationController)
            .GetMethod(nameof(CpfBatchValidationController.ValidateBatch))!
            .GetCustomAttributes(true);

        var authorizeAttribute = attributes.OfType<AuthorizeAttribute>().SingleOrDefault();
        var rateLimitAttribute = attributes.OfType<EnableRateLimitingAttribute>().SingleOrDefault();
        var consumesAttribute = methodAttributes.OfType<ConsumesAttribute>().SingleOrDefault();
        var requestSizeLimitAttribute = methodAttributes.OfType<RequestSizeLimitAttribute>().SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.NotNull(rateLimitAttribute);
        Assert.NotNull(consumesAttribute);
        Assert.NotNull(requestSizeLimitAttribute);
        Assert.Equal("cpf-batch", rateLimitAttribute!.PolicyName);
        Assert.Equal("multipart/form-data", consumesAttribute!.ContentTypes.Single());
    }
}
