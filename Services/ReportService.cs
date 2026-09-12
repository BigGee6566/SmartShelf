using System;
using System.Collections.Generic;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    // ------------------------------------------------------------------
    // Report row shapes
    // ------------------------------------------------------------------

    public class TopSellerRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal GrossProfit { get; set; }
    }

    public class LowStockRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string SupplierName { get; set; }
        public int? SupplierId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }
        public int SuggestedOrderQty { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    public class ExpiryRow
    {
        public int StockBatchId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string BatchCode { get; set; }
        public int QuantityRemaining { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public decimal ValueAtRisk { get; set; }
        public bool AlreadyExpired { get { return DaysToExpiry < 0; } }
    }

    public class WasteRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int UnitsWasted { get; set; }
        public decimal CostOfWaste { get; set; }
        public int Incidents { get; set; }
    }

    public class SupplierPerformanceRow
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public int OrdersPlaced { get; set; }
        public int OrdersDelivered { get; set; }
        public int OrdersOnTime { get; set; }
        public int UnitsOrdered { get; set; }
        public int UnitsReceived { get; set; }
        public decimal OrderValue { get; set; }
        public double? AverageConfirmHours { get; set; }

        /// <summary>Share of ordered units that actually arrived.</summary>
        public double FillRate
        {
            get { return UnitsOrdered == 0 ? 0 : (double)UnitsReceived / UnitsOrdered * 100.0; }
        }

        /// <summary>Share of delivered orders that arrived on or before the expected date.</summary>
        public double OnTimeRate
        {
            get { return OrdersDelivered == 0 ? 0 : (double)OrdersOnTime / OrdersDelivered * 100.0; }
        }
    }

    /// <summary>
    /// The four Key Reports. Every one of them aggregates the ordinary tables the rest of the
    /// system writes to - nothing here reads a stored or pre-computed result, so a sale rung up
    /// on the POS a second ago is already reflected.
    /// </summary>
    public static class ReportService
    {
        // ----------------------------------------------------------
        // 1. Top-selling products
        // ----------------------------------------------------------
        public static List<TopSellerRow> TopSellers(DateTime from, DateTime to, int take = 25)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1); // inclusive of the whole end day

            using (var db = new SmartShelfContext())
            {
                // Cost is read from the product rather than the line, because a sale line stores
                // the selling price it charged, not what the stock cost at the time.
                var rows = (from line in db.SaleLines
                            join sale in db.Sales on line.SaleId equals sale.SaleId
                            join product in db.Products on line.ProductId equals product.ProductId
                            join category in db.Categories on product.CategoryId equals category.CategoryId
                            where sale.Status == SaleStatus.Completed
                                  && sale.SaleDate >= start && sale.SaleDate < end
                            group new { line, product, category } by new
                            {
                                line.ProductId,
                                ProductName = product.Name,
                                CategoryName = category.Name,
                                product.CostPrice
                            } into g
                            select new
                            {
                                g.Key.ProductId,
                                g.Key.ProductName,
                                g.Key.CategoryName,
                                g.Key.CostPrice,
                                UnitsSold = g.Sum(x => x.line.Quantity),
                                Revenue = g.Sum(x => x.line.LineTotal)
                            })
                           .OrderByDescending(r => r.Revenue)
                           .Take(take)
                           .ToList();

                return rows.Select(r => new TopSellerRow
                {
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    CategoryName = r.CategoryName,
                    UnitsSold = r.UnitsSold,
                    Revenue = decimal.Round(r.Revenue, 2),
                    GrossProfit = decimal.Round(r.Revenue - (r.CostPrice * r.UnitsSold), 2)
                }).ToList();
            }
        }

        // ----------------------------------------------------------
        // 2. Low-stock alerts
        // ----------------------------------------------------------
        public static List<LowStockRow> LowStockAlerts()
        {
            using (var db = new SmartShelfContext())
            {
                var rows = (from p in db.Products
                            join c in db.Categories on p.CategoryId equals c.CategoryId
                            from s in db.Suppliers.Where(x => x.SupplierId == p.SupplierId).DefaultIfEmpty()
                            where p.IsActive && p.QuantityOnHand <= p.ReorderLevel
                            orderby p.QuantityOnHand
                            select new
                            {
                                p.ProductId,
                                ProductName = p.Name,
                                CategoryName = c.Name,
                                SupplierName = s == null ? null : s.Name,
                                SupplierId = s == null ? (int?)null : s.SupplierId,
                                p.QuantityOnHand,
                                p.ReorderLevel,
                                p.CostPrice
                            }).ToList();

                return rows.Select(r =>
                {
                    // Order back up to three times the reorder level: enough cover to trade
                    // through the supplier's lead time without over-stocking perishables.
                    var suggested = Math.Max(1, (r.ReorderLevel * 3) - r.QuantityOnHand);
                    return new LowStockRow
                    {
                        ProductId = r.ProductId,
                        ProductName = r.ProductName,
                        CategoryName = r.CategoryName,
                        SupplierName = r.SupplierName ?? "(no preferred supplier)",
                        SupplierId = r.SupplierId,
                        QuantityOnHand = r.QuantityOnHand,
                        ReorderLevel = r.ReorderLevel,
                        SuggestedOrderQty = suggested,
                        EstimatedCost = decimal.Round(r.CostPrice * suggested, 2)
                    };
                }).ToList();
            }
        }

        // ----------------------------------------------------------
        // 3a. Expiry - batches at or past their date
        // ----------------------------------------------------------
        public static List<ExpiryRow> ExpiringBatches(int withinDays)
        {
            var cutoff = DateTime.Today.AddDays(withinDays);

            using (var db = new SmartShelfContext())
            {
                var rows = (from b in db.StockBatches
                            join p in db.Products on b.ProductId equals p.ProductId
                            where b.QuantityRemaining > 0
                                  && b.ExpiryDate != null
                                  && b.ExpiryDate <= cutoff
                            orderby b.ExpiryDate
                            select new
                            {
                                b.StockBatchId,
                                b.ProductId,
                                ProductName = p.Name,
                                b.BatchCode,
                                b.QuantityRemaining,
                                b.ExpiryDate,
                                b.UnitCost
                            }).ToList();

                return rows.Select(r => new ExpiryRow
                {
                    StockBatchId = r.StockBatchId,
                    ProductId = r.ProductId,
                    ProductName = r.ProductName,
                    BatchCode = r.BatchCode,
                    QuantityRemaining = r.QuantityRemaining,
                    ExpiryDate = r.ExpiryDate,
                    DaysToExpiry = (int)(r.ExpiryDate.Value.Date - DateTime.Today).TotalDays,
                    ValueAtRisk = decimal.Round(r.UnitCost * r.QuantityRemaining, 2)
                }).ToList();
            }
        }

        // ----------------------------------------------------------
        // 3b. Waste - what was actually written off
        // ----------------------------------------------------------
        public static List<WasteRow> WasteSummary(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            using (var db = new SmartShelfContext())
            {
                return (from m in db.StockMovements
                        join p in db.Products on m.ProductId equals p.ProductId
                        where m.MovementType == MovementType.Waste
                              && m.CreatedAt >= start && m.CreatedAt < end
                        group m by new { m.ProductId, ProductName = p.Name } into g
                        select new WasteRow
                        {
                            ProductId = g.Key.ProductId,
                            ProductName = g.Key.ProductName,
                            // Waste deltas are negative, so negate to report units lost.
                            UnitsWasted = -g.Sum(x => x.QuantityDelta),
                            CostOfWaste = g.Sum(x => -x.QuantityDelta * x.UnitCostAtTime),
                            Incidents = g.Count()
                        })
                       .OrderByDescending(r => r.CostOfWaste)
                       .ToList();
            }
        }

        // ----------------------------------------------------------
        // 4. Supplier performance
        // ----------------------------------------------------------
        public static List<SupplierPerformanceRow> SupplierPerformance(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            using (var db = new SmartShelfContext())
            {
                // Pulled back before the timing maths: date arithmetic across a nullable
                // DeliveredAt is clearer in memory than in a translated LINQ expression.
                var orders = (from o in db.PurchaseOrders
                              join s in db.Suppliers on o.SupplierId equals s.SupplierId
                              where o.CreatedAt >= start && o.CreatedAt < end
                                    && o.Status != PurchaseOrderStatus.Draft
                                    && o.Status != PurchaseOrderStatus.Cancelled
                              select new
                              {
                                  o.PurchaseOrderId,
                                  o.SupplierId,
                                  SupplierName = s.Name,
                                  o.Status,
                                  o.SentAt,
                                  o.ConfirmedAt,
                                  o.ExpectedDate,
                                  o.DeliveredAt
                              }).ToList();

                if (orders.Count == 0) return new List<SupplierPerformanceRow>();

                var orderIds = orders.Select(o => o.PurchaseOrderId).ToList();

                var lineTotals = db.PurchaseOrderLines
                    .Where(l => orderIds.Contains(l.PurchaseOrderId))
                    .GroupBy(l => l.PurchaseOrderId)
                    .Select(g => new
                    {
                        PurchaseOrderId = g.Key,
                        Ordered = g.Sum(x => x.QuantityOrdered),
                        Received = g.Sum(x => x.QuantityReceived),
                        Value = g.Sum(x => x.QuantityOrdered * x.UnitCost)
                    })
                    .ToDictionary(x => x.PurchaseOrderId);

                return orders
                    .GroupBy(o => new { o.SupplierId, o.SupplierName })
                    .Select(g =>
                    {
                        var delivered = g.Where(o => o.Status == PurchaseOrderStatus.Delivered).ToList();

                        var confirmHours = g
                            .Where(o => o.ConfirmedAt.HasValue && o.SentAt.HasValue)
                            .Select(o => (o.ConfirmedAt.Value - o.SentAt.Value).TotalHours)
                            .ToList();

                        return new SupplierPerformanceRow
                        {
                            SupplierId = g.Key.SupplierId,
                            SupplierName = g.Key.SupplierName,
                            OrdersPlaced = g.Count(),
                            OrdersDelivered = delivered.Count,
                            OrdersOnTime = delivered.Count(o => o.DeliveredAt.HasValue
                                                                && o.DeliveredAt.Value.Date <= o.ExpectedDate.Date),
                            UnitsOrdered = g.Sum(o => lineTotals.ContainsKey(o.PurchaseOrderId)
                                                      ? lineTotals[o.PurchaseOrderId].Ordered : 0),
                            UnitsReceived = g.Sum(o => lineTotals.ContainsKey(o.PurchaseOrderId)
                                                       ? lineTotals[o.PurchaseOrderId].Received : 0),
                            OrderValue = decimal.Round(g.Sum(o => lineTotals.ContainsKey(o.PurchaseOrderId)
                                                                  ? lineTotals[o.PurchaseOrderId].Value : 0m), 2),
                            AverageConfirmHours = confirmHours.Count == 0 ? (double?)null : confirmHours.Average()
                        };
                    })
                    .OrderByDescending(r => r.OrderValue)
                    .ToList();
            }
        }

        // ----------------------------------------------------------
        // Dashboard figures
        // ----------------------------------------------------------
        public static decimal RevenueBetween(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            using (var db = new SmartShelfContext())
            {
                return db.Sales
                    .Where(s => s.Status == SaleStatus.Completed && s.SaleDate >= start && s.SaleDate < end)
                    .Sum(s => (decimal?)s.Total) ?? 0m;
            }
        }

        public static int SaleCountBetween(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            using (var db = new SmartShelfContext())
            {
                return db.Sales.Count(s => s.Status == SaleStatus.Completed
                                           && s.SaleDate >= start && s.SaleDate < end);
            }
        }

        public static int ActiveProductCount()
        {
            using (var db = new SmartShelfContext())
            {
                return db.Products.Count(p => p.IsActive);
            }
        }

        public static int CategoryCount()
        {
            using (var db = new SmartShelfContext())
            {
                return db.Categories.Count(c => c.IsActive);
            }
        }
    }
}
