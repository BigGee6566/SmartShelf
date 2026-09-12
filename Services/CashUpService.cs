using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>Everything the cash-up screen needs to show before the drawer is counted.</summary>
    public class ShiftSummary
    {
        public int ShiftId { get; set; }
        public string TillNumber { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal OpeningFloat { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal MobileSales { get; set; }
        public int SaleCount { get; set; }

        /// <summary>Float plus cash takings. Calculated here, displayed read-only, never posted back.</summary>
        public decimal ExpectedCash { get { return OpeningFloat + CashSales; } }
    }

    public static class CashUpService
    {
        /// <summary>The cashier's currently open shift, or null if they have not rung anything up.</summary>
        public static ShiftSummary GetOpenShiftSummary(int cashierUserId)
        {
            using (var db = new SmartShelfContext())
            {
                var shift = db.Shifts
                    .FirstOrDefault(s => s.CashierUserId == cashierUserId && s.Status == ShiftStatus.Open);

                return shift == null ? null : Summarise(db, shift);
            }
        }

        private static ShiftSummary Summarise(SmartShelfContext db, Shift shift)
        {
            // One grouped query rather than three: the expected total has to be exact and
            // cheap enough to recompute on every render.
            var byMethod = db.Sales
                .Where(s => s.ShiftId == shift.ShiftId && s.Status == SaleStatus.Completed)
                .GroupBy(s => s.PaymentMethod)
                .Select(g => new { Method = g.Key, Total = g.Sum(x => (decimal?)x.Total) ?? 0m, Count = g.Count() })
                .ToList();

            return new ShiftSummary
            {
                ShiftId = shift.ShiftId,
                TillNumber = shift.TillNumber,
                OpenedAt = shift.OpenedAt,
                OpeningFloat = shift.OpeningFloat,
                CashSales = byMethod.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Total),
                CardSales = byMethod.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.Total),
                MobileSales = byMethod.Where(x => x.Method == PaymentMethod.Mobile).Sum(x => x.Total),
                SaleCount = byMethod.Sum(x => x.Count)
            };
        }

        /// <summary>
        /// Closes the shift against a counted drawer. The only figure accepted from the browser is
        /// <paramref name="countedCash"/>; the expected total is recomputed here from the sales
        /// themselves, so a tampered form cannot make a variance disappear.
        /// </summary>
        public static CashUp CompleteCashUp(int shiftId, decimal countedCash, string notes, int userId)
        {
            if (countedCash < 0)
                throw new BusinessRuleException("The counted cash cannot be negative.");
            if (countedCash > 1000000m)
                throw new BusinessRuleException("That counted amount looks wrong - please check and re-enter it.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var shift = db.Shifts.FirstOrDefault(s => s.ShiftId == shiftId);
                if (shift == null) throw new BusinessRuleException("That shift no longer exists.");

                // A cashier may only cash up their own drawer.
                if (shift.CashierUserId != userId)
                    throw new BusinessRuleException("You can only cash up a shift that is your own.");

                if (shift.Status == ShiftStatus.Closed)
                    throw new BusinessRuleException("This shift has already been cashed up.");

                var summary = Summarise(db, shift);
                var expected = decimal.Round(summary.ExpectedCash, 2);
                var counted = decimal.Round(countedCash, 2);

                var cashUp = new CashUp
                {
                    ShiftId = shift.ShiftId,
                    UserId = userId,
                    ExpectedCash = expected,
                    CountedCash = counted,
                    Variance = counted - expected,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    CreatedAt = DateTime.Now
                };

                db.CashUps.Add(cashUp);

                shift.Status = ShiftStatus.Closed;
                shift.ClosedAt = DateTime.Now;

                db.SaveChanges();
                tx.Commit();

                return cashUp;
            }
        }

        /// <summary>Manager sign-off on a variance. Records who reviewed it and when.</summary>
        public static void ReviewCashUp(int cashUpId, string reviewNote, int managerUserId)
        {
            using (var db = new SmartShelfContext())
            {
                var cashUp = db.CashUps.FirstOrDefault(c => c.CashUpId == cashUpId);
                if (cashUp == null) throw new BusinessRuleException("That cash-up no longer exists.");
                if (cashUp.ReviewedAt.HasValue)
                    throw new BusinessRuleException("That cash-up has already been reviewed.");
                if (string.IsNullOrWhiteSpace(reviewNote))
                    throw new BusinessRuleException("Please record what you found before signing the variance off.");

                cashUp.ReviewedByUserId = managerUserId;
                cashUp.ReviewedAt = DateTime.Now;
                cashUp.ReviewNote = reviewNote.Trim();

                db.SaveChanges();
            }
        }

        public static List<CashUp> GetCashUps(bool unreviewedVariancesOnly, int take = 200)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.CashUps
                    .Include(c => c.User)
                    .Include(c => c.Shift)
                    .Include(c => c.ReviewedByUser)
                    .AsNoTracking()
                    .AsQueryable();

                if (unreviewedVariancesOnly)
                    query = query.Where(c => c.Variance != 0 && c.ReviewedAt == null);

                return query.OrderByDescending(c => c.CreatedAt).Take(take).ToList();
            }
        }

        public static CashUp GetMostRecent()
        {
            using (var db = new SmartShelfContext())
            {
                return db.CashUps
                    .Include(c => c.User)
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefault();
            }
        }

        public static int CountUnreviewedVariances()
        {
            using (var db = new SmartShelfContext())
            {
                return db.CashUps.Count(c => c.Variance != 0 && c.ReviewedAt == null);
            }
        }
    }
}
