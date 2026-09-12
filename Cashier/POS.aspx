<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="POS.aspx.cs" Inherits="SmartShelf.Cashier.POS" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">POS Terminal &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:TextBox ID="txtSearch" runat="server" CssClass="search-input" AutoPostBack="true"
               OnTextChanged="txtSearch_TextChanged"
               placeholder="Search or scan a barcode, then press Enter" />
  <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-ghost btn-sm"
              OnClick="btnSearch_Click" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="pos-layout">

  <%-- Catalogue: tap a tile to add one unit. Out-of-stock tiles are disabled rather than
       hidden, so a cashier can see the product exists and tell the customer why. --%>
  <div>
    <div style="font-size:13px;font-weight:700;text-transform:uppercase;letter-spacing:0.04em;color:var(--color-muted);margin-bottom:12px;">
      <asp:Literal ID="litCatalogHint" runat="server" />
    </div>

    <asp:Repeater ID="rptCatalog" runat="server" OnItemCommand="rptCatalog_ItemCommand">
      <HeaderTemplate><div class="catalog-grid"></HeaderTemplate>
      <ItemTemplate>
        <asp:LinkButton runat="server" CssClass="catalog-item" CommandName="add"
                        CommandArgument='<%# Eval("ProductId") %>'
                        Enabled='<%# (int)Eval("QuantityOnHand") > 0 %>'
                        CausesValidation="false">
          <img src='<%# GetImageUrl(Container.DataItem) %>' alt="" />
          <div class="cn"><%# Server.HtmlEncode((string)Eval("Name")) %></div>
          <div class="cp">R <%# ((decimal)Eval("SellingPrice")).ToString("N2") %></div>
          <div class="cs"><%# StockCaption(Container.DataItem) %></div>
        </asp:LinkButton>
      </ItemTemplate>
      <FooterTemplate></div></FooterTemplate>
    </asp:Repeater>

    <asp:PlaceHolder ID="phNoProducts" runat="server" Visible="false">
      <div class="app-card empty-state">Nothing matched that search. Clear the box to see the full catalogue.</div>
    </asp:PlaceHolder>
  </div>

  <%-- Current sale. Every figure here is calculated by SaleService from the database. --%>
  <div class="app-card" style="position:sticky;top:24px;">
    <div class="card-head" style="margin-bottom:6px;">
      <h4 style="font-size:17px;">Current Sale</h4>
      <span class="tag"><asp:Literal ID="litItemCount" runat="server" /></span>
    </div>

    <asp:Repeater ID="rptCart" runat="server" OnItemCommand="rptCart_ItemCommand">
      <ItemTemplate>
        <div class="cart-item">
          <img src='<%# GetCartImageUrl(Container.DataItem) %>' alt="" />
          <div class="ci-info">
            <div class="ci-name"><%# Server.HtmlEncode((string)Eval("Name")) %></div>
            <div class="ci-price">R <%# ((decimal)Eval("UnitPrice")).ToString("N2") %> each</div>
          </div>
          <div class="ci-qty">
            <asp:LinkButton runat="server" CssClass="qty-btn" CommandName="dec"
                            CommandArgument='<%# Eval("ProductId") %>' CausesValidation="false"
                            ToolTip="Reduce quantity">&#8722;</asp:LinkButton>
            <span style="font-size:13px;min-width:16px;text-align:center;"><%# Eval("Quantity") %></span>
            <asp:LinkButton runat="server" CssClass="qty-btn" CommandName="inc"
                            CommandArgument='<%# Eval("ProductId") %>' CausesValidation="false"
                            ToolTip="Increase quantity">+</asp:LinkButton>
          </div>
          <div class="ci-total">R <%# LineTotal(Container.DataItem) %></div>
          <asp:LinkButton runat="server" CssClass="ci-remove" CommandName="del"
                          CommandArgument='<%# Eval("ProductId") %>' CausesValidation="false"
                          ToolTip="Remove from sale">&times;</asp:LinkButton>
        </div>
      </ItemTemplate>
    </asp:Repeater>

    <asp:PlaceHolder ID="phEmptyCart" runat="server">
      <div style="padding:30px 0;text-align:center;color:var(--color-muted);font-size:13.5px;">
        Cart is empty &mdash; tap a product to start a sale.
      </div>
    </asp:PlaceHolder>

    <div style="margin-top:16px;padding-top:16px;border-top:1px dashed var(--color-line);">
      <div style="display:flex;justify-content:space-between;font-size:13.5px;color:var(--color-muted);margin-bottom:6px;">
        <span>Subtotal</span><span><asp:Literal ID="litSubtotal" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;color:var(--color-muted);margin-bottom:10px;">
        <span>VAT</span><span><asp:Literal ID="litVat" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-family:var(--font-display);font-size:24px;font-weight:600;color:var(--color-primary-dark);">
        <span>Total</span><span><asp:Literal ID="litTotal" runat="server" /></span>
      </div>
    </div>

    <asp:Button ID="btnPay" runat="server" Text="Proceed to Payment &rarr;" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;margin-top:18px;padding:14px;"
                OnClick="btnPay_Click" CausesValidation="false" />
    <asp:Button ID="btnVoid" runat="server" Text="Void Sale" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnVoid_Click" CausesValidation="false"
                OnClientClick="return confirm('Clear this whole sale?');" />
  </div>
