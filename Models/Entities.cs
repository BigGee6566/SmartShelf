using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartShelf.Models
{
    // ------------------------------------------------------------------
    // Identity and permissions
    // ------------------------------------------------------------------

    public class Role
    {
        public int RoleId { get; set; }

        [Required, StringLength(30)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }

    public class User
    {
        public int UserId { get; set; }

        [Required, StringLength(50)]
        public string Username { get; set; }

        /// <summary>Base64 PBKDF2 hash. The plain password is never stored anywhere.</summary>
        [Required, StringLength(200)]
        public string PasswordHash { get; set; }

        [Required, StringLength(100)]
        public string PasswordSalt { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [StringLength(150)]
        public string Email { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; }

        /// <summary>Set only for Supplier logins: which supplier company this person represents.</summary>
        public int? SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    // ------------------------------------------------------------------
    // Focus A - catalogue and pricing
    // ------------------------------------------------------------------

    public class Category
    {
        public int CategoryId { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }

    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string ContactPerson { get; set; }

        [StringLength(150)]
        public string Email { get; set; }

        [StringLength(30)]
        public string Phone { get; set; }

        /// <summary>Days the supplier normally takes to deliver; drives the expected date on new orders.</summary>
        public int LeadTimeDays { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }

        [Required, StringLength(30)]
        public string Barcode { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        /// <summary>Preferred supplier - used to pre-fill purchase orders from low-stock alerts.</summary>
        public int? SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        [Column(TypeName = "decimal")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal")]
        public decimal SellingPrice { get; set; }

        /// <summary>Stored per product because zero-rated goods (e.g. brown bread) differ from the 15% norm.</summary>
        [Column(TypeName = "decimal")]
        public decimal VatRate { get; set; }

        public int ReorderLevel { get; set; }

        /// <summary>
        /// Authoritative on-hand figure. Only the service layer changes it, and always inside the
        /// same transaction as the StockMovement row that explains the change.
        /// </summary>
        public int QuantityOnHand { get; set; }

        [StringLength(400)]
        public string ImageUrl { get; set; }

        /// <summary>Optional uploaded image. When present it takes precedence over ImageUrl.</summary>
        public byte[] ImageData { get; set; }

        [StringLength(60)]
        public string ImageContentType { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<StockBatch> StockBatches { get; set; }
        public virtual ICollection<StockMovement> StockMovements { get; set; }

        [NotMapped]
        public bool IsLowStock { get { return QuantityOnHand <= ReorderLevel; } }
    }

    // ------------------------------------------------------------------
    // Focus B - stock control
    // ------------------------------------------------------------------

    /// <summary>
    /// A delivery of one product with a single expiry date. Sales consume batches
    /// first-expiring-first-out so the expiry/waste report reflects what is actually on the shelf.
    /// </summary>
    public class StockBatch
    {
        public int StockBatchId { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [StringLength(40)]
        public string BatchCode { get; set; }

        public int QuantityReceived { get; set; }
        public int QuantityRemaining { get; set; }

        public DateTime ReceivedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitCost { get; set; }

        public int? PurchaseOrderId { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }
    }

    /// <summary>
    /// The audit trail for stock. Every sale line, adjustment, receipt and write-off appends one
    /// row stamped with the staff member responsible.
    /// </summary>
    public class StockMovement
    {
        public int StockMovementId { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public MovementType MovementType { get; set; }

        /// <summary>Negative for sales and waste, positive for receipts. Adjustments can be either.</summary>
        public int QuantityDelta { get; set; }

        public int QuantityAfter { get; set; }

        [StringLength(250)]
        public string Reason { get; set; }

        /// <summary>Who did it - required for every movement, per the traceability rule.</summary>
        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int? SaleId { get; set; }
        public int? PurchaseOrderId { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitCostAtTime { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    // ------------------------------------------------------------------
    // Focus C - supplier order processing
    // ------------------------------------------------------------------

    public class PurchaseOrder
    {
        public int PurchaseOrderId { get; set; }

        [Required, StringLength(20)]
        public string OrderNumber { get; set; }

        public int SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public int CreatedByUserId { get; set; }
        public virtual User CreatedByUser { get; set; }

        public PurchaseOrderStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime ExpectedDate { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        [StringLength(300)]
        public string Notes { get; set; }

        [StringLength(300)]
        public string SupplierNote { get; set; }

        public virtual ICollection<PurchaseOrderLine> Lines { get; set; }

        [NotMapped]
        public bool IsOpen
        {
            get
            {
                return Status != PurchaseOrderStatus.Delivered
                       && Status != PurchaseOrderStatus.Cancelled;
            }
        }
    }

    public class PurchaseOrderLine
    {
        public int PurchaseOrderLineId { get; set; }

        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int QuantityOrdered { get; set; }
        public int QuantityReceived { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitCost { get; set; }

        [NotMapped]
        public decimal LineTotal { get { return QuantityOrdered * UnitCost; } }
    }

    // ------------------------------------------------------------------
    // Focus D - POS sales, and Focus E - cash-up
    // ------------------------------------------------------------------

    public class Shift
    {
        public int ShiftId { get; set; }

        public int CashierUserId { get; set; }
        public virtual User CashierUser { get; set; }

        [Required, StringLength(20)]
        public string TillNumber { get; set; }

        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        /// <summary>Float placed in the till at the start of the shift; part of the expected cash total.</summary>
        [Column(TypeName = "decimal")]
        public decimal OpeningFloat { get; set; }

        public ShiftStatus Status { get; set; }

        public virtual ICollection<Sale> Sales { get; set; }
    }

    public class Sale
    {
        public int SaleId { get; set; }

        [Required, StringLength(20)]
        public string ReceiptNumber { get; set; }

        public int CashierUserId { get; set; }
        public virtual User CashierUser { get; set; }

        public int ShiftId { get; set; }
        public virtual Shift Shift { get; set; }

        public DateTime SaleDate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Total { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [Column(TypeName = "decimal")]
        public decimal AmountTendered { get; set; }

        /// <summary>Always computed by SaleService; a client-submitted value is never persisted.</summary>
        [Column(TypeName = "decimal")]
        public decimal ChangeDue { get; set; }

        public SaleStatus Status { get; set; }

        public virtual ICollection<SaleLine> Lines { get; set; }
    }

    public class SaleLine
    {
        public int SaleLineId { get; set; }

        public int SaleId { get; set; }
        public virtual Sale Sale { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        /// <summary>Snapshot of name and price at the time of sale, so later edits don't rewrite history.</summary>
        [Required, StringLength(120)]
        public string ProductName { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal")]
        public decimal LineTotal { get; set; }
    }

    public class CashUp
    {
        public int CashUpId { get; set; }

        public int ShiftId { get; set; }
        public virtual Shift Shift { get; set; }

        /// <summary>The cashier who performed the count - the variance is traceable to them.</summary>
        public int UserId { get; set; }
        public virtual User User { get; set; }

        /// <summary>Opening float + all completed cash sales in the shift, recomputed server-side.</summary>
        [Column(TypeName = "decimal")]
        public decimal ExpectedCash { get; set; }

        [Column(TypeName = "decimal")]
        public decimal CountedCash { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Variance { get; set; }

        [StringLength(300)]
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? ReviewedByUserId { get; set; }
        public virtual User ReviewedByUser { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(300)]
        public string ReviewNote { get; set; }

        [NotMapped]
        public bool NeedsReview { get { return Variance != 0m && ReviewedAt == null; } }
    }
}
