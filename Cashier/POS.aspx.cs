using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Cashier
{
    /// <summary>
    /// The till (Focus D). The cart lives in Session as product/quantity pairs only - no price
    /// or total ever survives a round trip through the browser. Every figure on screen is
    /// recalculated by SaleService from the database.
    /// </summary>
    public partial class POS : SecurePageBase
    {
        private const string CartKey = "POS_Cart";
        private const string PayMethodKey = "POS_PayMethod";

        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Cashier, RoleNames.Manager }; }
        }

        protected bool PayModalVisible { get; private set; }
        protected bool ReceiptModalVisible { get; private set; }

        private List<CartLine> Cart
        {
            get
            {
                var cart = Session[CartKey] as List<CartLine>;
                if (cart == null)
                {
                    cart = new List<CartLine>();
                    Session[CartKey] = cart;
                }
                return cart;
            }
            set { Session[CartKey] = value; }
        }

        private PaymentMethod SelectedPayMethod
        {
            get
            {
                var raw = Session[PayMethodKey] as string;
                PaymentMethod parsed;
                return Enum.TryParse(raw, out parsed) ? parsed : PaymentMethod.Cash;
            }
            set { Session[PayMethodKey] = value.ToString(); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "POS Terminal";
            master.PageSubtitle = AppMaster.TodayLabel() + " · signed in as " + CurrentUser.FullName;

            if (!IsPostBack)
            {
                BindCatalog();
            }

            BindCart();
        }

        // ----------------------------------------------------------
        // Catalogue
        // ----------------------------------------------------------

        private void BindCatalog()
        {
            var term = txtSearch.Text;
            var products = CatalogService.SearchProducts(term, null, true, 60);

            rptCatalog.DataSource = products;
            rptCatalog.DataBind();

            phNoProducts.Visible = products.Count == 0;
            litCatalogHint.Text = string.IsNullOrWhiteSpace(term)
                ? "Tap a product to add it"
                : Server.HtmlEncode(products.Count + " match(es) for \"" + term.Trim() + "\"");
        }

        /// <summary>
        /// Typing in the search box and pressing the button both post back in the same request
        /// (TextChanged fires, then Click). Without this guard the second handler would clear the
        /// message the first one just raised - which is exactly what a cashier needs to read.
        /// </summary>
        private bool _searchHandled;

        protected void txtSearch_TextChanged(object sender, EventArgs e) { SearchOrScan(); }
        protected void btnSearch_Click(object sender, EventArgs e) { SearchOrScan(); }

        /// <summary>
        /// One box does both jobs: an exact barcode match goes straight into the cart the way a
        /// scanner would, anything else filters the grid.
        /// </summary>
        private void SearchOrScan()
        {
            if (_searchHandled) return;
            _searchHandled = true;

            alert.Hide();
            var term = (txtSearch.Text ?? string.Empty).Trim();

            if (term.Length > 0)
            {
                var scanned = CatalogService.FindByBarcode(term);
                if (scanned != null)
                {
                    AddToCart(scanned.ProductId);
                    txtSearch.Text = string.Empty;
                    BindCatalog();
                    BindCart();
                    return;
                }
            }

            BindCatalog();
        }

        protected string GetImageUrl(object dataItem)
        {
            return ImageUrlFor((Product)dataItem);
        }

        private string ImageUrlFor(Product product)
        {
            // An uploaded image is served by the handler; otherwise fall back to the stored URL.
            if (product.ImageData != null && product.ImageData.Length > 0)
                return ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId);

            return string.IsNullOrWhiteSpace(product.ImageUrl)
                ? ResolveUrl("~/Handlers/ProductImage.ashx?id=" + product.ProductId)
                : product.ImageUrl;
        }

        protected string StockCaption(object dataItem)
        {
            var product = (Product)dataItem;
            if (product.QuantityOnHand <= 0) return "Out of stock";
            if (product.QuantityOnHand <= product.ReorderLevel) return "Only " + product.QuantityOnHand + " left";
            return product.QuantityOnHand + " in stock";
        }

        protected void rptCatalog_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "add") return;

            int productId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out productId)) return;

            alert.Hide();
            AddToCart(productId);
            BindCatalog();
            BindCart();
        }

        // ----------------------------------------------------------
        // Cart
        // ----------------------------------------------------------

        private void AddToCart(int productId)
        {
            var product = CatalogService.GetProduct(productId);
            if (product == null || !product.IsActive)
            {
                alert.ShowError("That product is no longer on the catalogue.");
                return;
            }

            var cart = Cart;
            var existing = cart.FirstOrDefault(c => c.ProductId == productId);
            var wanted = (existing == null ? 0 : existing.Quantity) + 1;

            // Checked here for a quick, friendly message; SaleService checks again inside the
            // transaction, which is what actually protects the stock level.
            if (wanted > product.QuantityOnHand)
            {
                alert.ShowError(string.Format("Only {0} of {1} left on the shelf.",
                                              product.QuantityOnHand, product.Name));
                return;
            }

            if (existing != null)
            {
                existing.Quantity = wanted;
            }
            else
            {
                cart.Add(new CartLine
                {
                    ProductId = product.ProductId,
                    Quantity = 1,
                    Name = product.Name,
                    UnitPrice = product.SellingPrice,
                    ImageUrl = ImageUrlFor(product)
                });
            }

            Cart = cart;
        }

        protected void rptCart_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int productId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out productId)) return;

            var cart = Cart;
            var line = cart.FirstOrDefault(c => c.ProductId == productId);
            if (line == null) return;

            alert.Hide();

            switch (e.CommandName)
            {
                case "inc":
                    // May raise its own "not enough stock" message, so hide first, add second.
                    AddToCart(productId);
                    break;
                case "dec":
                    line.Quantity -= 1;
                    if (line.Quantity <= 0) cart.Remove(line);
                    Cart = cart;
                    break;
                case "del":
                    cart.Remove(line);
                    Cart = cart;
                    break;
            }

            BindCatalog();
            BindCart();
        }

        private void BindCart()
        {
            var cart = Cart;

            rptCart.DataSource = cart;
            rptCart.DataBind();
            phEmptyCart.Visible = cart.Count == 0;

            var totals = SaleService.CalculateTotals(cart);
            litSubtotal.Text = Money(totals.Subtotal);
            litVat.Text = Money(totals.Vat);
            litTotal.Text = Money(totals.Total);
            litItemCount.Text = totals.ItemCount + (totals.ItemCount == 1 ? " item" : " items");

            btnPay.Enabled = cart.Count > 0;
            btnVoid.Enabled = cart.Count > 0;
        }

        protected string GetCartImageUrl(object dataItem)
        {
            var line = (CartLine)dataItem;
            return string.IsNullOrEmpty(line.ImageUrl)
                ? ResolveUrl("~/Handlers/ProductImage.ashx?id=" + line.ProductId)
                : line.ImageUrl;
        }

        protected string LineTotal(object dataItem)
        {
            var line = (CartLine)dataItem;
            return (line.UnitPrice * line.Quantity).ToString("N2");
        }

        protected void btnVoid_Click(object sender, EventArgs e)
        {
            Cart = new List<CartLine>();
            alert.ShowInfo("Sale cleared.");
            BindCatalog();
            BindCart();
        }

        // ----------------------------------------------------------
        // Payment
        // ----------------------------------------------------------

        protected void btnPay_Click(object sender, EventArgs e)
        {
            if (Cart.Count == 0)
            {
                alert.ShowError("There is nothing in the cart to pay for.");
                return;
            }

            SelectedPayMethod = PaymentMethod.Cash;
            txtTendered.Text = string.Empty;
            OpenPayModal();
        }

        private void OpenPayModal()
        {
            PayModalVisible = true;

            var totals = SaleService.CalculateTotals(Cart);
            litPayTotal.Text = Money(totals.Total);

            var method = SelectedPayMethod;
            btnPayCash.CssClass = method == PaymentMethod.Cash ? "pay-type active" : "pay-type";
            btnPayCard.CssClass = method == PaymentMethod.Card ? "pay-type active" : "pay-type";
            btnPayMobile.CssClass = method == PaymentMethod.Mobile ? "pay-type active" : "pay-type";

            // Card and mobile settle the exact amount, so the tendered field would be noise.
            phCashFields.Visible = method == PaymentMethod.Cash;
            rfvTendered.Enabled = method == PaymentMethod.Cash;
            cvTendered.Enabled = method == PaymentMethod.Cash;

            UpdateChangePreview(totals.Total);
        }

        protected void SelectPayMethod(object sender, CommandEventArgs e)
        {
            PaymentMethod method;
            if (Enum.TryParse(Convert.ToString(e.CommandArgument), out method))
            {
                SelectedPayMethod = method;
            }
            OpenPayModal();
        }

        protected void txtTendered_TextChanged(object sender, EventArgs e)
        {
            OpenPayModal();
        }

        /// <summary>Preview only - the change actually recorded is computed inside SaleService.</summary>
        private void UpdateChangePreview(decimal total)
        {
            decimal tendered;
            if (!TryParseAmount(txtTendered.Text, out tendered) || tendered < total)
            {
                litChange.Text = Money(0m);
                return;
            }
            litChange.Text = Money(decimal.Round(tendered - total, 2));
        }

        protected void btnCancelPay_Click(object sender, EventArgs e)
        {
            PayModalVisible = false;
            alert.Hide();
        }

        protected void btnConfirmPay_Click(object sender, EventArgs e)
        {
            Page.Validate("Pay");
            if (!Page.IsValid)
            {
                OpenPayModal();
                return;
            }

            var method = SelectedPayMethod;

            decimal tendered = 0m;
            if (method == PaymentMethod.Cash && !decimal.TryParse(txtTendered.Text, out tendered))
            {
                alert.ShowError("Enter the cash amount as a number, for example 250.00");
                OpenPayModal();
                return;
            }

            try
            {
                var result = SaleService.CompleteSale(Cart, method, tendered, CurrentUser.UserId);

                Cart = new List<CartLine>();
                PayModalVisible = false;
                ReceiptModalVisible = true;

                litReceiptNo.Text = Server.HtmlEncode(result.ReceiptNumber);
                litChangeSummary.Text = method == PaymentMethod.Cash
                    ? "Change due: " + Money(result.ChangeDue)
                    : "Paid " + Money(result.Total) + " by " + method.ToString().ToLower();
                lnkReceipt.NavigateUrl = ResolveUrl("~/Cashier/Receipt.aspx?id=" + result.SaleId);

                alert.Hide();
            }
            catch (BusinessRuleException ex)
            {
                // Covers insufficient stock, short payment and a delisted product - all things
                // the cashier can act on, so the message goes straight to the screen.
                alert.ShowError(ex.Message);
                OpenPayModal();
            }

            BindCatalog();
            BindCart();
        }

        protected void btnNewSale_Click(object sender, EventArgs e)
        {
            ReceiptModalVisible = false;
            alert.Hide();
            txtSearch.Text = string.Empty;
            BindCatalog();
            BindCart();
        }
    }
}
