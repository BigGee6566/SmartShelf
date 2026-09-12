<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="PurchaseOrderEdit.aspx.cs" Inherits="SmartShelf.Manager.PurchaseOrderEdit" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Purchase order &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/PurchaseOrders.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; All orders" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<%-- ============ New order ============ --%>
<asp:PlaceHolder ID="phNew" runat="server">
<div class="app-card">
  <div class="card-head">
    <h4>Raise a purchase order</h4>
    <asp:Button ID="btnSuggest" runat="server" Text="Fill from low-stock report" CssClass="btn btn-ghost btn-sm"
                OnClick="btnSuggest_Click" CausesValidation="false" />
  </div>

  <div class="filter-bar">
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlSupplier" Text="Supplier *" />
      <asp:DropDownList ID="ddlSupplier" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlSupplier_Changed" />
    </div>
    <div class="field-group" style="flex:1;min-width:260px;">
      <asp:Label runat="server" AssociatedControlID="txtNotes" Text="Notes to the supplier" />
      <asp:TextBox ID="txtNotes" runat="server" MaxLength="300" />
    </div>
  </div>

  <asp:PlaceHolder ID="phPickSupplier" runat="server">
    <div class="empty-state">Choose a supplier to see the products you can order from them.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptLines" runat="server">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Product</th><th class="text-right">On hand</th><th class="text-right">Reorder at</th>
          <th class="text-right">Unit cost</th><th class="text-right" style="width:130px;">Order qty</th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td>
          <%# Server.HtmlEncode((string)Eval("Name")) %>
          <asp:HiddenField ID="hfProductId" runat="server" Value='<%# Eval("ProductId") %>' />
        </td>
        <td class="num-cell"><%# Eval("QuantityOnHand") %></td>
        <td class="num-cell"><%# Eval("ReorderLevel") %></td>
        <td class="num-cell">R <%# ((decimal)Eval("CostPrice")).ToString("N2") %></td>
        <td>
          <asp:TextBox ID="txtQty" runat="server" TextMode="Number" min="0" step="1"
                       Text='<%# SuggestedQty(Container.DataItem) %>' style="text-align:right;" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <asp:PlaceHolder ID="phNewActions" runat="server" Visible="false">
    <div class="form-actions">
      <asp:Button ID="btnCreate" runat="server" Text="Save as draft" CssClass="btn btn-primary" OnClick="btnCreate_Click" />
      <asp:Button ID="btnCreateAndSend" runat="server" Text="Save and send to supplier" CssClass="btn btn-accent"
                  OnClick="btnCreateAndSend_Click" />
    </div>
    <p class="hint" style="margin-top:10px;">
      Leave a quantity at zero to leave that product off the order. The expected delivery date is
      worked out from this supplier&#39;s lead time.
    </p>
  </asp:PlaceHolder>
</div>
</asp:PlaceHolder>

