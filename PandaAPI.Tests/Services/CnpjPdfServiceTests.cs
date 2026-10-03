using System.Text;
using System.Text.Json;
using PandaAPI.Services;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using Xunit;

namespace PandaAPI.Tests.Services;

public class CnpjPdfServiceTests
{
    static CnpjPdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var assembly = typeof(CnpjPdfService).Assembly;
        foreach (var resourceName in new[]
                 {
                     "PandaAPI.Assets.Fonts.Arimo-Regular.ttf",
                     "PandaAPI.Assets.Fonts.Arimo-Italic.ttf"
                 })
        {
            using var fontStream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Fonte incorporada não encontrada: {resourceName}");
            FontManager.RegisterFont(fontStream);
        }
    }

    [Fact]
    public void GenerateReport_WithProviderData_ReturnsPdfDocument()
    {
        using var data = JsonDocument.Parse(
            "{\"dados\":{\"estabelecimento\":{\"cnpj\":\"37318313000100\",\"nome_fantasia\":\"Empresa Exemplo\",\"endereco\":{\"municipio\":\"São Paulo\",\"uf\":\"SP\"}}}}");
        var service = new CnpjPdfService();

        var pdf = service.GenerateReport(data.RootElement, "37318313000100");

        Assert.True(pdf.Length > 1000);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf));
    }

    [Fact]
    public void GenerateReport_WithoutFields_ReturnsPdfDocument()
    {
        using var data = JsonDocument.Parse("{}");
        var service = new CnpjPdfService();

        var pdf = service.GenerateReport(data.RootElement, "37318313000100");

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf));
    }
}
