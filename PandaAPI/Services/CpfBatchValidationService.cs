using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PandaAPI.Services;

public sealed class CpfBatchValidationService
{
    private const long DefaultMaxFileSizeBytes = 5 * 1024 * 1024;
    private const int DefaultMaxRecords = 10_000;
    private readonly IConfiguration _configuration;

    public CpfBatchValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<CpfBatchValidationResult> ProcessAsync(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        EnsureFileSizeWithinLimit(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return extension switch
        {
            ".csv" => Task.FromResult(ProcessCsv(file, cancellationToken)),
            ".xlsx" => Task.FromResult(ProcessXlsx(file, cancellationToken)),
            _ => throw new NotSupportedException("Formato de arquivo não suportado. Envie um arquivo CSV ou XLSX.")
        };
    }

    private void EnsureFileSizeWithinLimit(IFormFile file)
    {
        var maxFileSizeBytes = _configuration.GetValue<long?>("CpfBatchImport:MaxFileSizeBytes") ?? DefaultMaxFileSizeBytes;
        if (file.Length > maxFileSizeBytes)
        {
            throw new CpfBatchFileTooLargeException(
                $"Arquivo excede o limite permitido de {maxFileSizeBytes} bytes.");
        }
    }

    private CpfBatchValidationResult ProcessCsv(IFormFile file, CancellationToken cancellationToken)
    {
        using var inputStream = file.OpenReadStream();
        using var reader = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            DetectDelimiter = true
        });

        if (!csv.Read())
        {
            throw new InvalidDataException("Arquivo CSV vazio.");
        }

        csv.ReadHeader();

        var headers = csv.HeaderRecord?.ToArray() ?? throw new InvalidDataException("Cabeçalho CSV inválido.");
        var cpfColumnIndex = GetCpfColumnIndex(headers);
        EnsureNoDuplicateCpfHeader(headers);
        var maxRecords = GetMaxRecords(_configuration);

        var output = new StringBuilder();
        using (var writer = new StringWriter(output, CultureInfo.InvariantCulture))
        using (var outputCsv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            WriteHeaders(outputCsv, headers, cpfColumnIndex);

            var recordCount = 0;
            while (csv.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                recordCount++;
                EnsureRecordCountWithinLimit(recordCount, maxRecords);

                var record = csv.Parser.Record ?? Array.Empty<string>();
                WriteCsvRow(outputCsv, headers.Length, cpfColumnIndex, record);
            }
        }

