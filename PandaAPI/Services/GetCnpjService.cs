using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PandaAPI.Configuration;

namespace PandaAPI.Services
{
    public class GetCnpjService(HttpClient httpClient, IOptions<CnpjAiOptions> options)
    {
        public async Task<JsonElement> GetCnpjAsync(string cnpj, CancellationToken cancellationToken)
        {
            var normalizedCnpj = CnpjValidator.Normalize(cnpj);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"estabelecimentos/{Uri.EscapeDataString(normalizedCnpj)}/basic");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                options.Value.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        }
    }
}
