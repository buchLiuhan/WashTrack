using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using WashTrack.Data;
using WashTrack.Models;

namespace WashTrack.MVVM.ViewModels
{
    // One item's full stock position over the selected date range:
    //
    //     Opening + Stock In + Adjustments − Used = Closing
    //
    // Two event logs (what came in, what went out) can't answer "does my
    // stock actually add up?" — this can, because every figure in the line
    // is derived from the same history and has to balance. If it ever
    // doesn't, something went unrecorded.
    public partial class InventoryReconciliationRow : ObservableObject
    {
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public decimal Opening { get; set; }
        public decimal StockIn { get; set; }
        public decimal Used { get; set; }

        // Signed, and negative in practice: corrections for spillage,
        // spoilage or a miscount. Kept out of Stock In so a bad month
        // can't hide inside a good delivery.
        public decimal Adjustments { get; set; }

        public decimal Closing { get; set; }
        public bool IsLowStock { get; set; }

        public string OpeningText => $"{Opening:F0}{Unit}";
        public string StockInText => $"+{StockIn:F0}{Unit}";
        public string UsedText => $"−{Used:F0}{Unit}";
        public string AdjustmentsText => $"{(Adjustments > 0 ? "+" : "−")}{Math.Abs(Adjustments):F0}{Unit}";
        public string ClosingText => $"{Closing:F0}{Unit}";

        // Row is hidden when zero — most items have no corrections, and an
        // empty "0" line on every card buries the ones that do.
        public bool HasAdjustments => Adjustments != 0;
    }

    // One stock movement in the selected date range. Three kinds share this
    // row because they're the same thing from the owner's side — stock moved:
    //
    //   Used     supplies consumed by a job (usage history)
    //   Stock In new supply received        (restock, positive)
    //   Stock Out a correction: spillage, spoilage, miscount (restock, negative)
    //
    // Every figure on the stock summary card above now has its own lines
    // down here, so nothing on that card is asserted without working.
    public partial class StockMovementRow : ObservableObject
    {
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        // Signed: negative for usage and corrections, positive for new supply.
        public decimal QuantityChange { get; set; }
        public DateTime MovementDate { get; set; }
        public string? Notes { get; set; }

        // Separates usage from a correction — both are negative, so the sign
        // alone can't tell them apart.
        public bool IsUsage { get; set; }

        // Usage can exist without an order (the column is nullable), so this
        // doesn't assume one.
        public int? TransactionId { get; set; }

        public bool IsCorrection => !IsUsage && QuantityChange < 0;
        public string ChangeText => $"{(QuantityChange > 0 ? "+" : "")}{QuantityChange:F0}{Unit}";
        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        // Usage rows carry the order on the same line as the date rather than
        // in a row of their own — it keeps every movement card the same height.
        public string DateText => IsUsage && TransactionId.HasValue
            ? $"{MovementDate:MMM dd} · Order #{TransactionId.Value:D4}"
            : MovementDate.ToString("MMM dd");
    }

    public partial class ReportsViewModel : ObservableObject
    {
        private readonly WashTrackContext _context;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        private decimal totalRevenue;

        [ObservableProperty]
        private int totalTransactions;

        [ObservableProperty]
        private decimal averageTransactionValue;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private DateTime startDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime endDate = DateTime.Today;

        // Sales/Inventory segmented toggle
        [ObservableProperty]
        private bool showingInventoryReport = false;

        [ObservableProperty]
        private string reportToggleText = "View Inventory Report";

        [ObservableProperty]
        private ObservableCollection<InventoryReconciliationRow> inventoryUsage = new();

        [ObservableProperty]
        private int lowStockCount;

        [ObservableProperty]
        private ObservableCollection<StockMovementRow> restockHistory = new();

        // Count, not a sum: quantities span different units (L, kg, pcs),
        // so totalling them across items would be meaningless.
        [ObservableProperty]
        private int correctionCount;

        // ===== STOCK MOVEMENT FILTER =====

        // Which slice of the movement log is on screen. One segment per kind
        // of movement, so each of the three is reachable deliberately —
        // there's no combined view, because mixing directions in one list
        // makes it easy to skim past the losses.
        //
        // Held as a string so the three buttons can pass it as a
        // CommandParameter without needing a converter for an enum.
        [ObservableProperty]
        private string movementFilter = MovementFilterUsed;

