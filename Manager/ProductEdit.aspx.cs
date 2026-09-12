using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Manager
{
    /// <summary>
    /// Create or edit one catalogue entry (Focus A). Quantities are deliberately absent - they
    /// only move through InventoryService, which records who changed them and why.
    /// </summary>
    public partial class ProductEdit : SecurePageBase
    {
        protected override string[] AllowedRoles { get { return new[] { RoleNames.Manager }; } }

        /// <summary>Content types we are willing to store and serve back.</summary>
        private static readonly string[] AllowedImageTypes =
            { "image/jpeg", "image/pjpeg", "image/png", "image/gif", "image/webp" };

        private int ProductId
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["id"], out id) ? id : 0;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = ProductId == 0 ? "New product" : "Edit product";
            master.PageSubtitle = "Catalogue and pricing";

            if (IsPostBack) return;

            BindDropDowns();

            if (ProductId == 0)
            {
                var defaultVat = ConfigurationManager.AppSettings["DefaultVatRate"] ?? "0.15";
                SelectValue(ddlVat, defaultVat);
                imgPreview.ImageUrl = ResolveUrl("~/Handlers/ProductImage.ashx?id=0");
                return;
            }

            var product = CatalogService.GetProduct(ProductId);
            if (product == null)
            {
                alert.ShowError("That product no longer exists.");
                btnSave.Enabled = false;
                return;
            }

            Bind(product);
        }

        private void BindDropDowns()
        {
            ddlCategory.Items.Clear();
            foreach (var category in CatalogService.GetCategories(true))
            {
                ddlCategory.Items.Add(new ListItem(category.Name, category.CategoryId.ToString()));
            }

            ddlSupplier.Items.Clear();
            ddlSupplier.Items.Add(new ListItem("(no preferred supplier)", ""));
            foreach (var supplier in CatalogService.GetSuppliers(true))
            {
                ddlSupplier.Items.Add(new ListItem(supplier.Name, supplier.SupplierId.ToString()));
            }
        }

        private void Bind(Product product)
        {
            txtBarcode.Text = product.Barcode;
            txtName.Text = product.Name;
            txtDescription.Text = product.Description;
            SelectValue(ddlCategory, product.CategoryId.ToString());
            SelectValue(ddlSupplier, product.SupplierId.HasValue ? product.SupplierId.Value.ToString() : "");
            txtCost.Text = product.CostPrice.ToString("0.00", CultureInfo.InvariantCulture);
            txtPrice.Text = product.SellingPrice.ToString("0.00", CultureInfo.InvariantCulture);
            SelectValue(ddlVat, product.VatRate == 0m ? "0" : "0.15");
            txtReorder.Text = product.ReorderLevel.ToString();
            txtImageUrl.Text = product.ImageUrl;
            chkActive.Checked = product.IsActive;

            var hasUpload = product.ImageData != null && product.ImageData.Length > 0;
            phHasUpload.Visible = hasUpload;

            imgPreview.ImageUrl = hasUpload || string.IsNullOrWhiteSpace(product.ImageUrl)
                ? ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId + "&v=" + DateTime.Now.Ticks)
                : product.ImageUrl;

            phStock.Visible = true;
            litOnHand.Text = product.QuantityOnHand + " units";
            lnkAdjust.NavigateUrl = ResolveUrl("~/Manager/Inventory.aspx?productId=" + product.ProductId);

            if (product.CostPrice > 0m)
            {
                var margin = (product.SellingPrice - product.CostPrice) / product.CostPrice * 100m;
                litMargin.Text = string.Format("Margin on cost: {0:N1}%", margin);
            }
        }

        private static void SelectValue(DropDownList list, string value)
        {
            var item = list.Items.FindByValue(value ?? string.Empty);
            if (item != null) list.SelectedValue = item.Value;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            Page.Validate("Product");
            if (!Page.IsValid) return;

            decimal cost, price, vat;
            int reorder, categoryId;

            // Re-parse everything server-side: the client validators are only a courtesy.
            if (!TryParseAmount(txtCost.Text, out cost))
            {
                alert.ShowError("Enter the cost price as a number, for example 24.00");
                return;
            }
            if (!TryParseAmount(txtPrice.Text, out price))
            {
                alert.ShowError("Enter the selling price as a number, for example 32.50");
                return;
            }
            // The VAT list values are literals in the markup, so they are always invariant.
            if (!decimal.TryParse(ddlVat.SelectedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out vat))
            {
                alert.ShowError("Choose a VAT rate.");
                return;
            }
            if (!int.TryParse(txtReorder.Text, out reorder))
            {
                alert.ShowError("Enter the reorder level as a whole number.");
                return;
            }
            if (!int.TryParse(ddlCategory.SelectedValue, out categoryId))
            {
                alert.ShowError("Choose a category for this product.");
                return;
            }

            int supplierId;
            var supplier = int.TryParse(ddlSupplier.SelectedValue, out supplierId) ? (int?)supplierId : null;

            byte[] imageBytes = null;
            string imageContentType = null;

            if (fuImage.HasFile)
            {
                if (!TryReadUpload(out imageBytes, out imageContentType)) return;
            }

            try
            {
                var input = new Product
                {
                    ProductId = ProductId,
                    Barcode = txtBarcode.Text,
                    Name = txtName.Text,
                    Description = txtDescription.Text,
                    CategoryId = categoryId,
                    SupplierId = supplier,
                    CostPrice = cost,
                    SellingPrice = price,
                    VatRate = vat,
                    ReorderLevel = reorder,
                    ImageUrl = txtImageUrl.Text,
                    IsActive = chkActive.Checked
                };

                CatalogService.SaveProduct(input, imageBytes, imageContentType);
                Response.Redirect("~/Manager/Products.aspx?saved=1", true);
            }
            catch (System.Threading.ThreadAbortException)
            {
                throw; // Response.Redirect
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Validates the upload before a single byte reaches the database: size first, then the
        /// declared content type, then the file's own magic bytes so a renamed .exe is rejected.
        /// </summary>
        private bool TryReadUpload(out byte[] bytes, out string contentType)
        {
            bytes = null;
            contentType = null;

            var maxBytes = 2 * 1024 * 1024;
            int configured;
            if (int.TryParse(ConfigurationManager.AppSettings["MaxImageUploadBytes"], out configured) && configured > 0)
                maxBytes = configured;

            if (fuImage.PostedFile.ContentLength > maxBytes)
            {
                alert.ShowError(string.Format("That image is {0:N1} MB. The limit is {1:N1} MB.",
                    fuImage.PostedFile.ContentLength / 1048576.0, maxBytes / 1048576.0));
                return false;
            }

            var declared = (fuImage.PostedFile.ContentType ?? string.Empty).ToLowerInvariant();
            if (!AllowedImageTypes.Contains(declared))
            {
                alert.ShowError("Only JPEG, PNG, GIF or WebP images can be uploaded.");
                return false;
            }

            var data = new byte[fuImage.PostedFile.ContentLength];
            fuImage.PostedFile.InputStream.Read(data, 0, data.Length);

            if (!LooksLikeImage(data))
            {
                alert.ShowError("That file doesn't appear to be a real image. Please choose another.");
                return false;
            }

            bytes = data;
            contentType = declared == "image/pjpeg" ? "image/jpeg" : declared;
            return true;
        }

        /// <summary>Checks the file signature rather than trusting the extension or the posted type.</summary>
        private static bool LooksLikeImage(byte[] data)
        {
            if (data == null || data.Length < 12) return false;

            // JPEG: FF D8 FF
            if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF) return true;
            // PNG: 89 50 4E 47
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47) return true;
            // GIF: "GIF8"
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38) return true;
            // WebP: "RIFF" .... "WEBP"
            if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50) return true;

            return false;
        }

        protected void btnClearImage_Click(object sender, EventArgs e)
        {
            try
            {
                CatalogService.ClearProductImage(ProductId);
                alert.ShowSuccess("Uploaded photo removed. The image URL will be used instead.");

                var product = CatalogService.GetProduct(ProductId);
                if (product != null) Bind(product);
            }
            catch (BusinessRuleException ex)
            {
                alert.ShowError(ex.Message);
            }
        }
    }
}