        return new CpfBatchValidationResult(
            Encoding.UTF8.GetBytes(output.ToString()),
            BuildOutputFileName(file.FileName, ".csv"),
            "text/csv; charset=utf-8");
    }

    private CpfBatchValidationResult ProcessXlsx(IFormFile file, CancellationToken cancellationToken)
    {
        using var inputStream = file.OpenReadStream();
        using var sourceWorkbook = new XLWorkbook(inputStream);
        var sourceWorksheet = sourceWorkbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("Arquivo Excel sem planilhas.");

        var usedRange = sourceWorksheet.RangeUsed();
        if (usedRange is null)
        {
            throw new InvalidDataException("Arquivo Excel vazio.");
        }

        var headerRow = usedRange.FirstRow();
        var headers = headerRow.Cells().Select(cell => cell.GetString()).ToArray();
        var cpfColumnIndex = GetCpfColumnIndex(headers);
        EnsureNoDuplicateCpfHeader(headers);
        var maxRecords = GetMaxRecords(_configuration);

        using var outputWorkbook = new XLWorkbook();
        var outputWorksheet = outputWorkbook.AddWorksheet(sourceWorksheet.Name);

        WriteHeaders(outputWorksheet, headers, cpfColumnIndex);

        var recordCount = 0;
        foreach (var row in usedRange.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            recordCount++;
            EnsureRecordCountWithinLimit(recordCount, maxRecords);

            WriteXlsxRow(outputWorksheet, sourceWorksheet, row.RowNumber(), headers.Length, cpfColumnIndex, row.RowNumber());
        }

        using var outputStream = new MemoryStream();
        outputWorkbook.SaveAs(outputStream);

        return new CpfBatchValidationResult(
            outputStream.ToArray(),
            BuildOutputFileName(file.FileName, ".xlsx"),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static void WriteHeaders(CsvWriter outputCsv, IReadOnlyList<string> headers, int cpfColumnIndex)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            outputCsv.WriteField(headers[i]);

            if (i == cpfColumnIndex)
            {
                outputCsv.WriteField("Validade CPF");
            }
        }

        outputCsv.NextRecord();
    }

    private static void WriteCsvRow(CsvWriter outputCsv, int originalColumnCount, int cpfColumnIndex, IReadOnlyList<string> record)
    {
        for (var i = 0; i < originalColumnCount; i++)
        {
            var value = i < record.Count ? record[i] : string.Empty;
            outputCsv.WriteField(value);

            if (i == cpfColumnIndex)
            {
                outputCsv.WriteField(GetCpfValidity(value));
            }
        }

        outputCsv.NextRecord();
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers, int cpfColumnIndex)
    {
        var outputColumn = 1;

        for (var i = 0; i < headers.Count; i++)
        {
            worksheet.Cell(1, outputColumn).Value = headers[i];
            outputColumn++;

            if (i == cpfColumnIndex)
            {
                worksheet.Cell(1, outputColumn).Value = "Validade CPF";
                outputColumn++;
            }
        }
    }

    private static void WriteXlsxRow(IXLWorksheet worksheet, IXLWorksheet sourceWorksheet, int sourceRowNumber, int originalColumnCount, int cpfColumnIndex, int targetRow)
    {
        var outputColumn = 1;

        for (var i = 0; i < originalColumnCount; i++)
        {
            var sourceCell = sourceWorksheet.Cell(sourceRowNumber, i + 1);
            worksheet.Cell(targetRow, outputColumn).Value = GetCellText(sourceCell);
            outputColumn++;

            if (i == cpfColumnIndex)
            {
                worksheet.Cell(targetRow, outputColumn).Value = GetCpfValidity(GetCellText(sourceCell));
                outputColumn++;
            }
        }
    }

    private static string GetCellText(IXLCell cell)
    {
        return cell.HasFormula ? cell.FormulaA1 : cell.GetString();
    }

    private static string GetCpfValidity(string cpf)
    {
        return CpfValidator.IsValid(cpf) ? "Válido" : "Inválido";
    }

    private static int GetMaxRecords(IConfiguration configuration)
    {
        return configuration.GetValue<int?>("CpfBatchImport:MaxRecords") ?? DefaultMaxRecords;
    }

    private static void EnsureRecordCountWithinLimit(int recordCount, int maxRecords)
    {
        if (recordCount > maxRecords)
        {
            throw new InvalidDataException($"Arquivo excede o limite permitido de {maxRecords} registros.");
        }
    }

    private static int GetCpfColumnIndex(IReadOnlyList<string> headers)
    {
        var cpfIndices = headers
            .Select((header, index) => new { Header = header.Trim(), Index = index })
            .Where(x => string.Equals(x.Header, "CPF", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Index)
            .ToArray();

        if (cpfIndices.Length == 0)
        {
            throw new InvalidDataException("Cabeçalho CPF não encontrado.");
        }

        if (cpfIndices.Length > 1)
        {
            throw new InvalidDataException("Cabeçalho CPF duplicado.");
        }

        return cpfIndices[0];
    }

    private static void EnsureNoDuplicateCpfHeader(IReadOnlyList<string> headers)
    {
        var validityHeaderCount = headers.Count(header => string.Equals(header.Trim(), "Validade CPF", StringComparison.OrdinalIgnoreCase));
        if (validityHeaderCount > 0)
        {
            throw new InvalidDataException("Cabeçalho Validade CPF já existe.");
        }
    }

    private static string BuildOutputFileName(string originalFileName, string extension)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "cpf-processado";
        }

        return $"{baseName}-processado{extension}";
    }
}

public sealed record CpfBatchValidationResult(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed class CpfBatchFileTooLargeException : Exception
{
    public CpfBatchFileTooLargeException(string message)
        : base(message)
    {
    }
}
