using System;
using System.Collections.Generic;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>One thing that happened, normalised across every table that records history.</summary>
    public class ActivityEntry
    {
        public DateTime When { get; set; }
        public string Who { get; set; }
        public string Role { get; set; }
        public string Category { get; set; }
        public string Action { get; set; }
        public string Detail { get; set; }
        public string Reference { get; set; }

        /// <summary>Pill colour, so the grid reads at a glance rather than needing to be studied.</summary>
        public string PillCss
        {
            get
            {
                switch (Category)
                {
                    case ActivityService.CatSale: return "pill-green";
                    case ActivityService.CatStock: return "pill-blue";
                    case ActivityService.CatWaste: return "pill-red";
                    case ActivityService.CatCashUp: return "pill-amber";
                    case ActivityService.CatOrder: return "pill-blue";
                    case ActivityService.CatAccess: return "pill-grey";
                    default: return "pill-grey";
                }
            }
        }
    }

    /// <summary>Whole-system row counts shown on the activity log header.</summary>
    public class ActivityTotals
    {
        public int Sales { get; set; }
        public int StockMovements { get; set; }
        public int CashUps { get; set; }
        public int UnreviewedVariances { get; set; }
    }

    /// <summary>
    /// Builds the admin activity feed out of the history the system already keeps. Nothing here
    /// writes, and no new audit table was introduced - StockMovement, Sale, CashUp, PurchaseOrder
    /// and User.LastLoginAt between them already record who did what and when.
    /// <para>
    /// Known blind spot, stated on the page itself: sign-outs, failed sign-in attempts and edits
    /// to a user account are not stored anywhere, so they cannot appear here. Absence of an entry
    /// is not evidence that nothing happened.
    /// </para>
    /// </summary>
    public static class ActivityService
    {
        public const string CatSale = "Sale";
        public const string CatStock = "Stock";
        public const string CatWaste = "Waste";
        public const string CatCashUp = "Cash-up";
        public const string CatOrder = "Order";
        public const string CatAccess = "Access";

        public static readonly string[] Categories =
        {
            CatSale, CatStock, CatWaste, CatCashUp, CatOrder, CatAccess
        };

        /// <summary>
        /// Merges the sources into one descending timeline. Each source is filtered and capped in
        /// SQL first - with thousands of movements an unbounded union would pull the whole history
        /// into memory to show one page of it.
        /// </summary>
        public static List<ActivityEntry> GetActivity(DateTime? from, DateTime? to, int? userId,
                                                      string category, int maxPerSource = 400)
        {
            var start = from.HasValue ? from.Value.Date : (DateTime?)null;
            var end = to.HasValue ? to.Value.Date.AddDays(1) : (DateTime?)null;
            var wanted = string.IsNullOrEmpty(category) ? null : category;

            var entries = new List<ActivityEntry>();

            using (var db = new SmartShelfContext())
            {
                if (wanted == null || wanted == CatStock || wanted == CatWaste)
                    entries.AddRange(FromStockMovements(db, start, end, userId, wanted, maxPerSource));

                if (wanted == null || wanted == CatSale)
                    entries.AddRange(FromSales(db, start, end, userId, maxPerSource));

                if (wanted == null || wanted == CatCashUp)
                    entries.AddRange(FromCashUps(db, start, end, userId, maxPerSource));

                if (wanted == null || wanted == CatOrder)
                    entries.AddRange(FromPurchaseOrders(db, start, end, userId, maxPerSource));

                if (wanted == null || wanted == CatAccess)
                    entries.AddRange(FromLogins(db, start, end, userId, maxPerSource));
            }

            return entries.OrderByDescending(e => e.When).ToList();
        }

        /// <summary>Whole-system counts for the header tiles.</summary>
        public static ActivityTotals GetTotals()
        {
            using (var db = new SmartShelfContext())
            {
                return new ActivityTotals
                {
                    Sales = db.Sales.Count(),
                    StockMovements = db.StockMovements.Count(),
                    CashUps = db.CashUps.Count(),
                    UnreviewedVariances = db.CashUps.Count(c => c.Variance != 0 && c.ReviewedAt == null)
                };
            }
        }

        private static IEnumerable<ActivityEntry> FromStockMovements(SmartShelfContext db, DateTime? start,
            DateTime? end, int? userId, string wanted, int cap)
        {
            var q = db.StockMovements.AsNoTracking().AsQueryable();

            if (start.HasValue) q = q.Where(m => m.CreatedAt >= start.Value);
            if (end.HasValue) q = q.Where(m => m.CreatedAt < end.Value);
            if (userId.HasValue) q = q.Where(m => m.UserId == userId.Value);

            // A sale is reported once, from the Sale table. Its per-line movements are excluded
            // here in every view, so filtering to "Sale" shows the same events as the full
            // timeline does rather than an inconsistent, more granular list.
            if (wanted == CatWaste) q = q.Where(m => m.MovementType == MovementType.Waste);
            else if (wanted == CatStock) q = q.Where(m => m.MovementType != MovementType.Waste
                                                          && m.MovementType != MovementType.Sale);
            else q = q.Where(m => m.MovementType != MovementType.Sale);

            var rows = q.OrderByDescending(m => m.CreatedAt)
                .Take(cap)
                .Select(m => new
                {
                    m.CreatedAt,
                    m.MovementType,
                    m.QuantityDelta,
                    m.QuantityAfter,
                    m.Reason,
                    ProductName = m.Product.Name,
                    UserName = m.User.FullName,
                    RoleName = m.User.Role.Name
                })
                .ToList();

            return rows.Select(r => new ActivityEntry
            {
                When = r.CreatedAt,
                Who = r.UserName,
                Role = r.RoleName,
                Category = r.MovementType == MovementType.Waste ? CatWaste
                         : r.MovementType == MovementType.Sale ? CatSale : CatStock,
                Action = MovementLabel(r.MovementType),
                Detail = string.Format("{0}: {1}{2} → {3} on hand{4}",
                    r.ProductName,
                    r.QuantityDelta > 0 ? "+" : string.Empty,
                    r.QuantityDelta,
                    r.QuantityAfter,
                    string.IsNullOrWhiteSpace(r.Reason) ? string.Empty : ". " + r.Reason),
                Reference = string.Empty
            });
        }

        private static IEnumerable<ActivityEntry> FromSales(SmartShelfContext db, DateTime? start,
            DateTime? end, int? userId, int cap)
        {
            var q = db.Sales.AsNoTracking().AsQueryable();

            if (start.HasValue) q = q.Where(s => s.SaleDate >= start.Value);
            if (end.HasValue) q = q.Where(s => s.SaleDate < end.Value);
            if (userId.HasValue) q = q.Where(s => s.CashierUserId == userId.Value);

            var rows = q.OrderByDescending(s => s.SaleDate)
                .Take(cap)
                .Select(s => new
                {
                    s.SaleDate,
                    s.ReceiptNumber,
                    s.Total,
                    s.PaymentMethod,
                    s.Status,
                    LineCount = s.Lines.Count(),
                    UserName = s.CashierUser.FullName,
                    RoleName = s.CashierUser.Role.Name
                })
                .ToList();

            return rows.Select(r => new ActivityEntry
            {
                When = r.SaleDate,
                Who = r.UserName,
                Role = r.RoleName,
                Category = CatSale,
                Action = r.Status == SaleStatus.Voided ? "Sale voided" : "Sale completed",
                Detail = string.Format("{0} item line(s), R {1:N2}, paid by {2}",
                                       r.LineCount, r.Total, r.PaymentMethod),
                Reference = r.ReceiptNumber
            });
        }

        private static IEnumerable<ActivityEntry> FromCashUps(SmartShelfContext db, DateTime? start,
            DateTime? end, int? userId, int cap)
        {
            var q = db.CashUps.AsNoTracking().AsQueryable();

            if (start.HasValue) q = q.Where(c => c.CreatedAt >= start.Value);
            if (end.HasValue) q = q.Where(c => c.CreatedAt < end.Value);
            if (userId.HasValue) q = q.Where(c => c.UserId == userId.Value || c.ReviewedByUserId == userId.Value);

            var rows = q.OrderByDescending(c => c.CreatedAt)
                .Take(cap)
                .Select(c => new
                {
                    c.CreatedAt,
                    c.ExpectedCash,
                    c.CountedCash,
                    c.Variance,
                    c.ReviewedAt,
                    c.ReviewNote,
                    TillNumber = c.Shift.TillNumber,
                    UserName = c.User.FullName,
                    RoleName = c.User.Role.Name,
                    ReviewerName = c.ReviewedByUser == null ? null : c.ReviewedByUser.FullName,
                    ReviewerRole = c.ReviewedByUser == null ? null : c.ReviewedByUser.Role.Name
                })
                .ToList();

            var result = new List<ActivityEntry>();

            foreach (var r in rows)
            {
                result.Add(new ActivityEntry
                {
                    When = r.CreatedAt,
                    Who = r.UserName,
                    Role = r.RoleName,
                    Category = CatCashUp,
                    Action = r.Variance == 0m ? "Drawer counted, balanced" : "Drawer counted, variance",
                    Detail = string.Format("{0}: expected R {1:N2}, counted R {2:N2}, variance R {3:N2}",
                                           r.TillNumber, r.ExpectedCash, r.CountedCash, r.Variance),
                    Reference = string.Empty
                });

                // The sign-off is a separate act by a different person, so it gets its own line.
                if (r.ReviewedAt.HasValue)
                {
                    result.Add(new ActivityEntry
                    {
                        When = r.ReviewedAt.Value,
                        Who = r.ReviewerName ?? "(unknown)",
                        Role = r.ReviewerRole ?? string.Empty,
                        Category = CatCashUp,
                        Action = "Variance reviewed",
                        Detail = string.Format("Signed off a variance of R {0:N2}{1}",
                            r.Variance,
                            string.IsNullOrWhiteSpace(r.ReviewNote) ? string.Empty : ". " + r.ReviewNote),
                        Reference = string.Empty
                    });
                }
            }

            return result;
        }

        private static IEnumerable<ActivityEntry> FromPurchaseOrders(SmartShelfContext db, DateTime? start,
            DateTime? end, int? userId, int cap)
        {
            var q = db.PurchaseOrders.AsNoTracking().AsQueryable();
            if (userId.HasValue) q = q.Where(o => o.CreatedByUserId == userId.Value);

            var rows = q.OrderByDescending(o => o.CreatedAt)
                .Take(cap)
                .Select(o => new
                {
                    o.OrderNumber,
                    o.CreatedAt,
                    o.SentAt,
                    o.ConfirmedAt,
                    o.DeliveredAt,
                    o.Status,
                    o.SupplierNote,
                    SupplierName = o.Supplier.Name,
                    UserName = o.CreatedByUser.FullName,
                    RoleName = o.CreatedByUser.Role.Name
                })
                .ToList();

            var result = new List<ActivityEntry>();

            // One order produces several dated events; each timestamp column is a separate act.
            foreach (var r in rows)
            {
                Action<DateTime?, string, string, string, string> add = (when, who, role, action, detail) =>
                {
                    if (!when.HasValue) return;
                    if (start.HasValue && when.Value < start.Value) return;
                    if (end.HasValue && when.Value >= end.Value) return;

                    result.Add(new ActivityEntry
                    {
                        When = when.Value,
                        Who = who,
                        Role = role,
                        Category = CatOrder,
                        Action = action,
                        Detail = detail,
                        Reference = r.OrderNumber
                    });
                };

                add(r.CreatedAt, r.UserName, r.RoleName, "Order raised", "Raised for " + r.SupplierName);
                add(r.SentAt, r.UserName, r.RoleName, "Order sent", "Sent to " + r.SupplierName);
                add(r.ConfirmedAt, r.SupplierName, RoleNames.Supplier, "Order confirmed",
                    string.IsNullOrWhiteSpace(r.SupplierNote) ? "Confirmed by supplier" : r.SupplierNote);
                add(r.DeliveredAt, r.UserName, r.RoleName, "Goods received",
                    "Delivery counted in against " + r.OrderNumber);
            }

            return result;
        }

        /// <summary>
        /// Sign-ins are the one access event the system stores, and only the most recent one per
        /// account - User.LastLoginAt is overwritten each time, so this is a current-state
        /// snapshot rather than a history.
        /// </summary>
        private static IEnumerable<ActivityEntry> FromLogins(SmartShelfContext db, DateTime? start,
            DateTime? end, int? userId, int cap)
        {
            var q = db.Users.AsNoTracking().Where(u => u.LastLoginAt != null);

            if (start.HasValue) q = q.Where(u => u.LastLoginAt >= start.Value);
            if (end.HasValue) q = q.Where(u => u.LastLoginAt < end.Value);
            if (userId.HasValue) q = q.Where(u => u.UserId == userId.Value);

            var rows = q.OrderByDescending(u => u.LastLoginAt)
                .Take(cap)
                .Select(u => new
                {
                    u.LastLoginAt,
                    u.FullName,
                    u.Username,
                    RoleName = u.Role.Name
                })
                .ToList();

            return rows.Select(r => new ActivityEntry
            {
                When = r.LastLoginAt.Value,
                Who = r.FullName,
                Role = r.RoleName,
                Category = CatAccess,
                Action = "Signed in",
                Detail = "Most recent sign-in for " + r.Username,
                Reference = string.Empty
            });
        }

        private static string MovementLabel(MovementType type)
        {
            switch (type)
            {
                case MovementType.Sale: return "Stock sold";
                case MovementType.Adjustment: return "Stock adjusted";
                case MovementType.Receipt: return "Stock received";
                case MovementType.Waste: return "Stock written off";
                case MovementType.VoidReversal: return "Stock returned (void)";
                default: return type.ToString();
            }
        }
    }
}
