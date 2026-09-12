using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>One line a manager wants on a new order.</summary>
    public class OrderLineRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public static class PurchaseOrderService
    {
        /// <summary>Raises a draft order. Nothing is sent to the supplier until Send is called.</summary>
        public static PurchaseOrder CreateOrder(int supplierId, IEnumerable<OrderLineRequest> lines,
                                                string notes, int managerUserId)
        {
            var requested = (lines ?? Enumerable.Empty<OrderLineRequest>())
                .Where(l => l.Quantity > 0)
                .GroupBy(l => l.ProductId)
                .Select(g => new OrderLineRequest { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (requested.Count == 0)
                throw new BusinessRuleException("Add at least one product with a quantity before saving the order.");
            if (requested.Any(l => l.Quantity > 100000))
                throw new BusinessRuleException("That order quantity is unrealistically large - please check it.");

            using (var db = new SmartShelfContext())
            using (var tx = db.Database.BeginTransaction())
            {
                var supplier = db.Suppliers.FirstOrDefault(s => s.SupplierId == supplierId);
                if (supplier == null) throw new BusinessRuleException("Choose a supplier for this order.");
                if (!supplier.IsActive) throw new BusinessRuleException("That supplier is no longer active.");

                var ids = requested.Select(r => r.ProductId).ToList();
                var products = db.Products.Where(p => ids.Contains(p.ProductId)).ToDictionary(p => p.ProductId);

                if (products.Count != requested.Count)
                    throw new BusinessRuleException("One of the products on this order no longer exists.");

                var order = new PurchaseOrder
                {
                    OrderNumber = NextOrderNumber(db),
                    SupplierId = supplierId,
                    CreatedByUserId = managerUserId,
                    Status = PurchaseOrderStatus.Draft,
                    CreatedAt = DateTime.Now,
                    // The supplier's own lead time sets the date they will be measured against.
                    ExpectedDate = DateTime.Today.AddDays(Math.Max(1, supplier.LeadTimeDays)),
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    Lines = requested.Select(r => new PurchaseOrderLine
                    {
                        ProductId = r.ProductId,
                        QuantityOrdered = r.Quantity,
                        QuantityReceived = 0,
                        UnitCost = products[r.ProductId].CostPrice
                    }).ToList()
                };

                db.PurchaseOrders.Add(order);
                db.SaveChanges();
                tx.Commit();
                return order;
            }
        }

        /// <summary>Releases a draft to the supplier, making it visible in their portal.</summary>
        public static void SendOrder(int purchaseOrderId, int managerUserId)
        {
            using (var db = new SmartShelfContext())
            {
                var order = db.PurchaseOrders.Include(o => o.Lines)
                    .FirstOrDefault(o => o.PurchaseOrderId == purchaseOrderId);

                if (order == null) throw new BusinessRuleException("That order no longer exists.");
                if (order.Status != PurchaseOrderStatus.Draft)
                    throw new BusinessRuleException("Only a draft order can be sent.");
                if (order.Lines == null || order.Lines.Count == 0)
                    throw new BusinessRuleException("Add at least one line before sending this order.");

                order.Status = PurchaseOrderStatus.Sent;
                order.SentAt = DateTime.Now;
                db.SaveChanges();
            }
        }

        public static void CancelOrder(int purchaseOrderId, string reason, int managerUserId)
        {
            using (var db = new SmartShelfContext())
            {
                var order = db.PurchaseOrders.FirstOrDefault(o => o.PurchaseOrderId == purchaseOrderId);
                if (order == null) throw new BusinessRuleException("That order no longer exists.");
                if (order.Status == PurchaseOrderStatus.Delivered)
                    throw new BusinessRuleException("A delivered order cannot be cancelled.");
                if (order.Status == PurchaseOrderStatus.PartiallyDelivered)
                    throw new BusinessRuleException("Stock has already been received against this order.");
                if (order.Status == PurchaseOrderStatus.Dispatched)
                    throw new BusinessRuleException("The supplier has already dispatched this order - contact them before cancelling.");

                order.Status = PurchaseOrderStatus.Cancelled;
                order.Notes = string.IsNullOrWhiteSpace(reason)
                    ? order.Notes
                    : ("Cancelled: " + reason.Trim());
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Supplier confirms they will fulfil the order. <paramref name="supplierId"/> comes from
        /// the signed-in user's own record, so one supplier can never act on another's order.
        /// </summary>
        public static void ConfirmOrder(int purchaseOrderId, int supplierId, string supplierNote,
                                        DateTime? revisedExpectedDate)
        {
            RequireNoteFits(supplierNote);

            using (var db = new SmartShelfContext())
            {
                var order = RequireSupplierOrder(db, purchaseOrderId, supplierId);

                if (order.Status != PurchaseOrderStatus.Sent)
                    throw new BusinessRuleException("Only an order that has been sent to you can be confirmed.");

                order.Status = PurchaseOrderStatus.Confirmed;
                order.ConfirmedAt = DateTime.Now;
                order.SupplierNote = string.IsNullOrWhiteSpace(supplierNote) ? null : supplierNote.Trim();

                if (revisedExpectedDate.HasValue)
                {
                    if (revisedExpectedDate.Value.Date < DateTime.Today)
                        throw new BusinessRuleException("The revised delivery date cannot be in the past.");
                    order.ExpectedDate = revisedExpectedDate.Value.Date;
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Supplier marks an order as dispatched. Stock is not raised here - it is raised when the
        /// store physically receives it (InventoryService.ReceiveStock), so what the supplier says
        /// and what the store counted stay separate facts.
        /// <para>
        /// DeliveredAt is deliberately left alone: it records when the store took the goods in, and
        /// writing the supplier's claim into it would make the on-time figures in the supplier
        /// performance report measure the wrong event.
        /// </para>
        /// </summary>
        public static void UpdateDeliveryStatus(int purchaseOrderId, int supplierId, string note)
        {
            RequireNoteFits(note);

            using (var db = new SmartShelfContext())
            {
                var order = RequireSupplierOrder(db, purchaseOrderId, supplierId);

                if (order.Status != PurchaseOrderStatus.Confirmed
                    && order.Status != PurchaseOrderStatus.PartiallyDelivered
                    && order.Status != PurchaseOrderStatus.Dispatched)
                    throw new BusinessRuleException("Confirm the order before updating its delivery status.");

                // A part-delivered order stays part-delivered: the store has already counted some
                // of it in, and that fact outranks a fresh dispatch note.
                if (order.Status != PurchaseOrderStatus.PartiallyDelivered)
                    order.Status = PurchaseOrderStatus.Dispatched;

                order.SupplierNote = string.IsNullOrWhiteSpace(note)
                    ? "Dispatched by supplier."
                    : note.Trim();

                db.SaveChanges();
            }
        }

        /// <summary>
        /// SupplierNote is NVARCHAR(300), and the note box is a textarea - ASP.NET does not render
        /// maxlength on one, so an over-long paste would otherwise reach SQL and come back as a
        /// truncation error the supplier cannot act on.
        /// </summary>
        private static void RequireNoteFits(string note)
        {
            if (note != null && note.Trim().Length > 300)
                throw new BusinessRuleException("That note is too long - please keep it to 300 characters or fewer.");
        }

        private static PurchaseOrder RequireSupplierOrder(SmartShelfContext db, int purchaseOrderId, int supplierId)
        {
            var order = db.PurchaseOrders.FirstOrDefault(o => o.PurchaseOrderId == purchaseOrderId);
            if (order == null) throw new BusinessRuleException("That order no longer exists.");
            if (order.SupplierId != supplierId)
                throw new BusinessRuleException("That order belongs to a different supplier.");
            return order;
        }

        private static string NextOrderNumber(SmartShelfContext db)
        {
            var last = db.PurchaseOrders
                .OrderByDescending(o => o.PurchaseOrderId)
                .Select(o => o.OrderNumber)
                .FirstOrDefault();

            var next = 1001;
            if (!string.IsNullOrEmpty(last) && last.StartsWith("PO-"))
            {
                int parsed;
                if (int.TryParse(last.Substring(3), out parsed)) next = parsed + 1;
            }

            return "PO-" + next.ToString("00000");
        }

        // --------------------------------------------------------------
        // Read helpers
        // --------------------------------------------------------------

        public static List<PurchaseOrder> GetOrders(int? supplierId, PurchaseOrderStatus? status, int take = 200)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.PurchaseOrders
                    .Include(o => o.Supplier)
                    .Include(o => o.Lines)
                    .Include(o => o.CreatedByUser)
                    .AsNoTracking()
                    .AsQueryable();

                if (supplierId.HasValue) query = query.Where(o => o.SupplierId == supplierId.Value);
                if (status.HasValue) query = query.Where(o => o.Status == status.Value);

                return query.OrderByDescending(o => o.CreatedAt).Take(take).ToList();
            }
        }

        public static PurchaseOrder GetOrder(int purchaseOrderId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.PurchaseOrders
                    .Include(o => o.Supplier)
                    .Include(o => o.CreatedByUser)
                    .Include(o => o.Lines.Select(l => l.Product))
                    .AsNoTracking()
                    .FirstOrDefault(o => o.PurchaseOrderId == purchaseOrderId);
            }
        }

        public static int CountOpenOrders()
        {
            using (var db = new SmartShelfContext())
            {
                return db.PurchaseOrders.Count(o => o.Status != PurchaseOrderStatus.Delivered
                                                    && o.Status != PurchaseOrderStatus.Cancelled);
            }
        }

        /// <summary>Human label for a status, shared by the manager and supplier screens.</summary>
        public static string StatusLabel(PurchaseOrderStatus status)
        {
            switch (status)
            {
                case PurchaseOrderStatus.Draft: return "Draft";
                case PurchaseOrderStatus.Sent: return "Sent";
                case PurchaseOrderStatus.Confirmed: return "Confirmed";
                case PurchaseOrderStatus.Dispatched: return "Dispatched";
                case PurchaseOrderStatus.PartiallyDelivered: return "Part delivered";
                case PurchaseOrderStatus.Delivered: return "Delivered";
                case PurchaseOrderStatus.Cancelled: return "Cancelled";
                default: return status.ToString();
            }
        }

        public static string StatusPillCss(PurchaseOrderStatus status)
        {
            switch (status)
            {
                case PurchaseOrderStatus.Draft: return "pill-grey";
                case PurchaseOrderStatus.Sent: return "pill-amber";
                case PurchaseOrderStatus.Confirmed: return "pill-blue";
                case PurchaseOrderStatus.Dispatched: return "pill-blue";
                case PurchaseOrderStatus.PartiallyDelivered: return "pill-amber";
                case PurchaseOrderStatus.Delivered: return "pill-green";
                case PurchaseOrderStatus.Cancelled: return "pill-red";
                default: return "pill-grey";
            }
        }
    }
}
