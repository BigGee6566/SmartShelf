using System;
using SmartShelf.Models;
using SmartShelf.Services;

namespace SmartShelf.Cashier
{
    /// <summary>
    /// Printable receipt for a completed sale. Everything shown is the snapshot stored on the
    /// sale itself, so reprinting an old receipt reproduces what the customer was actually charged.
    /// </summary>
    public partial class Receipt : SecurePageBase
    {
        protected override string[] AllowedRoles
        {
            get { return new[] { RoleNames.Cashier, RoleNames.Manager }; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            var master = (AppMaster)Master;
            master.PageTitle = "Receipt";
            master.PageSubtitle = "Sale record";

            if (IsPostBack) return;

            int saleId;
            if (!int.TryParse(Request.QueryString["id"], out saleId))
            {
                ShowNotFound();
                return;
            }

            var sale = SaleService.GetSaleWithLines(saleId);
            if (sale == null)
            {
                ShowNotFound();
                return;
            }

            // A cashier may reprint only their own sales; a manager can reprint any of them.
            if (!CurrentUser.IsInRole(RoleNames.Manager) && sale.CashierUserId != CurrentUser.UserId)
            {
                Response.Redirect("~/AccessDenied.aspx", true);
                return;
            }

            Bind(sale);
        }

        private void Bind(Sale sale)
        {
            litReceiptNo.Text = Server.HtmlEncode(sale.ReceiptNumber);
            litWhen.Text = sale.SaleDate.ToString("dd MMM yyyy, HH:mm");

            rptLines.DataSource = sale.Lines;
            rptLines.DataBind();

            litSubtotal.Text = Money(sale.Subtotal);
            litVat.Text = Money(sale.VatAmount);
            litTotal.Text = Money(sale.Total);

            litMethod.Text = sale.PaymentMethod == PaymentMethod.Cash
                ? "Cash tendered"
                : "Paid by " + sale.PaymentMethod.ToString().ToLower();
            litTendered.Text = Money(sale.AmountTendered);
            litChange.Text = Money(sale.ChangeDue);

            litCashier.Text = Server.HtmlEncode(sale.CashierUser != null ? sale.CashierUser.FullName : "-");
            litStatus.Text = sale.Status == SaleStatus.Voided
                ? "*** THIS SALE WAS VOIDED ***"
                : "Sale completed";

            master_SetSubtitle(sale.ReceiptNumber);
        }

        private void master_SetSubtitle(string receiptNumber)
        {
            ((AppMaster)Master).PageSubtitle = receiptNumber;
        }

        /// <summary>Zero-rated lines are called out, since the customer's total won't show VAT on them.</summary>
        protected string VatLabel(object dataItem)
        {
            var line = (SaleLine)dataItem;
            return line.VatRate <= 0m ? "zero-rated" : (line.VatRate * 100m).ToString("0.#") + "% VAT";
        }

        private void ShowNotFound()
        {
            phReceipt.Visible = false;
            phNotFound.Visible = true;
        }
    }
}
