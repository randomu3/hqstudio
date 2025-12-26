using HQStudio.Models;
using HQStudio.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HQStudio.ViewModels
{
    /// <summary>
    /// ViewModel для страницы аналитики с графиками и статистикой
    /// </summary>
    public class AnalyticsViewModel : BaseViewModel
    {
        private readonly DataService _dataService = DataService.Instance;
        private readonly ApiService _apiService = ApiService.Instance;
        private readonly SettingsService _settings = SettingsService.Instance;

        private string _selectedPeriod = "Неделя";
        private decimal _totalRevenue;
        private decimal _averageOrderValue;
        private int _totalOrdersInPeriod;
        private int _completedOrdersInPeriod;
        private decimal _revenueGrowth;
        private bool _isLoading;

        public ObservableCollection<string> Periods { get; } = new()
        {
            "Неделя", "Месяц", "Квартал", "Год"
        };

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (SetProperty(ref _selectedPeriod, value))
                {
                    LoadData();
                }
            }
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public decimal AverageOrderValue
        {
            get => _averageOrderValue;
            set => SetProperty(ref _averageOrderValue, value);
        }

        public int TotalOrdersInPeriod
        {
            get => _totalOrdersInPeriod;
            set => SetProperty(ref _totalOrdersInPeriod, value);
        }

        public int CompletedOrdersInPeriod
        {
            get => _completedOrdersInPeriod;
            set => SetProperty(ref _completedOrdersInPeriod, value);
        }

        public decimal RevenueGrowth
        {
            get => _revenueGrowth;
            set => SetProperty(ref _revenueGrowth, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Графики
        private ISeries[] _revenueSeries = Array.Empty<ISeries>();
        private ISeries[] _ordersSeries = Array.Empty<ISeries>();
        private ISeries[] _servicesPieSeries = Array.Empty<ISeries>();
        private Axis[] _revenueXAxes = Array.Empty<Axis>();
        private Axis[] _revenueYAxes = Array.Empty<Axis>();
        private Axis[] _ordersXAxes = Array.Empty<Axis>();
        private Axis[] _ordersYAxes = Array.Empty<Axis>();

        public ISeries[] RevenueSeries
        {
            get => _revenueSeries;
            set => SetProperty(ref _revenueSeries, value);
        }

        public ISeries[] OrdersSeries
        {
            get => _ordersSeries;
            set => SetProperty(ref _ordersSeries, value);
        }

        public ISeries[] ServicesPieSeries
        {
            get => _servicesPieSeries;
            set => SetProperty(ref _servicesPieSeries, value);
        }

        public Axis[] RevenueXAxes
        {
            get => _revenueXAxes;
            set => SetProperty(ref _revenueXAxes, value);
        }

        public Axis[] RevenueYAxes
        {
            get => _revenueYAxes;
            set => SetProperty(ref _revenueYAxes, value);
        }

        public Axis[] OrdersXAxes
        {
            get => _ordersXAxes;
            set => SetProperty(ref _ordersXAxes, value);
        }

        public Axis[] OrdersYAxes
        {
            get => _ordersYAxes;
            set => SetProperty(ref _ordersYAxes, value);
        }

        // Топ услуги
        public ObservableCollection<ServiceStatistic> TopServices { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand GenerateReportCommand { get; }

        public AnalyticsViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadData());
            GenerateReportCommand = new RelayCommand(_ => GenerateReport());
            LoadData();
        }

        private async void LoadData()
        {
            IsLoading = true;

            try
            {
                var (startDate, endDate) = GetPeriodDates();
                var orders = await GetOrdersForPeriod(startDate, endDate);
                var previousOrders = await GetOrdersForPeriod(
                    startDate.AddDays(-(endDate - startDate).TotalDays),
                    startDate.AddDays(-1));

                CalculateStatistics(orders, previousOrders);
                LoadRevenueChart(orders, startDate, endDate);
                LoadOrdersChart(orders, startDate, endDate);
                LoadServicesPieChart(orders);
                LoadTopServices(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadData error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private (DateTime start, DateTime end) GetPeriodDates()
        {
            var end = DateTime.Today;
            var start = SelectedPeriod switch
            {
                "Неделя" => end.AddDays(-7),
                "Месяц" => end.AddMonths(-1),
                "Квартал" => end.AddMonths(-3),
                "Год" => end.AddYears(-1),
                _ => end.AddDays(-7)
            };
            return (start, end);
        }

        private async Task<List<Order>> GetOrdersForPeriod(DateTime start, DateTime end)
        {
            var orders = new List<Order>();

            if (_settings.UseApi && _apiService.IsConnected)
            {
                var page = 1;
                while (true)
                {
                    var response = await _apiService.GetOrdersAsync(page, 100);
                    if (response == null || !response.Items.Any()) break;

                    foreach (var apiOrder in response.Items)
                    {
                        if (apiOrder.CreatedAt >= start && apiOrder.CreatedAt <= end.AddDays(1))
                        {
                            orders.Add(new Order
                            {
                                Id = apiOrder.Id,
                                ClientId = apiOrder.ClientId,
                                Status = MapStatus(apiOrder.Status),
                                TotalPrice = apiOrder.TotalPrice,
                                CreatedAt = apiOrder.CreatedAt,
                                CompletedAt = apiOrder.CompletedAt,
                                ServiceIds = new List<int>() // API не возвращает ServiceIds
                            });
                        }
                    }

                    if (response.Items.Count < 100) break;
                    page++;
                }
            }
            else
            {
                orders = _dataService.Orders
                    .Where(o => o.CreatedAt >= start && o.CreatedAt <= end.AddDays(1))
                    .ToList();
            }

            return orders;
        }

        private string MapStatus(int status) => status switch
        {
            0 => "Новый",
            1 => "В работе",
            2 => "Завершен",
            3 => "Отменен",
            _ => "Неизвестно"
        };

        private void CalculateStatistics(List<Order> orders, List<Order> previousOrders)
        {
            var completedOrders = orders.Where(o => o.Status == "Завершен").ToList();
            var previousCompleted = previousOrders.Where(o => o.Status == "Завершен").ToList();

            TotalRevenue = completedOrders.Sum(o => o.TotalPrice);
            TotalOrdersInPeriod = orders.Count;
            CompletedOrdersInPeriod = completedOrders.Count;
            AverageOrderValue = completedOrders.Any() 
                ? completedOrders.Average(o => o.TotalPrice) 
                : 0;

            var previousRevenue = previousCompleted.Sum(o => o.TotalPrice);
            RevenueGrowth = previousRevenue > 0 
                ? ((TotalRevenue - previousRevenue) / previousRevenue) * 100 
                : (TotalRevenue > 0 ? 100 : 0);
        }

        private void LoadRevenueChart(List<Order> orders, DateTime start, DateTime end)
        {
            var groupedData = GetGroupedData(orders.Where(o => o.Status == "Завершен"), start, end);
            var labels = groupedData.Select(g => g.Label).ToArray();
            var values = groupedData.Select(g => (double)g.Revenue).ToArray();

            RevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(new SKColor(76, 175, 80)),
                    Stroke = null,
                    MaxBarWidth = 40
                }
            };

            RevenueXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    TextSize = 11
                }
            };

            RevenueYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    TextSize = 11,
                    Labeler = value => $"{value:N0} ₽",
                    MinLimit = 0
                }
            };
        }

        private void LoadOrdersChart(List<Order> orders, DateTime start, DateTime end)
        {
            var groupedData = GetGroupedData(orders, start, end);
            var labels = groupedData.Select(g => g.Label).ToArray();
            var completedValues = groupedData.Select(g => (double)g.CompletedCount).ToArray();
            var inProgressValues = groupedData.Select(g => (double)g.InProgressCount).ToArray();

            OrdersSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = completedValues,
                    Fill = new SolidColorPaint(new SKColor(76, 175, 80)),
                    Name = "Завершено",
                    MaxBarWidth = 20
                },
                new ColumnSeries<double>
                {
                    Values = inProgressValues,
                    Fill = new SolidColorPaint(new SKColor(255, 193, 7)),
                    Name = "В работе",
                    MaxBarWidth = 20
                }
            };

            OrdersXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    TextSize = 11
                }
            };

            OrdersYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(150, 150, 150)),
                    TextSize = 11,
                    MinLimit = 0
                }
            };
        }

        private List<GroupedOrderData> GetGroupedData(IEnumerable<Order> orders, DateTime start, DateTime end)
        {
            var result = new List<GroupedOrderData>();
            var days = (end - start).TotalDays;

            if (days <= 7)
            {
                // По дням
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    var dayOrders = orders.Where(o => o.CreatedAt.Date == d).ToList();
                    result.Add(new GroupedOrderData
                    {
                        Label = d.ToString("dd.MM"),
                        Revenue = dayOrders.Sum(o => o.TotalPrice),
                        CompletedCount = dayOrders.Count(o => o.Status == "Завершен"),
                        InProgressCount = dayOrders.Count(o => o.Status != "Завершен" && o.Status != "Отменен")
                    });
                }
            }
            else if (days <= 31)
            {
                // По неделям
                var weekStart = start;
                while (weekStart <= end)
                {
                    var weekEnd = weekStart.AddDays(6);
                    if (weekEnd > end) weekEnd = end;
                    
                    var weekOrders = orders.Where(o => o.CreatedAt.Date >= weekStart && o.CreatedAt.Date <= weekEnd).ToList();
                    result.Add(new GroupedOrderData
                    {
                        Label = $"{weekStart:dd.MM}",
                        Revenue = weekOrders.Sum(o => o.TotalPrice),
                        CompletedCount = weekOrders.Count(o => o.Status == "Завершен"),
                        InProgressCount = weekOrders.Count(o => o.Status != "Завершен" && o.Status != "Отменен")
                    });
                    weekStart = weekStart.AddDays(7);
                }
            }
            else
            {
                // По месяцам
                var monthStart = new DateTime(start.Year, start.Month, 1);
                while (monthStart <= end)
                {
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    var monthOrders = orders.Where(o => o.CreatedAt.Date >= monthStart && o.CreatedAt.Date <= monthEnd).ToList();
                    result.Add(new GroupedOrderData
                    {
                        Label = monthStart.ToString("MMM"),
                        Revenue = monthOrders.Sum(o => o.TotalPrice),
                        CompletedCount = monthOrders.Count(o => o.Status == "Завершен"),
                        InProgressCount = monthOrders.Count(o => o.Status != "Завершен" && o.Status != "Отменен")
                    });
                    monthStart = monthStart.AddMonths(1);
                }
            }

            return result;
        }

        private void LoadServicesPieChart(List<Order> orders)
        {
            var serviceStats = _dataService.Services
                .Select(s => new
                {
                    Service = s,
                    Count = orders.Count(o => o.ServiceIds.Contains(s.Id)),
                    Revenue = orders.Where(o => o.ServiceIds.Contains(s.Id) && o.Status == "Завершен")
                        .Sum(o => o.TotalPrice / Math.Max(o.ServiceIds.Count, 1))
                })
                .Where(s => s.Count > 0)
                .OrderByDescending(s => s.Revenue)
                .Take(5)
                .ToList();

            var colors = new[]
            {
                new SKColor(76, 175, 80),   // Green
                new SKColor(255, 193, 7),   // Yellow
                new SKColor(33, 150, 243),  // Blue
                new SKColor(156, 39, 176),  // Purple
                new SKColor(255, 87, 34)    // Orange
            };

            ServicesPieSeries = serviceStats.Select((s, i) => new PieSeries<double>
            {
                Values = new[] { (double)s.Revenue },
                Name = s.Service.Name,
                Fill = new SolidColorPaint(colors[i % colors.Length]),
                DataLabelsSize = 12,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsFormatter = p => $"{p.StackedValue?.Share:P0}"
            } as ISeries).ToArray();
        }

        private void LoadTopServices(List<Order> orders)
        {
            TopServices.Clear();

            var stats = _dataService.Services
                .Select(s => new ServiceStatistic
                {
                    Service = s,
                    OrderCount = orders.Count(o => o.ServiceIds.Contains(s.Id)),
                    TotalRevenue = orders.Where(o => o.ServiceIds.Contains(s.Id) && o.Status == "Завершен")
                        .Sum(o => o.TotalPrice / Math.Max(o.ServiceIds.Count, 1))
                })
                .Where(s => s.OrderCount > 0)
                .OrderByDescending(s => s.TotalRevenue)
                .Take(5);

            foreach (var stat in stats)
            {
                TopServices.Add(stat);
            }
        }

        private void GenerateReport()
        {
            try
            {
                var (startDate, endDate) = GetPeriodDates();
                var reportData = BuildReportData(startDate, endDate);

                byte[] pdfBytes;
                string fileName;

                if (SelectedPeriod == "Месяц" || SelectedPeriod == "Квартал" || SelectedPeriod == "Год")
                {
                    pdfBytes = ReportService.Instance.GenerateMonthlyReport(reportData);
                    fileName = $"HQStudio_Report_{endDate:yyyy-MM-dd}";
                }
                else
                {
                    pdfBytes = ReportService.Instance.GenerateWeeklyReport(reportData);
                    fileName = $"HQStudio_Weekly_{endDate:yyyy-MM-dd}";
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = fileName,
                    DefaultExt = ".pdf",
                    Filter = "PDF документы|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    ReportService.Instance.SaveReport(pdfBytes, dialog.FileName);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                    ToastService.Instance.ShowSuccess("Отчёт сохранён");
                }
            }
            catch (Exception ex)
            {
                ToastService.Instance.ShowError($"Ошибка: {ex.Message}");
            }
        }

        private ReportData BuildReportData(DateTime startDate, DateTime endDate)
        {
            var orders = _dataService.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate.AddDays(1))
                .ToList();

            return new ReportData
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.Status == "Завершен"),
                InProgressOrders = orders.Count(o => o.Status == "В работе"),
                TotalRevenue = orders.Where(o => o.Status == "Завершен").Sum(o => o.TotalPrice),
                DailyRevenue = orders.GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new DailyRevenue
                    {
                        Date = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Where(o => o.Status == "Завершен").Sum(o => o.TotalPrice)
                    }).OrderBy(d => d.Date).ToList(),
                ServiceStats = TopServices.Select(s => new ReportServiceStat
                {
                    ServiceName = s.Service.Name,
                    Count = s.OrderCount,
                    Revenue = s.TotalRevenue
                }).ToList(),
                Orders = orders.Select(o => new OrderSummary
                {
                    Id = o.Id,
                    ClientName = _dataService.Clients.FirstOrDefault(c => c.Id == o.ClientId)?.Name ?? "—",
                    Date = o.CreatedAt,
                    Status = o.Status,
                    TotalPrice = o.TotalPrice
                }).OrderByDescending(o => o.Date).ToList()
            };
        }

        private class GroupedOrderData
        {
            public string Label { get; set; } = "";
            public decimal Revenue { get; set; }
            public int CompletedCount { get; set; }
            public int InProgressCount { get; set; }
        }
    }
}
