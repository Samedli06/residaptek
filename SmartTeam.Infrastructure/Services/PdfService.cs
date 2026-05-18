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
                column.Item().Text("Masazır, Yeni Bakı 123").FontSize(10);
                column.Item().Text("Tel: +994 099 399 96 44").FontSize(10);
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
                // Subtotal row
                col.Item().Row(row =>
                {
                    row.RelativeItem().AlignRight().Text($"Cəm Məbləğ: {order.SubTotal:N2} ₼").FontSize(10);
                });

                if (order.FinalPrice.HasValue)
                {
                    // Show calculated total as a reference line (greyed out)
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().AlignRight()
                            .Text($"Hesablanmış məbləğ: {order.TotalAmount:N2} ₼")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Medium);
                    });

                    // Show admin-overridden final price prominently
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().AlignRight()
                            .Text($"Yekun Məbləğ: {order.FinalPrice.Value:N2} ₼")
                            .FontSize(16).SemiBold().FontColor(Colors.Blue.Medium);
                    });
                }
                else
                {
                    // No override — display calculated total as usual
                    col.Item().PaddingTop(5).Row(row =>
                    {
                        row.RelativeItem().AlignRight()
                            .Text($"Yekun Məbləğ: {order.TotalAmount:N2} ₼")
                            .FontSize(16).SemiBold();
                    });
                }
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
                    return container
                        .Border(1)
                        .BorderColor(Colors.Grey.Medium)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(5)
                        .DefaultTextStyle(x => x.SemiBold());
                }
            });

            // Content
            for (var i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                
                table.Cell().Element(CellStyle).Text($"{i + 1}");
                table.Cell().Element(CellStyle).Text(item.ProductName);
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Quantity}");

                // Price cell — show discounted price with original struck-through if discount is set
                if (item.DiscountedUnitPrice.HasValue)
                {
                    table.Cell().Element(CellStyle).AlignRight().Column(col =>
                    {
                        col.Item().Text($"{item.UnitPrice:N2} ₼")
                            .FontSize(8).FontColor(Colors.Grey.Medium).Strikethrough();
                        col.Item().Text($"{item.DiscountedUnitPrice.Value:N2} ₼")
                            .FontSize(10).FontColor(Colors.Blue.Medium).SemiBold();
                    });
                }
                else
                {
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N2} ₼");
                }

                table.Cell().Element(CellStyle).AlignRight().Text($"{item.TotalPrice:N2} ₼");

                IContainer CellStyle(IContainer container)
                {
                    return container
                        .Border(1)
                        .BorderColor(Colors.Grey.Medium)
                        .Padding(5);
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

    // ─────────────────────────────────────────────────────────────────
    // Purchase Expense Receipt (Alış Xərcləri Qaiməsi)
    // ─────────────────────────────────────────────────────────────────

    public byte[] GeneratePurchaseExpenseReceipt(ProductPurchaseExpenseDto expense)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Element(header => ComposePurchaseHeader(header, expense));
                page.Content().Element(content => ComposePurchaseContent(content, expense));
                page.Footer().Element(footer => ComposePurchaseFooter(footer));
            });
        }).GeneratePdf();
    }

    public byte[] GenerateBulkPurchaseExpenseReceipts(IEnumerable<ProductPurchaseExpenseDto> expenses)
    {
        return Document.Create(container =>
        {
            foreach (var expense in expenses)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().Element(header => ComposePurchaseHeader(header, expense));
                    page.Content().Element(content => ComposePurchaseContent(content, expense));
                    page.Footer().Element(footer => ComposePurchaseFooter(footer));
                });
            }
        }).GeneratePdf();
    }

    private void ComposePurchaseHeader(IContainer container, ProductPurchaseExpenseDto expense)
    {
        container.Row(row =>
        {
            // Left: Company info
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("E-DEPO.az").FontSize(14).ExtraBold().FontColor(Colors.Blue.Medium);
                column.Item().Text("Masazır, Yeni Bakı 123").FontSize(10);
                column.Item().Text("Tel: +994 099 399 96 44").FontSize(10);
                column.Item().Text("Email: Info@e-depo.az").FontSize(10);
            });

            // Right: Invoice title + number + date
            row.ConstantItem(180).Column(column =>
            {
                column.Item().Text("ALIŞ QAİMƏSİ").FontSize(18).SemiBold().AlignRight();
                column.Item().Text(expense.InvoiceNumber).FontSize(12).AlignRight();
                column.Item().Text(expense.PurchaseDate.ToString("dd.MM.yyyy HH:mm")).FontSize(10).AlignRight();
            });
        });
    }

    private void ComposePurchaseContent(IContainer container, ProductPurchaseExpenseDto expense)
    {
        container.PaddingVertical(40).Column(column =>
        {
            // Supplier / Product info block
            column.Item().Row(row =>
            {
                // Left: Supplier info
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Tədarikçi:").FontColor(Colors.Grey.Medium).FontSize(10);
                    col.Item().Text(
                        string.IsNullOrEmpty(expense.SupplierName) ? "—" : expense.SupplierName
                    ).FontSize(12).SemiBold();
                });

                // Right: Status badge
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Növ:").FontColor(Colors.Grey.Medium).FontSize(10).AlignRight();
                    col.Item().Text("Alış").FontSize(12).SemiBold().AlignRight();
                });
            });

            // Notes (if any)
            if (!string.IsNullOrEmpty(expense.Notes))
            {
                column.Item().PaddingTop(8).Text($"Qeyd: {expense.Notes}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1).Italic();
            }

            // Item table
            column.Item().PaddingTop(25).Table(table =>
            {
                // Column widths
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);  // #
                    columns.RelativeColumn(3);   // Məhsul
                    columns.RelativeColumn();    // Miqdar
                    columns.RelativeColumn();    // Vahid Qiymət
                    columns.RelativeColumn();    // Ümumi Xərc
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("#");
                    header.Cell().Element(HeaderCellStyle).Text("Məhsul");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Miqdar");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Vahid Qiymət");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Ümumi Xərc");

                    static IContainer HeaderCellStyle(IContainer c) =>
                        c.Border(1)
                         .BorderColor(Colors.Grey.Medium)
                         .Background(Colors.Grey.Lighten3)
                         .Padding(5)
                         .DefaultTextStyle(x => x.SemiBold());
                });

                // Single item row (one expense = one product purchase line)
                table.Cell().Element(BodyCellStyle).Text("1");
                table.Cell().Element(BodyCellStyle).Text(expense.ProductName);
                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{expense.Quantity}");
                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{expense.UnitPurchasePrice:N2} ₼");
                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{expense.TotalExpense:N2} ₼");

                static IContainer BodyCellStyle(IContainer c) =>
                    c.Border(1)
                     .BorderColor(Colors.Grey.Medium)
                     .Padding(5);
            });

            // Totals
            column.Item().PaddingTop(20).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().AlignRight()
                        .Text($"Cəm Məbləğ: {expense.TotalExpense:N2} ₼").FontSize(10);
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().AlignRight()
                        .Text($"Yekun Məbləğ: {expense.TotalExpense:N2} ₼").FontSize(16).SemiBold();
                });
            });
        });
    }

    private void ComposePurchaseFooter(IContainer container)
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
                    col.Item().Text("Qəbul edən (Anbar):").FontSize(10);
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