        public const string MovementFilterUsed = "Used";
        public const string MovementFilterIn = "In";
        public const string MovementFilterOut = "Out";

        // Drive the selected-state highlight on each segment button.
        public bool IsMovementFilterUsed => MovementFilter == MovementFilterUsed;
        public bool IsMovementFilterIn => MovementFilter == MovementFilterIn;
        public bool IsMovementFilterOut => MovementFilter == MovementFilterOut;

        // Every movement in range, before the segment filter. Lets the
        // buttons re-slice in memory instead of re-querying.
        private List<StockMovementRow> _allMovements = new();

        partial void OnMovementFilterChanged(string value)
        {
            OnPropertyChanged(nameof(IsMovementFilterUsed));
            OnPropertyChanged(nameof(IsMovementFilterIn));
            OnPropertyChanged(nameof(IsMovementFilterOut));
            OnPropertyChanged(nameof(MovementEmptyText));
            ApplyMovementFilter();
        }

        // The empty state names the segment you're on. A single generic
        // "no movements" line reads like the whole range is empty when it's
        // only this one slice that is.
        public string MovementEmptyText => MovementFilter switch
        {
            MovementFilterIn => "No stock was received in this date range.",
            MovementFilterOut => "No corrections were recorded in this date range.",
            _ => "No supplies were used in this date range."
        };

        [RelayCommand]
        public void SetMovementFilter(string filter)
        {
            MovementFilter = filter;
        }

        private void ApplyMovementFilter()
        {
            IEnumerable<StockMovementRow> source = MovementFilter switch
            {
                MovementFilterIn => _allMovements.Where(m => !m.IsUsage && m.QuantityChange > 0),
                MovementFilterOut => _allMovements.Where(m => !m.IsUsage && m.QuantityChange < 0),
                _ => _allMovements.Where(m => m.IsUsage)
            };

            RestockHistory = new ObservableCollection<StockMovementRow>(source);
        }

        public ReportsViewModel(WashTrackContext context)
        {
            _context = context;
        }

        [RelayCommand]
        public async Task LoadReportsAsync()
        {
            IsLoading = true;

            var list = await _context.Transactions
               .Include(t => t.Customer)
               .Include(t => t.Items)
               .ThenInclude(i => i.Service)
               .Where(t => t.CreatedAt.Date >= StartDate.Date &&
                t.CreatedAt.Date <= EndDate.Date)
               .OrderByDescending(t => t.CreatedAt)
               .ToListAsync();

            Transactions = new ObservableCollection<Transaction>(list);
            TotalTransactions = list.Count;
            TotalRevenue = list
                .Where(t => t.Status == "Completed")
                .Sum(t => t.TotalCost);
            AverageTransactionValue = TotalTransactions > 0
                ? TotalRevenue / TotalTransactions
                : 0;

            await LoadInventoryReportAsync();

            IsLoading = false;
        }

