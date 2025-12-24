using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HQStudio.Models;

namespace HQStudio.Services
{
    /// <summary>
    /// Сервис генерации отчётов (PDF)
    /// </summary>
    public class ReportService
    {
        private static ReportService? _instance;
        public static ReportService Instance => _instance ??= new ReportService();

        private ReportService()
        {
            // Лицензия QuestPDF (Community - бесплатно для малого бизнеса)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// Генерирует еженедельный отчёт
        /// </summary>
        public byte[] GenerateWeeklyReport(ReportData data)
        {
            return GenerateReport(data, "Еженедельный отчёт", 
                $"{data.StartDate:dd.MM.yyyy} - {data.EndDate:dd.MM.yyyy}");
        }

        /// <summary>
        /// Генерирует месячный отчёт
        /// </summary>
        public byte[] GenerateMonthlyReport(ReportData data)
        {
            return GenerateReport(data, "Месячный отчёт",
                data.StartDate.ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU")));
        }

        private byte[] GenerateReport(ReportData data, string title, string period)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(c => ComposeHeader(c, title, period));
                    page.Content().Element(c => ComposeContent(c, data));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, string title, string period)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("HQ STUDIO").Bold().FontSize(24);
                        c.Item().Text("Профессиональное дооснащение автомобилей").FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                    row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(10);
                });

                column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                column.Item().Text(title).Bold().FontSize(18);
                column.Item().Text(period).FontSize(12).FontColor(Colors.Grey.Darken1);

                column.Item().PaddingTop(15);
            });
        }

        private void ComposeContent(IContainer container, ReportData data)
        {
            container.Column(column =>
            {
                // Сводка
                column.Item().Element(c => ComposeSummary(c, data));
                column.Item().PaddingVertical(15);

                // Выручка по дням
                if (data.DailyRevenue.Any())
                {
                    column.Item().Text("📊 Выручка по дням").Bold().FontSize(14);
                    column.Item().PaddingTop(10).Element(c => ComposeRevenueTable(c, data.DailyRevenue));
                    column.Item().PaddingVertical(15);
                }

                // Популярные услуги
                if (data.ServiceStats.Any())
                {
                    column.Item().Text("🔧 Популярные услуги").Bold().FontSize(14);
                    column.Item().PaddingTop(10).Element(c => ComposeServicesTable(c, data.ServiceStats));
                    column.Item().PaddingVertical(15);
                }

                // Список заказов
                if (data.Orders.Any())
                {
                    column.Item().Text("📋 Заказы за период").Bold().FontSize(14);
                    column.Item().PaddingTop(10).Element(c => ComposeOrdersTable(c, data.Orders));
                }
            });
        }

        private void ComposeSummary(IContainer container, ReportData data)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Всего заказов").FontSize(10).FontColor(Colors.Grey.Medium);
                    c.Item().Text(data.TotalOrders.ToString()).Bold().FontSize(24);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Завершено").FontSize(10).FontColor(Colors.Grey.Medium);
                    c.Item().Text(data.CompletedOrders.ToString()).Bold().FontSize(24).FontColor(Colors.Green.Medium);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("В работе").FontSize(10).FontColor(Colors.Grey.Medium);
                    c.Item().Text(data.InProgressOrders.ToString()).Bold().FontSize(24).FontColor(Colors.Orange.Medium);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Выручка").FontSize(10).FontColor(Colors.Grey.Medium);
                    c.Item().Text($"{data.TotalRevenue:N0} ₽").Bold().FontSize(24).FontColor(Colors.Green.Darken1);
                });
            });
        }

        private void ComposeRevenueTable(IContainer container, List<DailyRevenue> revenues)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Дата").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Заказов").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Выручка").Bold();
                });

                foreach (var item in revenues)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(item.Date.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(item.OrderCount.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .AlignRight().Text($"{item.Revenue:N0} ₽");
                }

                // Итого
                table.Cell().Background(Colors.Grey.Lighten4).Padding(5).Text("Итого").Bold();
                table.Cell().Background(Colors.Grey.Lighten4).Padding(5).Text(revenues.Sum(r => r.OrderCount).ToString()).Bold();
                table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight()
                    .Text($"{revenues.Sum(r => r.Revenue):N0} ₽").Bold();
            });
        }

        private void ComposeServicesTable(IContainer container, List<ReportServiceStat> services)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Услуга").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Кол-во").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Выручка").Bold();
                });

                foreach (var item in services.Take(10))
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.ServiceName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .AlignRight().Text($"{item.Revenue:N0} ₽");
                }
            });
        }

        private void ComposeOrdersTable(IContainer container, List<OrderSummary> orders)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Клиент").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Дата").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Статус").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Сумма").Bold();
                });

                foreach (var order in orders.Take(50))
                {
                    var statusColor = order.Status switch
                    {
                        "Завершен" => Colors.Green.Medium,
                        "В работе" => Colors.Orange.Medium,
                        "Отменен" => Colors.Red.Medium,
                        _ => Colors.Grey.Medium
                    };

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.Id.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(order.ClientName);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(order.Date.ToString("dd.MM.yyyy"));
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .Text(order.Status).FontColor(statusColor);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5)
                        .AlignRight().Text($"{order.TotalPrice:N0} ₽");
                }
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(text =>
            {
                text.Span("HQ Studio • Сургут • ");
                text.Span("+7-929-293-52-22").FontColor(Colors.Grey.Medium);
            });
        }

        /// <summary>
        /// Сохраняет отчёт в файл
        /// </summary>
        public void SaveReport(byte[] pdfBytes, string filePath)
        {
            File.WriteAllBytes(filePath, pdfBytes);
        }
    }

    #region Report Models

    public class ReportData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int InProgressOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<DailyRevenue> DailyRevenue { get; set; } = new();
        public List<ReportServiceStat> ServiceStats { get; set; } = new();
        public List<OrderSummary> Orders { get; set; } = new();
    }

    public class DailyRevenue
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ReportServiceStat
    {
        public string ServiceName { get; set; } = "";
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderSummary
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = "";
        public DateTime Date { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalPrice { get; set; }
    }

    #endregion
}
