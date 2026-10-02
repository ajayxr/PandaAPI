using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PandaAPI.Configuration;
using PandaAPI.Services;
using Xunit;

namespace PandaAPI.Tests.Services;

public class GetCnpjServiceTests
{
    [Fact]
    public async Task GetCnpjAsync_NormalizesCnpjAndReturnsProviderJson()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"dados\":{\"estabelecimento\":{\"cnpj\":\"37.318.313/0001-00\"}},\"meta\":{\"disponibilidade\":\"disponivel\"}}",
                Encoding.UTF8,
                "application/json")
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cnpj.ai/v2/")
        };
        var service = new GetCnpjService(
            httpClient,
            Options.Create(new CnpjAiOptions { ApiKey = "fake-test-token" }));

        var result = await service.GetCnpjAsync("37.318.313/0001-00", CancellationToken.None);

        Assert.Equal(
            new Uri("https://api.cnpj.ai/v2/estabelecimentos/37318313000100/basic"),
            handler.RequestUri);
        Assert.Equal("Bearer fake-test-token", handler.AuthorizationHeader);
        Assert.Equal(
            "37.318.313/0001-00",
            result.GetProperty("dados").GetProperty("estabelecimento").GetProperty("cnpj").GetString());
    }

    [Fact]
    public async Task GetCnpjAsync_WhenProviderReturnsNotFound_ThrowsWithStatusCode()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.cnpj.ai/v2/")
        };
        var service = new GetCnpjService(
            httpClient,
            Options.Create(new CnpjAiOptions { ApiKey = "fake-test-token" }));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetCnpjAsync("37318313000100", CancellationToken.None));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            AuthorizationHeader = request.Headers.Authorization?.ToString();
            return Task.FromResult(responseFactory(request));
        }
    }
}