<%-- ============ Existing order ============ --%>
<asp:PlaceHolder ID="phExisting" runat="server" Visible="false">
<div class="dash-grid">
  <div class="app-card">
    <div class="card-head">
      <h4><asp:Literal ID="litOrderNumber" runat="server" /></h4>
      <span class="pill <%= StatusCssClass %>"><asp:Literal ID="litStatus" runat="server" /></span>
    </div>

    <asp:Repeater ID="rptExistingLines" runat="server" OnItemCommand="rptExistingLines_ItemCommand">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr>
            <th>Product</th><th class="text-right">Ordered</th><th class="text-right">Received</th>
            <th class="text-right">Outstanding</th><th class="text-right">Unit cost</th><th></th>
          </tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td><%# ProductName(Container.DataItem) %></td>
          <td class="num-cell"><%# Eval("QuantityOrdered") %></td>
          <td class="num-cell"><%# Eval("QuantityReceived") %></td>
          <td class="num-cell"><strong><%# Outstanding(Container.DataItem) %></strong></td>
          <td class="num-cell">R <%# ((decimal)Eval("UnitCost")).ToString("N2") %></td>
          <td class="text-right">
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="receive"
                            CommandArgument='<%# Eval("PurchaseOrderLineId") %>' Text="Receive"
                            Visible='<%# CanReceive(Container.DataItem) %>' CausesValidation="false" />
          </td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>
  </div>

  <div class="app-card">
    <h4 style="font-size:16px;margin-bottom:16px;">Order details</h4>
    <table class="data-table">
      <tbody>
        <tr><td>Supplier</td><td class="text-right"><asp:Literal ID="litSupplier" runat="server" /></td></tr>
        <tr><td>Raised by</td><td class="text-right"><asp:Literal ID="litRaisedBy" runat="server" /></td></tr>
        <tr><td>Raised on</td><td class="text-right"><asp:Literal ID="litRaisedOn" runat="server" /></td></tr>
        <tr><td>Expected</td><td class="text-right"><asp:Literal ID="litExpected" runat="server" /></td></tr>
        <tr><td>Confirmed</td><td class="text-right"><asp:Literal ID="litConfirmed" runat="server" /></td></tr>
        <tr><td>Delivered</td><td class="text-right"><asp:Literal ID="litDelivered" runat="server" /></td></tr>
        <tr><td>Order value</td><td class="text-right"><strong><asp:Literal ID="litValue" runat="server" /></strong></td></tr>
      </tbody>
    </table>

    <asp:PlaceHolder ID="phNotes" runat="server" Visible="false">
      <div style="margin-top:16px;padding-top:14px;border-top:1px dashed var(--color-line);">
        <div class="muted" style="font-size:12.5px;text-transform:uppercase;letter-spacing:0.05em;font-weight:700;margin-bottom:6px;">Notes</div>
        <p style="font-size:13.5px;"><asp:Literal ID="litNotes" runat="server" /></p>
      </div>
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phSupplierNote" runat="server" Visible="false">
      <div style="margin-top:16px;padding-top:14px;border-top:1px dashed var(--color-line);">
        <div class="muted" style="font-size:12.5px;text-transform:uppercase;letter-spacing:0.05em;font-weight:700;margin-bottom:6px;">From the supplier</div>
        <p style="font-size:13.5px;"><asp:Literal ID="litSupplierNote" runat="server" /></p>
      </div>
    </asp:PlaceHolder>

    <div class="form-actions" style="margin-top:20px;">
      <asp:Button ID="btnSend" runat="server" Text="Send to supplier" CssClass="btn btn-primary"
                  OnClick="btnSend_Click" CausesValidation="false" Visible="false" />
      <asp:Button ID="btnCancelOrder" runat="server" Text="Cancel order" CssClass="btn btn-ghost"
                  OnClick="btnCancelOrder_Click" CausesValidation="false" Visible="false"
                  OnClientClick="return confirm('Cancel this purchase order?');" />
    </div>
  </div>
</div>
</asp:PlaceHolder>

</asp:Content>

<asp:Content ID="cModal" ContentPlaceHolderID="ModalContent" runat="server">

<%-- Goods received: this is where stock actually goes up, and where the batch expiry is captured. --%>
<div class="modal-backdrop <%= ReceiveModalVisible ? "show" : "" %>">
  <div class="modal-box">
    <span class="eyebrow">Goods received</span>
    <h3 style="font-size:21px;margin-bottom:4px;"><asp:Literal ID="litReceiveProduct" runat="server" /></h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:18px;">
      <strong><asp:Literal ID="litReceiveOutstanding" runat="server" /></strong> still outstanding on this line.
    </p>

    <asp:HiddenField ID="hfLineId" runat="server" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtReceiveQty" Text="Units received *" />
      <asp:TextBox ID="txtReceiveQty" runat="server" TextMode="Number" min="1" step="1" ValidationGroup="Receive" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtReceiveQty" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Receive" ErrorMessage="Enter how many units arrived." />
      <asp:RangeValidator runat="server" ControlToValidate="txtReceiveQty" MinimumValue="1" MaximumValue="1000000"
           Type="Integer" Display="Dynamic" CssClass="field-error" ValidationGroup="Receive"
           ErrorMessage="Enter a whole number of 1 or more." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtExpiry" Text="Expiry date" />
      <asp:TextBox ID="txtExpiry" runat="server" TextMode="Date" ValidationGroup="Receive" />
      <span class="hint">Leave blank for goods that don&#39;t expire. Feeds the expiry and waste report.</span>
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtBatchCode" Text="Batch code" />
      <asp:TextBox ID="txtBatchCode" runat="server" MaxLength="40" placeholder="From the delivery note" />
    </div>

    <asp:Button ID="btnSaveReceive" runat="server" Text="Book in stock" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;padding:13px;"
                ValidationGroup="Receive" OnClick="btnSaveReceive_Click" />
    <asp:Button ID="btnCancelReceive" runat="server" Text="Cancel" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCancelReceive_Click" CausesValidation="false" />
  </div>
</div>

</asp:Content>
