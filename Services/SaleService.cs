using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>What the till asks for: a product and how many. Nothing else is trusted.</summary>
    [Serializable]
    public class CartLine
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Display-only copies so the cart can render without hitting the database on every
        // postback. They are recomputed from the database before anything is saved.
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }
    }

    /// <summary>Server-calculated totals for a cart. The only figures the POS is allowed to show.</summary>
    public class SaleTotals
    {
        public decimal Subtotal { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    public class CompletedSale
    {
        public int SaleId { get; set; }
        public string ReceiptNumber { get; set; }
        public decimal Total { get; set; }
        public decimal ChangeDue { get; set; }
    }

    public static class SaleService
    {
        /// <summary>
        /// Prices the cart from the database, ignoring whatever the browser last displayed.
        /// Used both to render the running total and, again, at the moment of payment.
        /// </summary>
        public static SaleTotals CalculateTotals(IEnumerable<CartLine> cart)
        {
            var totals = new SaleTotals();
            var lines = (cart ?? Enumerable.Empty<CartLine>()).Where(l => l.Quantity > 0).ToList();
            if (lines.Count == 0) return totals;

            var ids = lines.Select(l => l.ProductId).Distinct().ToList();

            using (var db = new SmartShelfContext())
            {
                var products = db.Products
                    .Where(p => ids.Contains(p.ProductId))
                    .ToDictionary(p => p.ProductId);

                foreach (var line in lines)
                {
                    Product product;
                    if (!products.TryGetValue(line.ProductId, out product)) continue;

                    var lineTotal = decimal.Round(product.SellingPrice * line.Quantity, 2);
                    totals.Subtotal += lineTotal;
                    totals.Vat += decimal.Round(lineTotal * product.VatRate, 2);
                    totals.ItemCount += line.Quantity;
                }
            }

            totals.Subtotal = decimal.Round(totals.Subtotal, 2);
            totals.Vat = decimal.Round(totals.Vat, 2);
            totals.Total = totals.Subtotal + totals.Vat;
            return totals;
        }

        /// <summary>
        /// Commits a sale. The browser supplies only the cart contents, the payment method and
        /// the cash actually handed over; the price of every line, the VAT, the total and the
        /// change due are all recalculated here from the database. Stock is checked and
        /// decremented inside the same transaction, so a sale that would push any product below
        /// zero is rejected outright rather than partially applied.
        /// </summary>
        public static CompletedSale CompleteSale(IEnumerable<CartLine> cart, PaymentMethod paymentMethod,
                                                 decimal amountTendered, int cashierUserId)
        {
            var lines = (cart ?? Enumerable.Empty<CartLine>()).Where(l => l.Quantity > 0).ToList();
            if (lines.Count == 0)
                throw new BusinessRuleException("There is nothing in the cart to pay for.");
            if (lines.Any(l => l.Quantity > 999))
                throw new BusinessRuleException("A single line cannot exceed 999 units.");
            if (amountTendered < 0)
                throw new BusinessRuleException("The amount tendered cannot be negative.");

            // Merge duplicates so the stock check sees the true quantity for each product.
            var wanted = lines
                .GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.ReadCommitted))
            {
                var ids = wanted.Select(w => w.ProductId).ToList();
                var products = db.Products
                    .Where(p => ids.Contains(p.ProductId))
                    .ToDictionary(p => p.ProductId);

                // 1. Validate everything before writing anything.
                foreach (var item in wanted)
                {
                    Product product;
                    if (!products.TryGetValue(item.ProductId, out product) || !product.IsActive)
                        throw new BusinessRuleException(
                            "One of the items is no longer on the catalogue. Remove it and try again.");

                    if (product.QuantityOnHand < item.Quantity)
                        throw new InsufficientStockException(product.Name, product.QuantityOnHand, item.Quantity);
                }

                // 2. Price from the database, never from the post.
                decimal subtotal = 0m, vat = 0m;
                var saleLines = new List<SaleLine>();

                foreach (var item in wanted)
                {
                    var product = products[item.ProductId];
                    var lineTotal = decimal.Round(product.SellingPrice * item.Quantity, 2);
                    subtotal += lineTotal;
                    vat += decimal.Round(lineTotal * product.VatRate, 2);

                    saleLines.Add(new SaleLine
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        UnitPrice = product.SellingPrice,
                        Quantity = item.Quantity,
                        VatRate = product.VatRate,
                        LineTotal = lineTotal
                    });
                }

                subtotal = decimal.Round(subtotal, 2);
                vat = decimal.Round(vat, 2);
                var total = subtotal + vat;

                // 3. Cash must cover the bill. Card and mobile settle the exact amount, so there
                //    is never change on those.
                decimal tendered, change;
                if (paymentMethod == PaymentMethod.Cash)
                {
                    if (amountTendered < total)
                        throw new BusinessRuleException(string.Format(
                            "The cash tendered (R {0:N2}) is less than the total due (R {1:N2}).",
                            amountTendered, total));
                    tendered = amountTendered;
                    change = decimal.Round(tendered - total, 2);
                }
                else
                {
                    tendered = total;
                    change = 0m;
                }

                var shift = GetOrOpenShift(db, cashierUserId);

                var sale = new Sale
                {
                    ReceiptNumber = NextReceiptNumber(db),
                    CashierUserId = cashierUserId,
                    ShiftId = shift.ShiftId,
                    SaleDate = DateTime.Now,
                    Subtotal = subtotal,
                    VatAmount = vat,
                    Total = total,
                    PaymentMethod = paymentMethod,
                    AmountTendered = tendered,
                    ChangeDue = change,
                    Status = SaleStatus.Completed
                };

                sale.Lines = saleLines;
                db.Sales.Add(sale);
                db.SaveChanges();

                // 4. Now move the stock, with a movement row per line naming the cashier.
                foreach (var item in wanted)
                {
                    var product = products[item.ProductId];
                    product.QuantityOnHand -= item.Quantity;

                    InventoryService.ConsumeBatches(db, product.ProductId, item.Quantity);

                    db.StockMovements.Add(new StockMovement
                    {
                        ProductId = product.ProductId,
                        MovementType = MovementType.Sale,
                        QuantityDelta = -item.Quantity,
                        QuantityAfter = product.QuantityOnHand,
                        Reason = "POS sale " + sale.ReceiptNumber,
                        UserId = cashierUserId,
                        SaleId = sale.SaleId,
                        UnitCostAtTime = product.CostPrice,
                        CreatedAt = sale.SaleDate
                    });
                }

                db.SaveChanges();
                tx.Commit();

                return new CompletedSale
                {
                    SaleId = sale.SaleId,
                    ReceiptNumber = sale.ReceiptNumber,
                    Total = total,
                    ChangeDue = change
                };
            }
        }

        /// <summary>
        /// Reverses a completed sale: stock goes back, and a VoidReversal movement records who
        /// did it. Used by a manager correcting a mis-rung sale.
        /// </summary>
        public static void VoidSale(int saleId, string reason, int userId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("A reason is required to void a sale.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var sale = db.Sales.Include(s => s.Lines).FirstOrDefault(s => s.SaleId == saleId);
                if (sale == null) throw new BusinessRuleException("That sale no longer exists.");
                if (sale.Status == SaleStatus.Voided)
                    throw new BusinessRuleException("That sale has already been voided.");

                sale.Status = SaleStatus.Voided;

                foreach (var line in sale.Lines)
                {
                    var product = db.Products.FirstOrDefault(p => p.ProductId == line.ProductId);
                    if (product == null) continue;

                    product.QuantityOnHand += line.Quantity;

                    db.StockMovements.Add(new StockMovement
                    {
                        ProductId = product.ProductId,
                        MovementType = MovementType.VoidReversal,
                        QuantityDelta = line.Quantity,
                        QuantityAfter = product.QuantityOnHand,
                        Reason = "Void of " + sale.ReceiptNumber + ": " + reason.Trim(),
                        UserId = userId,
                        SaleId = sale.SaleId,
                        UnitCostAtTime = product.CostPrice,
                        CreatedAt = DateTime.Now
                    });
                }

                db.SaveChanges();
                tx.Commit();
            }
        }

        /// <summary>
        /// A cashier always rings up against an open shift. If none exists one is opened, so the
        /// till is usable without a separate "start shift" step.
        /// </summary>
        private static Shift GetOrOpenShift(SmartShelfContext db, int cashierUserId)
        {
            var shift = db.Shifts.FirstOrDefault(s => s.CashierUserId == cashierUserId
                                                      && s.Status == ShiftStatus.Open);
            if (shift != null) return shift;

            // The float is a store-wide standing amount rather than something the till asks for,
            // so it comes from configuration. It feeds the expected-cash figure at cash-up, which
            // is why it must not be a literal buried in this method.
            var openingFloat = 500.00m;
            decimal configured;
            if (decimal.TryParse(System.Configuration.ConfigurationManager.AppSettings["DefaultOpeningFloat"],
                                 System.Globalization.NumberStyles.Number,
                                 System.Globalization.CultureInfo.InvariantCulture, out configured)
                && configured >= 0m)
            {
                openingFloat = configured;
            }

            shift = new Shift
            {
                CashierUserId = cashierUserId,
                TillNumber = "Till 1",
                OpenedAt = DateTime.Now,
                OpeningFloat = openingFloat,
                Status = ShiftStatus.Open
            };
            db.Shifts.Add(shift);
            db.SaveChanges();
            return shift;
        }

        /// <summary>
        /// Receipt numbers run in a single sequence. The unique index on ReceiptNumber is the
        /// real guarantee; this just picks the next free value inside the sale's transaction.
        /// </summary>
        private static string NextReceiptNumber(SmartShelfContext db)
        {
            var last = db.Sales
                .OrderByDescending(s => s.SaleId)
                .Select(s => s.ReceiptNumber)
                .FirstOrDefault();

            var next = 1;
            if (!string.IsNullOrEmpty(last) && last.StartsWith("RCT-"))
            {
                int parsed;
                if (int.TryParse(last.Substring(4), out parsed)) next = parsed + 1;
            }

            return "RCT-" + next.ToString("0000000");
        }

        // --------------------------------------------------------------
        // Read helpers
        // --------------------------------------------------------------

        public static Sale GetSaleWithLines(int saleId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Sales
                    .Include(s => s.Lines)
                    .Include(s => s.CashierUser)
                    .Include(s => s.Shift)
                    .AsNoTracking()
                    .FirstOrDefault(s => s.SaleId == saleId);
            }
        }

        public static List<Sale> GetRecentSales(int cashierUserId, int take = 40)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Sales
                    .Include(s => s.Lines)
                    .Where(s => s.CashierUserId == cashierUserId)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(take)
                    .AsNoTracking()
                    .ToList();
            }
        }
    }
}
