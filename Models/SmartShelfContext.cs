using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace SmartShelf.Models
{
    /// <summary>
    /// EF6 Code-First context. The physical schema is created by App_Data\SmartShelf_Schema.sql,
    /// so this class must stay in step with that script - no initializer or migration runs at
    /// startup (see Global.Application_Start).
    /// </summary>
    public class SmartShelfContext : DbContext
    {
        public SmartShelfContext() : base("name=SmartShelfDb") { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<StockBatch> StockBatches { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleLine> SaleLines { get; set; }
        public DbSet<CashUp> CashUps { get; set; }
        public DbSet<SecurityEvent> SecurityEvents { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            // Table names in the script are singular; EF's pluralising convention would look for
            // "Products", "Users" and so on, so it is switched off.
            mb.Conventions.Remove<PluralizingTableNameConvention>();

            // Money is decimal(18,2) everywhere; VAT rates need more places.
            mb.Properties<decimal>().Configure(c => c.HasPrecision(18, 2));

            mb.Entity<Product>().Property(p => p.VatRate).HasPrecision(5, 4);
            mb.Entity<SaleLine>().Property(l => l.VatRate).HasPrecision(5, 4);

            // Uniqueness (username, barcode, receipt and order number) is enforced by unique
            // indexes in the schema script; the service layer also checks before insert so the
            // user gets a readable message rather than a constraint violation.

            // Every relationship below states its delete behaviour explicitly. Left to convention,
            // EF turns cascade delete ON for a required one-to-many, which would not match the
            // schema's foreign keys - removing a Role would silently take its users with it.
            mb.Entity<User>()
                .HasRequired(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .WillCascadeOnDelete(false);

            mb.Entity<PurchaseOrder>()
                .HasRequired(o => o.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(o => o.SupplierId)
                .WillCascadeOnDelete(false);

            // A supplier login points at its company, but deleting the company must not cascade
            // into the user table - the account is deactivated instead.
            mb.Entity<User>()
                .HasOptional(u => u.Supplier)
                .WithMany()
                .HasForeignKey(u => u.SupplierId)
                .WillCascadeOnDelete(false);

            mb.Entity<Product>()
                .HasOptional(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .WillCascadeOnDelete(false);

            mb.Entity<Product>()
                .HasRequired(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .WillCascadeOnDelete(false);

            mb.Entity<StockMovement>()
                .HasRequired(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .WillCascadeOnDelete(false);

            mb.Entity<StockMovement>()
                .HasRequired(m => m.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(m => m.ProductId)
                .WillCascadeOnDelete(false);

            mb.Entity<StockBatch>()
                .HasRequired(b => b.Product)
                .WithMany(p => p.StockBatches)
                .HasForeignKey(b => b.ProductId)
                .WillCascadeOnDelete(false);

            mb.Entity<StockBatch>()
                .HasOptional(b => b.PurchaseOrder)
                .WithMany()
                .HasForeignKey(b => b.PurchaseOrderId)
                .WillCascadeOnDelete(false);

            mb.Entity<PurchaseOrder>()
                .HasRequired(o => o.CreatedByUser)
                .WithMany()
                .HasForeignKey(o => o.CreatedByUserId)
                .WillCascadeOnDelete(false);

            mb.Entity<PurchaseOrderLine>()
                .HasRequired(l => l.PurchaseOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.PurchaseOrderId)
                .WillCascadeOnDelete(true);

            mb.Entity<PurchaseOrderLine>()
                .HasRequired(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .WillCascadeOnDelete(false);

            mb.Entity<Shift>()
                .HasRequired(s => s.CashierUser)
                .WithMany()
                .HasForeignKey(s => s.CashierUserId)
                .WillCascadeOnDelete(false);

            mb.Entity<Sale>()
                .HasRequired(s => s.CashierUser)
                .WithMany()
                .HasForeignKey(s => s.CashierUserId)
                .WillCascadeOnDelete(false);

            mb.Entity<Sale>()
                .HasRequired(s => s.Shift)
                .WithMany(sh => sh.Sales)
                .HasForeignKey(s => s.ShiftId)
                .WillCascadeOnDelete(false);

            mb.Entity<SaleLine>()
                .HasRequired(l => l.Sale)
                .WithMany(s => s.Lines)
                .HasForeignKey(l => l.SaleId)
                .WillCascadeOnDelete(true);

            mb.Entity<SaleLine>()
                .HasRequired(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .WillCascadeOnDelete(false);

            mb.Entity<CashUp>()
                .HasRequired(c => c.Shift)
                .WithMany()
                .HasForeignKey(c => c.ShiftId)
                .WillCascadeOnDelete(false);

            mb.Entity<CashUp>()
                .HasRequired(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .WillCascadeOnDelete(false);

            mb.Entity<CashUp>()
                .HasOptional(c => c.ReviewedByUser)
                .WithMany()
                .HasForeignKey(c => c.ReviewedByUserId)
                .WillCascadeOnDelete(false);

            // Both sides are optional: UserId may be unresolved (a failed sign-in against a
            // nonexistent username) and ActorUserId is only set for admin-driven events.
            mb.Entity<SecurityEvent>()
                .HasOptional(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .WillCascadeOnDelete(false);

            mb.Entity<SecurityEvent>()
                .HasOptional(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(mb);
        }
    }
}
