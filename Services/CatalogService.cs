using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>
    /// Focus A: the product catalogue, its categories and its suppliers. Validation lives here so
    /// the same rules apply whether a product is created from the Products screen or anywhere else.
    /// </summary>
    public static class CatalogService
    {
        // ----------------------------------------------------------
        // Products
        // ----------------------------------------------------------

        public static List<Product> SearchProducts(string term, int? categoryId, bool activeOnly, int take = 300)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .AsNoTracking()
                    .AsQueryable();

                if (activeOnly) query = query.Where(p => p.IsActive);
                if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var t = term.Trim();
                    // A cashier scans a barcode or types part of a name - both hit the same query.
                    query = query.Where(p => p.Name.Contains(t) || p.Barcode.Contains(t));
                }

                return query.OrderBy(p => p.Name).Take(take).ToList();
            }
        }

        /// <summary>Exact barcode match, used by the POS scanner field.</summary>
        public static Product FindByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            var code = barcode.Trim();

            using (var db = new SmartShelfContext())
            {
                return db.Products.AsNoTracking()
                    .FirstOrDefault(p => p.Barcode == code && p.IsActive);
            }
        }

        public static Product GetProduct(int productId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .AsNoTracking()
                    .FirstOrDefault(p => p.ProductId == productId);
            }
        }

        /// <summary>
        /// Creates or updates a product. Stock is deliberately not settable here - it only moves
        /// through InventoryService, so every change keeps its audit trail.
        /// </summary>
        public static int SaveProduct(Product input, byte[] imageData, string imageContentType)
        {
            ValidateProduct(input);

            using (var db = new SmartShelfContext())
            {
                var barcode = input.Barcode.Trim();

                var clash = db.Products.Any(p => p.Barcode == barcode && p.ProductId != input.ProductId);
                if (clash)
                    throw new BusinessRuleException("Another product already uses barcode " + barcode + ".");

                if (!db.Categories.Any(c => c.CategoryId == input.CategoryId))
                    throw new BusinessRuleException("Choose a category for this product.");

                if (input.SupplierId.HasValue && !db.Suppliers.Any(s => s.SupplierId == input.SupplierId.Value))
                    throw new BusinessRuleException("That supplier no longer exists.");

                Product product;
                if (input.ProductId == 0)
                {
                    product = new Product { CreatedAt = DateTime.Now, QuantityOnHand = 0 };
                    db.Products.Add(product);
                }
                else
                {
                    product = db.Products.FirstOrDefault(p => p.ProductId == input.ProductId);
                    if (product == null) throw new BusinessRuleException("That product no longer exists.");
                }

                product.Barcode = barcode;
                product.Name = input.Name.Trim();
                product.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
                product.CategoryId = input.CategoryId;
                product.SupplierId = input.SupplierId;
                product.CostPrice = input.CostPrice;
                product.SellingPrice = input.SellingPrice;
                product.VatRate = input.VatRate;
                product.ReorderLevel = input.ReorderLevel;
                product.ImageUrl = string.IsNullOrWhiteSpace(input.ImageUrl) ? null : input.ImageUrl.Trim();
                product.IsActive = input.IsActive;

                // Only replace the stored image when a new file actually came up with the post.
                if (imageData != null && imageData.Length > 0)
                {
                    product.ImageData = imageData;
                    product.ImageContentType = imageContentType;
                }

                db.SaveChanges();
                return product.ProductId;
            }
        }

        public static void ClearProductImage(int productId)
        {
            using (var db = new SmartShelfContext())
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product == null) throw new BusinessRuleException("That product no longer exists.");

                product.ImageData = null;
                product.ImageContentType = null;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Products are deactivated rather than deleted: sale lines, movements and order lines
        /// all point at them, and that history has to stay readable.
        /// </summary>
        public static void SetProductActive(int productId, bool isActive)
        {
            using (var db = new SmartShelfContext())
            {
                var product = db.Products.FirstOrDefault(p => p.ProductId == productId);
                if (product == null) throw new BusinessRuleException("That product no longer exists.");

                product.IsActive = isActive;
                db.SaveChanges();
            }
        }

        private static void ValidateProduct(Product p)
        {
            if (p == null) throw new BusinessRuleException("No product details were submitted.");

            if (string.IsNullOrWhiteSpace(p.Barcode))
                throw new BusinessRuleException("A barcode is required.");
            if (p.Barcode.Trim().Length > 30)
                throw new BusinessRuleException("The barcode must be 30 characters or fewer.");

            if (string.IsNullOrWhiteSpace(p.Name))
                throw new BusinessRuleException("A product name is required.");
            if (p.Name.Trim().Length > 120)
                throw new BusinessRuleException("The product name must be 120 characters or fewer.");

            if (p.CostPrice < 0)
                throw new BusinessRuleException("The cost price cannot be negative.");
            if (p.SellingPrice <= 0)
                throw new BusinessRuleException("The selling price must be greater than zero.");
            if (p.SellingPrice > 1000000m || p.CostPrice > 1000000m)
                throw new BusinessRuleException("That price looks wrong - please check it.");

            if (p.VatRate < 0m || p.VatRate > 1m)
                throw new BusinessRuleException("The VAT rate must be between 0 and 1 (0.15 means 15%).");

            if (p.ReorderLevel < 0)
                throw new BusinessRuleException("The reorder level cannot be negative.");
            if (p.ReorderLevel > 100000)
                throw new BusinessRuleException("That reorder level is unrealistically high.");

            if (!string.IsNullOrWhiteSpace(p.ImageUrl))
            {
                if (p.ImageUrl.Trim().Length > 400)
                    throw new BusinessRuleException("The image URL must be 400 characters or fewer.");

                Uri parsed;
                if (!Uri.TryCreate(p.ImageUrl.Trim(), UriKind.Absolute, out parsed)
                    || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
                {
                    throw new BusinessRuleException("The image URL must be a full http:// or https:// address.");
                }
            }
        }

        // ----------------------------------------------------------
        // Categories
        // ----------------------------------------------------------

        public static List<Category> GetCategories(bool activeOnly = false)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.Categories.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(c => c.IsActive);
                return query.OrderBy(c => c.Name).ToList();
            }
        }

        public static void SaveCategory(int categoryId, string name, string description, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessRuleException("A category name is required.");
            if (name.Trim().Length > 60)
                throw new BusinessRuleException("The category name must be 60 characters or fewer.");

            var trimmed = name.Trim();

            using (var db = new SmartShelfContext())
            {
                if (db.Categories.Any(c => c.Name == trimmed && c.CategoryId != categoryId))
                    throw new BusinessRuleException("A category called \"" + trimmed + "\" already exists.");

                Category category;
                if (categoryId == 0)
                {
                    category = new Category();
                    db.Categories.Add(category);
                }
                else
                {
                    category = db.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                    if (category == null) throw new BusinessRuleException("That category no longer exists.");
                }

                category.Name = trimmed;
                category.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
                category.IsActive = isActive;

                db.SaveChanges();
            }
        }

        public static void DeleteCategory(int categoryId)
        {
            using (var db = new SmartShelfContext())
            {
                var category = db.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                if (category == null) throw new BusinessRuleException("That category no longer exists.");

                var inUse = db.Products.Count(p => p.CategoryId == categoryId);
                if (inUse > 0)
                    throw new BusinessRuleException(string.Format(
                        "{0} product(s) still use this category. Move them first, or deactivate the category instead.",
                        inUse));

                db.Categories.Remove(category);
                db.SaveChanges();
            }
        }

        // ----------------------------------------------------------
        // Suppliers
        // ----------------------------------------------------------

        public static List<Supplier> GetSuppliers(bool activeOnly = false)
        {
            using (var db = new SmartShelfContext())
            {
                var query = db.Suppliers.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(s => s.IsActive);
                return query.OrderBy(s => s.Name).ToList();
            }
        }

        public static Supplier GetSupplier(int supplierId)
        {
            using (var db = new SmartShelfContext())
            {
                return db.Suppliers.AsNoTracking().FirstOrDefault(s => s.SupplierId == supplierId);
            }
        }

        public static void SaveSupplier(Supplier input)
        {
            if (input == null) throw new BusinessRuleException("No supplier details were submitted.");
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new BusinessRuleException("A supplier name is required.");
            if (input.Name.Trim().Length > 100)
                throw new BusinessRuleException("The supplier name must be 100 characters or fewer.");
            if (input.LeadTimeDays < 0 || input.LeadTimeDays > 365)
                throw new BusinessRuleException("The lead time must be between 0 and 365 days.");
            if (!string.IsNullOrWhiteSpace(input.Email) && !input.Email.Contains("@"))
                throw new BusinessRuleException("That does not look like a valid email address.");

            var name = input.Name.Trim();

            using (var db = new SmartShelfContext())
            {
                if (db.Suppliers.Any(s => s.Name == name && s.SupplierId != input.SupplierId))
                    throw new BusinessRuleException("A supplier called \"" + name + "\" already exists.");

                Supplier supplier;
                if (input.SupplierId == 0)
                {
                    supplier = new Supplier();
                    db.Suppliers.Add(supplier);
                }
                else
                {
                    supplier = db.Suppliers.FirstOrDefault(s => s.SupplierId == input.SupplierId);
                    if (supplier == null) throw new BusinessRuleException("That supplier no longer exists.");
                }

                supplier.Name = name;
                supplier.ContactPerson = Trim(input.ContactPerson);
                supplier.Email = Trim(input.Email);
                supplier.Phone = Trim(input.Phone);
                supplier.LeadTimeDays = input.LeadTimeDays;
                supplier.IsActive = input.IsActive;

                db.SaveChanges();
            }
        }

        private static string Trim(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
