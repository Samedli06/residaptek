using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartTeam.Application.DTOs;
using SmartTeam.Application.Services;

namespace SmartTeam.Infrastructure.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateOrderReceipt(OrderDto order)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                
                page.Header().Element(header => ComposeHeader(header, order));
                page.Content().Element(content => ComposeContent(content, order));
                page.Footer().Element(footer => ComposeFooter(footer, order));
            });
        }).GeneratePdf();
    }

    public byte[] GenerateBulkOrderReceipts(IEnumerable<OrderDto> orders, DateTime? fromDate, DateTime? toDate)
    {
        return Document.Create(container =>
        {
            foreach (var order in orders)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    
                    page.Header().Element(header => ComposeHeader(header, order));
                    page.Content().Element(content => ComposeContent(content, order));
                    page.Footer().Element(footer => ComposeFooter(footer, order));
                });
            }
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, OrderDto order)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("E-DEPO.az").FontSize(14).ExtraBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Bakı Masazır, Yeni Bakı 16").FontSize(10);
                column.Item().Text("Tel: +994 993 99 96 76").FontSize(10);
                column.Item().Text("Email: Info@e-depo.az").FontSize(10);
            });

            row.ConstantItem(150).Column(column =>
            {
                column.Item().Text("QAİMƏ").FontSize(20).SemiBold().AlignRight();
                column.Item().Text(order.OrderNumber).FontSize(12).AlignRight();
                column.Item().Text(order.CreatedAt.ToString("dd.MM.yyyy HH:mm")).FontSize(10).AlignRight();
            });
        });
    }

    private void ComposeContent(IContainer container, OrderDto order)
    {
        container.PaddingVertical(40).Column(column =>
        {
            // Customer Details
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Sifarişçi:").FontColor(Colors.Grey.Medium).FontSize(10);
                    col.Item().Text(order.CustomerName).FontSize(12).SemiBold();
                    if (!string.IsNullOrEmpty(order.CustomerPhone))
                        col.Item().Text(order.CustomerPhone).FontSize(10);
                    if (!string.IsNullOrEmpty(order.DeliveryAddress))
                        col.Item().Text(order.DeliveryAddress).FontSize(10);
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Status:").FontColor(Colors.Grey.Medium).FontSize(10).AlignRight();
                    col.Item().Text(order.StatusText).FontSize(12).SemiBold().AlignRight();
                });
            });

            column.Item().PaddingTop(25).Element(ele => ComposeTable(ele, order));
            
            // Financials
            column.Item().PaddingTop(20).Column(col => 
            {
                // Subtotal
                col.Item().Row(row =>
                {
                    row.RelativeItem().AlignRight().Text($"Cəm Məbləğ: {order.SubTotal:N2} ₼").FontSize(10);
                });



                // Total
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().AlignRight().Text($"Yekun Məbləğ: {order.TotalAmount:N2} ₼").FontSize(16).SemiBold();
                });
            });
            

        });
    }

    private void ComposeTable(IContainer container, OrderDto order)
    {
        container.Table(table =>
        {
            // Definition
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(3);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("#");
                header.Cell().Element(CellStyle).Text("Məhsul");
                header.Cell().Element(CellStyle).AlignRight().Text("Say");
                header.Cell().Element(CellStyle).AlignRight().Text("Qiymət");
                header.Cell().Element(CellStyle).AlignRight().Text("Məbləğ");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                }
            });

            // Content
            for (var i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                
                table.Cell().Element(CellStyle).Text($"{i + 1}");
                table.Cell().Element(CellStyle).Text(item.ProductName);
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Quantity}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N2} ₼");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.TotalPrice:N2} ₼");

                IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeFooter(IContainer container, OrderDto order)
    {
        container.Row(row =>
        {
            row.RelativeItem(2).Row(sigRow =>
            {
                sigRow.RelativeItem().Column(col => 
                {
                    col.Item().Text("Təhvil verən (Satıcı):").FontSize(10);
                    col.Item().PaddingTop(25).Text("_____________________");
                });
                        
                sigRow.RelativeItem().Column(col => 
                {
                    col.Item().Text("Təhvil alan (Sifarişçi):").FontSize(10);
                    col.Item().PaddingTop(25).Text("_____________________");
                });
            });
            
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Səhifə ");
                x.CurrentPageNumber();
            });
        });
    }
}
