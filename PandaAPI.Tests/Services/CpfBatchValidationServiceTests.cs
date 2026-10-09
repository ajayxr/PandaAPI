using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PandaAPI.Services;
using Xunit;

namespace PandaAPI.Tests.Services;

public class CpfBatchValidationServiceTests
{
    [Fact]
    public async Task ProcessAsync_Csv_AddsValidityColumnAfterCpf()
    {
        var csv = "Nome,CPF,Cidade\nAna,529.982.247-25,São Paulo\nBob,11111111111,Recife\n";
        var file = CreateFormFile("entrada.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
        var service = CreateService();

        var result = await service.ProcessAsync(file, CancellationToken.None);

        Assert.Equal("text/csv; charset=utf-8", result.ContentType);
        Assert.EndsWith("-processado.csv", result.FileName);

        var output = Encoding.UTF8.GetString(result.Content);
        using var reader = new StringReader(output);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            DetectDelimiter = true
        });

        Assert.True(csvReader.Read());
        csvReader.ReadHeader();
        Assert.Equal(new[] { "Nome", "CPF", "Validade CPF", "Cidade" }, csvReader.HeaderRecord);

        Assert.True(csvReader.Read());
        Assert.Equal("Ana", csvReader.GetField(0));
        Assert.Equal("529.982.247-25", csvReader.GetField(1));
        Assert.Equal("Válido", csvReader.GetField(2));
        Assert.Equal("São Paulo", csvReader.GetField(3));

        Assert.True(csvReader.Read());
        Assert.Equal("Bob", csvReader.GetField(0));
        Assert.Equal("11111111111", csvReader.GetField(1));
        Assert.Equal("Inválido", csvReader.GetField(2));
        Assert.Equal("Recife", csvReader.GetField(3));
    }

    [Fact]
    public async Task ProcessAsync_Xlsx_AddsValidityColumnAfterCpf()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Planilha1");
        worksheet.Cell(1, 1).Value = "Nome";
        worksheet.Cell(1, 2).Value = "CPF";
        worksheet.Cell(1, 3).Value = "Cidade";
        worksheet.Cell(2, 1).Value = "Ana";
        worksheet.Cell(2, 2).Value = "52998224725";
        worksheet.Cell(2, 3).Value = "São Paulo";
        worksheet.Cell(3, 1).Value = "Bob";
        worksheet.Cell(3, 2).Value = "11111111111";
        worksheet.Cell(3, 3).Value = "Recife";

        using var inputStream = new MemoryStream();
        workbook.SaveAs(inputStream);
        inputStream.Position = 0;

        var file = new FormFile(inputStream, 0, inputStream.Length, "file", "entrada.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        var service = CreateService();

        var result = await service.ProcessAsync(file, CancellationToken.None);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.EndsWith("-processado.xlsx", result.FileName);

        using var outputStream = new MemoryStream(result.Content);
        using var outputWorkbook = new XLWorkbook(outputStream);
        var outputWorksheet = outputWorkbook.Worksheet(1);

        Assert.Equal("Nome", outputWorksheet.Cell(1, 1).GetString());
        Assert.Equal("CPF", outputWorksheet.Cell(1, 2).GetString());
        Assert.Equal("Validade CPF", outputWorksheet.Cell(1, 3).GetString());
        Assert.Equal("Cidade", outputWorksheet.Cell(1, 4).GetString());

        Assert.Equal("Ana", outputWorksheet.Cell(2, 1).GetString());
        Assert.Equal("52998224725", outputWorksheet.Cell(2, 2).GetString());
        Assert.Equal("Válido", outputWorksheet.Cell(2, 3).GetString());
        Assert.Equal("São Paulo", outputWorksheet.Cell(2, 4).GetString());

        Assert.Equal("Bob", outputWorksheet.Cell(3, 1).GetString());
        Assert.Equal("11111111111", outputWorksheet.Cell(3, 2).GetString());
        Assert.Equal("Inválido", outputWorksheet.Cell(3, 3).GetString());
        Assert.Equal("Recife", outputWorksheet.Cell(3, 4).GetString());
    }

    [Fact]
    public async Task ProcessAsync_WhenCpfHeaderMissing_ThrowsInvalidDataException()
    {
        var csv = "Nome,Cidade\nAna,São Paulo\n";
        var file = CreateFormFile("entrada.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidDataException>(() => service.ProcessAsync(file, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_WhenFileExtensionUnsupported_ThrowsNotSupportedException()
    {
        var file = CreateFormFile("entrada.txt", "text/plain", Encoding.UTF8.GetBytes("teste"));
        var service = CreateService();

        await Assert.ThrowsAsync<NotSupportedException>(() => service.ProcessAsync(file, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_WhenFileExceedsConfiguredLimit_ThrowsCpfBatchFileTooLargeException()
    {
        var file = CreateFormFile("entrada.csv", "text/csv", Encoding.UTF8.GetBytes("Nome,CPF\nAna,52998224725\n"));
        var service = CreateService(maxFileSizeBytes: 1);

        await Assert.ThrowsAsync<CpfBatchFileTooLargeException>(() => service.ProcessAsync(file, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessAsync_WhenRecordCountExceedsConfiguredLimit_ThrowsInvalidDataException()
    {
        var csv = "Nome,CPF\nAna,52998224725\nBob,11111111111\n";
        var file = CreateFormFile("entrada.csv", "text/csv", Encoding.UTF8.GetBytes(csv));
        var service = CreateService(maxRecords: 1);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => service.ProcessAsync(file, CancellationToken.None));

        Assert.Contains("registros", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static FormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static CpfBatchValidationService CreateService(long maxFileSizeBytes = 5 * 1024 * 1024, int maxRecords = 10_000)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CpfBatchImport:MaxFileSizeBytes"] = maxFileSizeBytes.ToString(CultureInfo.InvariantCulture),
                ["CpfBatchImport:MaxRecords"] = maxRecords.ToString(CultureInfo.InvariantCulture)
            })
            .Build();

        return new CpfBatchValidationService(configuration);
    }
}