        // Builds the full stock position per item for the range, plus the
        // movement log underneath it.
        private async Task LoadInventoryReportAsync()
        {
            var rangeStart = StartDate.Date;
            var rangeEnd = EndDate.Date;

            var activeItems = await _context.Inventories
                .AsNoTracking()
                .Where(i => i.IsActive)
                .OrderBy(i => i.ItemName)
                .ToListAsync();

            // Everything from the start of the range onward — deliberately not
            // capped at EndDate. Closing stock has to be wound back from
            // CurrentStock, which is always "now", so movements made *after*
            // the range are needed to undo them.
            //
            // Summed in memory rather than with a SQL GROUP BY: EF stores
            // decimal as TEXT on SQLite, so aggregating server-side pushes
            // these through SQLite's own numeric coercion. In memory they stay
            // decimal the whole way.
            var usageRows = await _context.InventoryUsageHistories
                .AsNoTracking()
                .Where(h => h.UsageDate.Date >= rangeStart)
                .Select(h => new
                {
                    h.InventoryId,
                    h.UsageDate,
                    h.QuantityUsed,
                    h.TransactionId,
                    h.Notes,
                    ItemName = h.Inventory != null ? h.Inventory.ItemName : null,
                    Unit = h.Inventory != null ? h.Inventory.Unit : null
                })
                .ToListAsync();

            var restockRows = await _context.InventoryRestockHistories
                .AsNoTracking()
                .Include(h => h.Inventory)
                .Where(h => h.RestockDate.Date >= rangeStart)
                .ToListAsync();

            var rows = activeItems
                .Select(i =>
                {
                    var itemUsage = usageRows.Where(u => u.InventoryId == i.InventoryId).ToList();
                    var itemRestocks = restockRows.Where(r => r.InventoryId == i.InventoryId).ToList();

                    bool InRange(DateTime d) => d.Date >= rangeStart && d.Date <= rangeEnd;

                    decimal used = itemUsage
                        .Where(u => InRange(u.UsageDate))
                        .Sum(u => u.QuantityUsed);

                    var movesInRange = itemRestocks.Where(r => InRange(r.RestockDate)).ToList();
                    decimal stockIn = movesInRange.Where(r => r.QuantityChange > 0).Sum(r => r.QuantityChange);
                    decimal adjustments = movesInRange.Where(r => r.QuantityChange < 0).Sum(r => r.QuantityChange);

                    // Wind "now" back to the end of the range: undo anything
                    // that moved after it. When EndDate is today (the default)
                    // both of these are zero and closing == CurrentStock.
                    decimal usedAfter = itemUsage
                        .Where(u => u.UsageDate.Date > rangeEnd)
                        .Sum(u => u.QuantityUsed);
                    decimal movedAfter = itemRestocks
                        .Where(r => r.RestockDate.Date > rangeEnd)
                        .Sum(r => r.QuantityChange);

                    decimal closing = i.CurrentStock + usedAfter - movedAfter;

                    // Rearranged from closing = opening + in + adj − used.
                    decimal opening = closing - stockIn - adjustments + used;

                    return new InventoryReconciliationRow
                    {
                        ItemName = i.ItemName,
                        Unit = i.Unit,
                        Opening = opening,
                        StockIn = stockIn,
                        Used = used,
                        Adjustments = adjustments,
                        Closing = closing,
                        IsLowStock = i.IsLowStock
                    };
                })
                .OrderByDescending(r => r.Used)
                .ToList();

            InventoryUsage = new ObservableCollection<InventoryReconciliationRow>(rows);
            LowStockCount = activeItems.Count(i => i.IsLowStock);

            // Movement log. Not filtered to active items: deactivated ones keep
            // their history on purpose, and dropping their movements would hide
            // real stock changes from the period.
            var restocks = restockRows
                .Where(h => h.RestockDate.Date >= rangeStart && h.RestockDate.Date <= rangeEnd)
                .ToList();

            var movements = restocks.Select(h => new StockMovementRow
            {
                ItemName = h.Inventory?.ItemName ?? "(deleted item)",
                Unit = h.Inventory?.Unit ?? string.Empty,
                QuantityChange = h.QuantityChange,
                MovementDate = h.RestockDate,
                Notes = h.Notes,
                IsUsage = false
            });

            // Usage is stored as the positive amount consumed; negated here so
            // every row in the log reads in the same direction — a minus on a
            // card always means stock left.
            var usage = usageRows
                .Where(u => u.UsageDate.Date >= rangeStart && u.UsageDate.Date <= rangeEnd)
                .Select(u => new StockMovementRow
                {
                    ItemName = u.ItemName ?? "(deleted item)",
                    Unit = u.Unit ?? string.Empty,
                    QuantityChange = -u.QuantityUsed,
                    MovementDate = u.UsageDate,
                    Notes = u.Notes,
                    IsUsage = true,
                    TransactionId = u.TransactionId
                });

            _allMovements = movements
                .Concat(usage)
                .OrderByDescending(m => m.MovementDate)
                .ToList();

            // Honours whichever segment is currently selected, so regenerating
            // the report doesn't silently throw the user back to "All".
            ApplyMovementFilter();

            CorrectionCount = restocks.Count(r => r.QuantityChange < 0);
        }

        [RelayCommand]
        public async Task FilterAsync()
        {
            await LoadReportsAsync();
        }

        // One button, two states: switches the report body between Sales
        // and Inventory. Both share the same date range and data already
        // loaded, so this never needs to re-query.
        [RelayCommand]
        public void ToggleReportView()
        {
            ShowingInventoryReport = !ShowingInventoryReport;
            ReportToggleText = ShowingInventoryReport ? "View Sales Report" : "View Inventory Report";
        }
    }
}