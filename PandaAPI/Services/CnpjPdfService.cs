using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PandaAPI.Services;

public sealed class CnpjPdfService
{
    private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");

    public byte[] GenerateReport(JsonElement providerData, string cnpj)
    {
        var sections = new List<ReportSection>();
        var data = providerData.ValueKind == JsonValueKind.Object &&
                   providerData.TryGetProperty("dados", out var providerPayload)
            ? providerPayload
            : providerData;

        CollectSections(data, string.Empty, sections);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(42);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(style => style
                .FontSize(10)
                .FontColor(Colors.Grey.Darken3));

            page.Header().Element(container => ComposeHeader(container, cnpj));
            page.Content().PaddingTop(24).Column(column =>
            {
                if (sections.Count == 0)
                {
                    column.Item().Text("Nenhum dado cadastral foi disponibilizado.")
                        .FontColor(Colors.Grey.Darken1);
                    return;
                }

                foreach (var section in sections)
                {
                    column.Item().PaddingBottom(16).Element(container => ComposeSection(container, section));
                }
            });
            page.Footer().PaddingTop(12).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                .Row(row =>
                {
                    row.RelativeItem().Text("Relatório cadastral • PandaAPI")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
        })).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string cnpj)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(brand =>
                {
                    brand.Item().Text("PANDA API")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2)
                        .LetterSpacing(1);
                    brand.Item().PaddingTop(10).Text("Relatório cadastral")
                        .FontSize(22)
                        .Bold()
                        .FontColor(Colors.Grey.Darken4);
                });
                row.ConstantItem(175).AlignRight().AlignBottom().Column(details =>
                {
                    details.Item().Text("CNPJ CONSULTADO")
                        .FontSize(8)
                        .Bold()
                        .FontColor(Colors.Grey.Medium);
                    details.Item().PaddingTop(3).Text(CnpjValidator.Format(cnpj))
                        .FontSize(11)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);
                });
            });
            column.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Height(3).Background(Colors.Blue.Darken2);
                row.ConstantItem(34).Height(3).Background(Colors.Teal.Medium);
            });
            column.Item().PaddingTop(8).Text($"Emitido em {DateTimeOffset.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", PortugueseCulture)}")
                .FontSize(8)
                .FontColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeSection(IContainer container, ReportSection section)
    {
        container.BorderLeft(3)
            .BorderColor(Colors.Blue.Lighten2)
            .PaddingLeft(12)
            .Column(column =>
            {
                column.Item().PaddingBottom(5).Text(section.Title)
                    .FontSize(12)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);
                column.Item().Column(fields =>
                {
                    foreach (var field in section.Fields)
                    {
                        fields.Item().PaddingVertical(5).Row(row =>
                        {
                            row.RelativeItem(1).Text(field.Label)
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                            row.RelativeItem(2).Text(field.Value)
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(Colors.Grey.Darken4);
                        });
                        fields.Item().Height(1).Background(Colors.Grey.Lighten3);
                    }
                });
            });
    }

    private static void CollectSections(JsonElement element, string sectionPath, ICollection<ReportSection> sections)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var fields = new List<ReportField>();
        foreach (var property in element.EnumerateObject())
        {
            var propertyTitle = Humanize(property.Name);
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    CollectSections(property.Value, JoinSectionPath(sectionPath, propertyTitle), sections);
                    break;
                case JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            index++;
                            CollectSections(item, JoinSectionPath(sectionPath, $"{propertyTitle} {index}"), sections);
                        }
                        else if (TryFormatValue(item, out var itemValue))
                        {
                            fields.Add(new ReportField(propertyTitle, itemValue));
                        }
                    }
                    break;
                default:
                    if (TryFormatValue(property.Value, out var value))
                    {
                        fields.Add(new ReportField(propertyTitle, value));
                    }
                    break;
            }
        }

        if (fields.Count > 0)
        {
            sections.Add(new ReportSection(
                string.IsNullOrWhiteSpace(sectionPath) ? "Dados cadastrais" : sectionPath,
                fields));
        }
    }

    private static bool TryFormatValue(JsonElement value, out string formattedValue)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                formattedValue = value.GetString() ?? string.Empty;
                break;
            case JsonValueKind.Number:
                formattedValue = value.GetRawText();
                break;
            case JsonValueKind.True:
                formattedValue = "Sim";
                break;
            case JsonValueKind.False:
                formattedValue = "Não";
                break;
            default:
                formattedValue = string.Empty;
                break;
        }

        return !string.IsNullOrWhiteSpace(formattedValue);
    }

    private static string Humanize(string value)
    {
        var spaced = Regex.Replace(value.Replace('_', ' ').Replace('-', ' '), "([a-z0-9])([A-Z])", "$1 $2");
        return PortugueseCulture.TextInfo.ToTitleCase(spaced.ToLower(PortugueseCulture));
    }

    private static string JoinSectionPath(string parent, string child) =>
        string.IsNullOrWhiteSpace(parent) ? child : $"{parent} · {child}";

    private sealed record ReportSection(string Title, IReadOnlyCollection<ReportField> Fields);
    private sealed record ReportField(string Label, string Value);
}
