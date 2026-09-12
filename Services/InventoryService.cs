using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>
    /// Every change to a product's on-hand figure goes through here. Two rules are enforced in
    /// one place rather than scattered across pages: stock may never go negative, and every
    /// change is paired with a StockMovement naming the staff member responsible.
    /// </summary>
    public static class InventoryService
    {
        /// <summary>
        /// Manual correction from the Inventory screen. <paramref name="newQuantity"/> is the
        /// counted shelf figure, not a delta - that is what a stock take actually produces.
        /// </summary>
        public static void AdjustStock(int productId, int newQuantity, string reason, int userId)
        {
            if (newQuantity < 0)
                throw new BusinessRuleException("Stock cannot be set to a negative quantity.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("A reason is required so the adjustment can be traced later.");
            if (reason.Trim().Length > 250)
                throw new BusinessRuleException("The reason must be 250 characters or fewer.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product == null) throw new BusinessRuleException("That product no longer exists.");

                var delta = newQuantity - product.QuantityOnHand;
                if (delta == 0)
                    throw new BusinessRuleException("The counted quantity matches the system already - nothing to adjust.");

                product.QuantityOnHand = newQuantity;

                db.StockMovements.Add(new StockMovement
                {
                    ProductId = productId,
                    MovementType = MovementType.Adjustment,
                    QuantityDelta = delta,
                    QuantityAfter = newQuantity,
                    Reason = reason.Trim(),
                    UserId = userId,
                    UnitCostAtTime = product.CostPrice,
                    CreatedAt = DateTime.Now
                });

                // Batches describe what is on the shelf, so a correction has to land on them too,
                // otherwise the expiry report would keep counting stock that is no longer there.
                ReconcileBatches(db, product, newQuantity);

                db.SaveChanges();
                tx.Commit();
            }
        }

        /// <summary>Writes stock off as waste - expired, damaged or spoiled.</summary>
        public static void WriteOffWaste(int productId, int quantity, string reason, int userId, int? stockBatchId)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("The quantity written off must be at least 1.");
            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("A reason is required for every write-off.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product == null) throw new BusinessRuleException("That product no longer exists.");

                if (product.QuantityOnHand < quantity)
                    throw new InsufficientStockException(product.Name, product.QuantityOnHand, quantity);

                product.QuantityOnHand -= quantity;

                if (stockBatchId.HasValue)
                {
                    var batch = db.StockBatches.FirstOrDefault(b => b.StockBatchId == stockBatchId.Value
                                                                   && b.ProductId == productId);
                    if (batch == null)
                        throw new BusinessRuleException("That batch does not belong to this product.");
                    if (batch.QuantityRemaining < quantity)
                        throw new BusinessRuleException(
                            string.Format("That batch only has {0} left; you cannot write off {1}.",
                                          batch.QuantityRemaining, quantity));
                    batch.QuantityRemaining -= quantity;
                }
                else
                {
                    ConsumeBatches(db, productId, quantity);
                }

                db.StockMovements.Add(new StockMovement
                {
                    ProductId = productId,
                    MovementType = MovementType.Waste,
                    QuantityDelta = -quantity,
                    QuantityAfter = product.QuantityOnHand,
                    Reason = reason.Trim(),
                    UserId = userId,
                    UnitCostAtTime = product.CostPrice,
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
                tx.Commit();
            }
        }

        /// <summary>
        /// Books in goods against a purchase order line: raises stock, opens a batch carrying the
        /// expiry date, and advances the order's status. Called by the manager's receiving screen.
        /// </summary>
        public static void ReceiveStock(int purchaseOrderLineId, int quantity, DateTime? expiryDate,
                                        string batchCode, int userId)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("The quantity received must be at least 1.");
            if (expiryDate.HasValue && expiryDate.Value.Date < DateTime.Today)
                throw new BusinessRuleException("The expiry date is already in the past - check the delivery note.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var line = db.PurchaseOrderLines
                    .Include(l => l.PurchaseOrder)
                    .Include(l => l.Product)
                    .FirstOrDefault(l => l.PurchaseOrderLineId == purchaseOrderLineId);

                if (line == null) throw new BusinessRuleException("That order line no longer exists.");

                var order = line.PurchaseOrder;
                if (order.Status == PurchaseOrderStatus.Cancelled)
                    throw new BusinessRuleException("This order was cancelled and cannot be received against.");
                if (order.Status == PurchaseOrderStatus.Draft)
                    throw new BusinessRuleException("Send the order to the supplier before receiving stock against it.");

                var outstanding = line.QuantityOrdered - line.QuantityReceived;
                if (quantity > outstanding)
                    throw new BusinessRuleException(string.Format(
                        "Only {0} of {1} are still outstanding on this line.", outstanding, line.Product.Name));

                line.QuantityReceived += quantity;
                line.Product.QuantityOnHand += quantity;

                db.StockBatches.Add(new StockBatch
                {
                    ProductId = line.ProductId,
                    BatchCode = string.IsNullOrWhiteSpace(batchCode)
                        ? order.OrderNumber + "-" + line.PurchaseOrderLineId
                        : batchCode.Trim(),
                    QuantityReceived = quantity,
                    QuantityRemaining = quantity,
                    ReceivedDate = DateTime.Now,
                    ExpiryDate = expiryDate,
                    UnitCost = line.UnitCost,
                    PurchaseOrderId = order.PurchaseOrderId
                });

                db.StockMovements.Add(new StockMovement
                {
                    ProductId = line.ProductId,
                    MovementType = MovementType.Receipt,
                    QuantityDelta = quantity,
                    QuantityAfter = line.Product.QuantityOnHand,
                    Reason = "Goods received against " + order.OrderNumber,
                    UserId = userId,
                    PurchaseOrderId = order.PurchaseOrderId,
                    UnitCostAtTime = line.UnitCost,
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();

                // Status follows the goods: fully received closes the order, anything less leaves
                // it open so the outstanding balance stays visible to both sides.
                var lines = db.PurchaseOrderLines.Where(l => l.PurchaseOrderId == order.PurchaseOrderId).ToList();
                var allComplete = lines.All(l => l.QuantityReceived >= l.QuantityOrdered);
                var anyReceived = lines.Any(l => l.QuantityReceived > 0);

                if (allComplete)
                {
                    order.Status = PurchaseOrderStatus.Delivered;
                    order.DeliveredAt = DateTime.Now;
                }
                else if (anyReceived)
                {
                    order.Status = PurchaseOrderStatus.PartiallyDelivered;
                }

                db.SaveChanges();
                tx.Commit();
            }
        }

        /// <summary>
        /// Takes <paramref name="quantity"/> units off the batches that expire soonest (FEFO).
        /// Shared by sales and untargeted write-offs. The caller owns the transaction and the
        /// SaveChanges call.
        /// </summary>
        internal static void ConsumeBatches(SmartShelfContext db, int productId, int quantity)
        {
            var remaining = quantity;

            // Nulls last: dated stock must move before undated stock.
            var batches = db.StockBatches
                .Where(b => b.ProductId == productId && b.QuantityRemaining > 0)
                .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                .ThenBy(b => b.ExpiryDate)
                .ThenBy(b => b.ReceivedDate)
                .ToList();

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var take = Math.Min(batch.QuantityRemaining, remaining);
                batch.QuantityRemaining -= take;
                remaining -= take;
            }

            // Any shortfall means batch records lag the on-hand figure (older data, or stock
            // booked in before batches were tracked). The product total is authoritative, so this
            // is not an error - there is simply nothing left to deduct here.
        }

        /// <summary>Re-points batch quantities at a corrected on-hand total after a stock take.</summary>
        private static void ReconcileBatches(SmartShelfContext db, Product product, int newQuantity)
        {
            var batches = db.StockBatches
                .Where(b => b.ProductId == product.ProductId)
                .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                .ThenBy(b => b.ExpiryDate)
                .ToList();

            var allocated = batches.Sum(b => (int?)b.QuantityRemaining) ?? 0;

            if (newQuantity < allocated)
            {
                // Fewer on the shelf than recorded: take the difference off the oldest stock first.
                var toRemove = allocated - newQuantity;
                foreach (var batch in batches)
                {
                    if (toRemove <= 0) break;
                    var take = Math.Min(batch.QuantityRemaining, toRemove);
                    batch.QuantityRemaining -= take;
                    toRemove -= take;
                }
            }
            else if (newQuantity > allocated)
            {
                // More on the shelf than recorded: the surplus has no known expiry date.
                var surplus = newQuantity - allocated;
                db.StockBatches.Add(new StockBatch
                {
                    ProductId = product.ProductId,
                    BatchCode = "ADJ-" + DateTime.Now.ToString("yyyyMMddHHmm"),
                    QuantityReceived = surplus,
                    QuantityRemaining = surplus,
                    ReceivedDate = DateTime.Now,
                    ExpiryDate = null,
                    UnitCost = product.CostPrice
                });
            }
        }

        // --------------------------------------------------------------
        // Read helpers used by the inventory and dashboard screens
        // --------------------------------------------------------------

        public static List<Product> GetLowStockProducts(int max = 100)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .Where(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel)
                    .OrderBy(p => p.QuantityOnHand)
                    .Take(max)
                    .ToList();
            }
        }

        public static int CountLowStock()
        {
            using (var db = new SmartShelfContext())
            {
                return db.Products.Count(p => p.IsActive && p.QuantityOnHand <= p.ReorderLevel);
            }
        }

        /// <summary>Open batches for one product, soonest expiry first - what the write-off form offers.</summary>
        public static List<StockBatch> GetOpenBatches(int productId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.StockBatches
                    .AsNoTracking()
                    .Where(b => b.ProductId == productId && b.QuantityRemaining > 0)
                    .OrderBy(b => b.ExpiryDate.HasValue ? 0 : 1)
                    .ThenBy(b => b.ExpiryDate)
                    .ToList();
            }
        }

        /// <summary>
        /// Soonest expiry date per product, fetched in one query so the inventory grid doesn't
        /// issue a lookup per row.
        /// </summary>
        public static Dictionary<int, DateTime> GetSoonestExpiryByProduct()
        {
            using (var db = new SmartShelfContext())
            {
                return db.StockBatches
                    .Where(b => b.QuantityRemaining > 0 && b.ExpiryDate != null)
                    .GroupBy(b => b.ProductId)
                    .Select(g => new { ProductId = g.Key, Soonest = g.Min(x => x.ExpiryDate.Value) })
                    .ToDictionary(x => x.ProductId, x => x.Soonest);
            }
        }

        /// <summary>Recent stock movements, optionally narrowed to one product or movement type.</summary>
        public static List<StockMovement> GetMovements(int? productId, MovementType? type,
                                                       DateTime? from, DateTime? to, int take = 300)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.StockMovements
                    .Include(m => m.Product)
                    .Include(m => m.User)
                    .AsNoTracking()
                    .AsQueryable();

                if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
                if (type.HasValue) query = query.Where(m => m.MovementType == type.Value);
                if (from.HasValue)
                {
                    var start = from.Value.Date;
                    query = query.Where(m => m.CreatedAt >= start);
                }
                if (to.HasValue)
                {
                    var end = to.Value.Date.AddDays(1);
                    query = query.Where(m => m.CreatedAt < end);
                }

                return query.OrderByDescending(m => m.CreatedAt).Take(take).ToList();
            }
        }
    }
}
