using System.IO;
using Xunit;
using HQStudio.Services;

namespace HQStudio.Desktop.Tests
{
    public class ReportServiceTests
    {
        [Fact]
        public void Instance_ReturnsSameInstance()
        {
            var instance1 = ReportService.Instance;
            var instance2 = ReportService.Instance;
            
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void GenerateWeeklyReport_WithEmptyData_ReturnsPdf()
        {
            var data = new ReportData
            {
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today,
                TotalOrders = 0,
                CompletedOrders = 0,
                InProgressOrders = 0,
                TotalRevenue = 0
            };

            var pdf = ReportService.Instance.GenerateWeeklyReport(data);

            Assert.NotNull(pdf);
            Assert.True(pdf.Length > 0);
            // PDF начинается с %PDF
            Assert.Equal(0x25, pdf[0]); // %
            Assert.Equal(0x50, pdf[1]); // P
            Assert.Equal(0x44, pdf[2]); // D
            Assert.Equal(0x46, pdf[3]); // F
        }

        [Fact]
        public void GenerateMonthlyReport_WithData_ReturnsPdf()
        {
            var data = new ReportData
            {
                StartDate = new DateTime(2024, 12, 1),
                EndDate = new DateTime(2024, 12, 31),
                TotalOrders = 15,
                CompletedOrders = 10,
                InProgressOrders = 5,
                TotalRevenue = 150000,
                DailyRevenue = new List<DailyRevenue>
                {
                    new() { Date = new DateTime(2024, 12, 1), OrderCount = 3, Revenue = 30000 },
                    new() { Date = new DateTime(2024, 12, 2), OrderCount = 2, Revenue = 20000 }
                },
                ServiceStats = new List<ReportServiceStat>
                {
                    new() { ServiceName = "Шумоизоляция", Count = 5, Revenue = 75000 },
                    new() { ServiceName = "Доводчики", Count = 3, Revenue = 45000 }
                },
                Orders = new List<OrderSummary>
                {
                    new() { Id = 1, ClientName = "Иванов", Date = DateTime.Today, Status = "Завершен", TotalPrice = 15000 }
                }
            };

            var pdf = ReportService.Instance.GenerateMonthlyReport(data);

            Assert.NotNull(pdf);
            Assert.True(pdf.Length > 1000); // PDF с данными должен быть больше
        }

        [Fact]
        public void GenerateWeeklyReport_WithAllStatuses_IncludesAllData()
        {
            var data = new ReportData
            {
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today,
                TotalOrders = 10,
                CompletedOrders = 5,
                InProgressOrders = 3,
                TotalRevenue = 100000,
                Orders = new List<OrderSummary>
                {
                    new() { Id = 1, ClientName = "Клиент 1", Date = DateTime.Today, Status = "Завершен", TotalPrice = 20000 },
                    new() { Id = 2, ClientName = "Клиент 2", Date = DateTime.Today, Status = "В работе", TotalPrice = 15000 },
                    new() { Id = 3, ClientName = "Клиент 3", Date = DateTime.Today, Status = "Отменен", TotalPrice = 10000 },
                    new() { Id = 4, ClientName = "Клиент 4", Date = DateTime.Today, Status = "Новый", TotalPrice = 5000 }
                }
            };

            var pdf = ReportService.Instance.GenerateWeeklyReport(data);

            Assert.NotNull(pdf);
            Assert.True(pdf.Length > 0);
        }

        [Fact]
        public void SaveReport_WritesToFile()
        {
            var data = new ReportData
            {
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today,
                TotalOrders = 1,
                TotalRevenue = 10000
            };

            var pdf = ReportService.Instance.GenerateWeeklyReport(data);
            var tempPath = Path.Combine(Path.GetTempPath(), $"test_report_{Guid.NewGuid()}.pdf");

            try
            {
                ReportService.Instance.SaveReport(pdf, tempPath);
                
                Assert.True(File.Exists(tempPath));
                var savedBytes = File.ReadAllBytes(tempPath);
                Assert.Equal(pdf.Length, savedBytes.Length);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public void ReportData_DefaultValues_AreEmpty()
        {
            var data = new ReportData();

            Assert.Equal(default, data.StartDate);
            Assert.Equal(default, data.EndDate);
            Assert.Equal(0, data.TotalOrders);
            Assert.Equal(0, data.CompletedOrders);
            Assert.Equal(0, data.InProgressOrders);
            Assert.Equal(0, data.TotalRevenue);
            Assert.Empty(data.DailyRevenue);
            Assert.Empty(data.ServiceStats);
            Assert.Empty(data.Orders);
        }

        [Fact]
        public void DailyRevenue_Properties_SetCorrectly()
        {
            var revenue = new DailyRevenue
            {
                Date = new DateTime(2024, 12, 24),
                OrderCount = 5,
                Revenue = 50000
            };

            Assert.Equal(new DateTime(2024, 12, 24), revenue.Date);
            Assert.Equal(5, revenue.OrderCount);
            Assert.Equal(50000, revenue.Revenue);
        }

        [Fact]
        public void ReportServiceStat_Properties_SetCorrectly()
        {
            var stat = new ReportServiceStat
            {
                ServiceName = "Шумоизоляция",
                Count = 10,
                Revenue = 150000
            };

            Assert.Equal("Шумоизоляция", stat.ServiceName);
            Assert.Equal(10, stat.Count);
            Assert.Equal(150000, stat.Revenue);
        }

        [Fact]
        public void OrderSummary_Properties_SetCorrectly()
        {
            var order = new OrderSummary
            {
                Id = 42,
                ClientName = "Тестовый клиент",
                Date = new DateTime(2024, 12, 24),
                Status = "Завершен",
                TotalPrice = 25000
            };

            Assert.Equal(42, order.Id);
            Assert.Equal("Тестовый клиент", order.ClientName);
            Assert.Equal(new DateTime(2024, 12, 24), order.Date);
            Assert.Equal("Завершен", order.Status);
            Assert.Equal(25000, order.TotalPrice);
        }
    }
}