</div>
</asp:Content>

<asp:Content ID="cModal" ContentPlaceHolderID="ModalContent" runat="server">

<%-- Payment. The total shown is re-read from the server on every render; the only thing the
     browser sends back is the payment method and the cash actually handed over. --%>
<div class="modal-backdrop <%= PayModalVisible ? "show" : "" %>">
  <div class="modal-box">
    <span class="eyebrow">Payment</span>
    <h3 style="font-size:22px;margin-bottom:4px;">Take payment</h3>
    <div style="font-family:var(--font-display);font-size:34px;font-weight:600;color:var(--color-primary-dark);margin:10px 0;">
      <asp:Literal ID="litPayTotal" runat="server" />
    </div>

    <div class="pay-type-row">
      <asp:LinkButton ID="btnPayCash" runat="server" CssClass="pay-type" Text="Cash"
                      CommandArgument="Cash" OnCommand="SelectPayMethod" CausesValidation="false" />
      <asp:LinkButton ID="btnPayCard" runat="server" CssClass="pay-type" Text="Card"
                      CommandArgument="Card" OnCommand="SelectPayMethod" CausesValidation="false" />
      <asp:LinkButton ID="btnPayMobile" runat="server" CssClass="pay-type" Text="Mobile"
                      CommandArgument="Mobile" OnCommand="SelectPayMethod" CausesValidation="false" />
    </div>

    <asp:PlaceHolder ID="phCashFields" runat="server">
      <div class="field-group">
        <asp:Label ID="lblTendered" runat="server" AssociatedControlID="txtTendered" Text="Amount tendered (R)" />
        <asp:TextBox ID="txtTendered" runat="server" TextMode="Number" step="0.01" min="0"
                     AutoPostBack="true" OnTextChanged="txtTendered_TextChanged" ValidationGroup="Pay" />
        <asp:RequiredFieldValidator ID="rfvTendered" runat="server" ControlToValidate="txtTendered"
             Display="Dynamic" CssClass="field-error" ValidationGroup="Pay"
             ErrorMessage="Enter the cash the customer handed over." />
        <asp:CompareValidator ID="cvTendered" runat="server" ControlToValidate="txtTendered"
             Operator="DataTypeCheck" Type="Currency" Display="Dynamic" CssClass="field-error"
             ValidationGroup="Pay" ErrorMessage="Enter a valid amount, for example 250.00" />
      </div>

      <div class="app-card" style="background:var(--color-bg-warm);border:none;padding:14px 16px;margin-bottom:16px;">
        <div style="display:flex;justify-content:space-between;font-size:14px;font-weight:600;">
          <span>Change due</span><span><asp:Literal ID="litChange" runat="server" /></span>
        </div>
        <div class="hint" style="margin-top:4px;">Calculated on the server from the sale total.</div>
      </div>
    </asp:PlaceHolder>

    <asp:Button ID="btnConfirmPay" runat="server" Text="Confirm Payment" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;padding:13px;"
                OnClick="btnConfirmPay_Click" ValidationGroup="Pay" />
    <asp:Button ID="btnCancelPay" runat="server" Text="Cancel" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCancelPay_Click" CausesValidation="false" />
  </div>
</div>

<%-- Receipt confirmation --%>
<div class="modal-backdrop <%= ReceiptModalVisible ? "show" : "" %>">
  <div class="modal-box" style="text-align:center;">
    <div style="width:52px;height:52px;border-radius:50%;background:var(--color-primary-tint);color:var(--color-primary);display:flex;align-items:center;justify-content:center;margin:0 auto 14px;font-size:24px;">&#10003;</div>
    <span class="eyebrow" style="justify-content:center;">Receipt</span>
    <h3 style="font-size:22px;margin-bottom:6px;">Payment complete</h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:6px;">
      Receipt <strong><asp:Literal ID="litReceiptNo" runat="server" /></strong> &middot; sale marked completed
    </p>
    <p style="font-size:15px;font-weight:600;margin-bottom:18px;"><asp:Literal ID="litChangeSummary" runat="server" /></p>
    <div style="display:flex;gap:10px;">
      <asp:HyperLink ID="lnkReceipt" runat="server" CssClass="btn btn-primary" Target="_blank"
                     style="flex:1;justify-content:center;" Text="Print receipt" />
      <asp:Button ID="btnNewSale" runat="server" Text="New sale" CssClass="btn btn-ghost"
                  style="flex:1;justify-content:center;" OnClick="btnNewSale_Click" CausesValidation="false" />
    </div>
  </div>
</div>

</asp:Content>
