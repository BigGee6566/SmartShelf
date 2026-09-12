<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Orders.aspx.cs" Inherits="SmartShelf.SupplierPortal.Orders" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">My orders &mdash; SmartShelf</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="stat-card-row">
  <div class="stat-card">
    <div class="sc-lbl">Awaiting your confirmation</div>
    <div class="sc-num" style="<%= PendingStyle %>"><asp:Literal ID="litPending" runat="server" /></div>
    <div class="sc-delta muted">Sent by the store</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Confirmed, not yet delivered</div>
    <div class="sc-num"><asp:Literal ID="litConfirmed" runat="server" /></div>
    <div class="sc-delta muted">In progress</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Delivered in full</div>
    <div class="sc-num"><asp:Literal ID="litDelivered" runat="server" /></div>
    <div class="sc-delta up">Completed orders</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Value ordered from you</div>
    <div class="sc-num" style="font-size:24px;"><asp:Literal ID="litValue" runat="server" /></div>
    <div class="sc-delta muted">All open and completed orders</div>
  </div>
</div>

<div class="app-card">
  <div class="card-head">
    <h4>Purchase orders from <asp:Literal ID="litCompany" runat="server" /></h4>
    <div class="filter-bar" style="margin-bottom:0;">
      <div class="field-group">
        <asp:DropDownList ID="ddlStatus" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlStatus_Changed">
          <asp:ListItem Value="" Text="All orders" />
          <asp:ListItem Value="Sent" Text="Needs confirming" />
          <asp:ListItem Value="Confirmed" Text="Confirmed" />
          <asp:ListItem Value="Dispatched" Text="Dispatched" />
          <asp:ListItem Value="PartiallyDelivered" Text="Part delivered" />
          <asp:ListItem Value="Delivered" Text="Delivered" />
        </asp:DropDownList>
      </div>
    </div>
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">No orders here at the moment.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptOrders" runat="server" OnItemCommand="rptOrders_ItemCommand">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Order</th><th>Received</th><th>Wanted by</th><th class="text-right">Lines</th>
          <th class="text-right">Units</th><th class="text-right">Value</th><th>Status</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("OrderNumber")) %></strong></td>
        <td style="white-space:nowrap;"><%# SentOn(Container.DataItem) %></td>
        <td style="white-space:nowrap;"><%# ExpectedCell(Container.DataItem) %></td>
        <td class="num-cell"><%# LineCount(Container.DataItem) %></td>
        <td class="num-cell"><%# UnitCount(Container.DataItem) %></td>
        <td class="num-cell">R <%# OrderValue(Container.DataItem) %></td>
        <td><span class="pill <%# StatusCss(Container.DataItem) %>"><%# StatusLabel(Container.DataItem) %></span></td>
        <td class="text-right" style="white-space:nowrap;">
          <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="view"
                          CommandArgument='<%# Eval("PurchaseOrderId") %>' Text="View" CausesValidation="false" />
          <asp:LinkButton runat="server" CssClass="btn btn-primary btn-sm" CommandName="confirm"
                          CommandArgument='<%# Eval("PurchaseOrderId") %>' Text="Confirm"
                          Visible='<%# NeedsConfirming(Container.DataItem) %>' CausesValidation="false" />
          <asp:LinkButton runat="server" CssClass="btn btn-accent btn-sm" CommandName="dispatch"
                          CommandArgument='<%# Eval("PurchaseOrderId") %>' Text="Mark dispatched"
                          Visible='<%# CanDispatch(Container.DataItem) %>' CausesValidation="false" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <p class="hint" style="margin-top:16px;">
    Marking an order dispatched tells the store it is on its way. Stock is only added to their
    shelf when they physically count the delivery in, so the two records stay independent.
  </p>
</div>

</asp:Content>

<asp:Content ID="cModal" ContentPlaceHolderID="ModalContent" runat="server">

<%-- Order detail, with confirm / dispatch actions in place --%>
<div class="modal-backdrop <%= DetailModalVisible ? "show" : "" %>">
  <div class="modal-box" style="width:560px;">
    <span class="eyebrow">Purchase order</span>
    <h3 style="font-size:22px;margin-bottom:4px;"><asp:Literal ID="litDetailNumber" runat="server" /></h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:18px;"><asp:Literal ID="litDetailMeta" runat="server" /></p>

    <asp:HiddenField ID="hfOrderId" runat="server" />

    <asp:Repeater ID="rptDetailLines" runat="server">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Product</th><th class="text-right">Ordered</th><th class="text-right">Received</th><th class="text-right">Unit cost</th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td><%# DetailProductName(Container.DataItem) %></td>
          <td class="num-cell"><%# Eval("QuantityOrdered") %></td>
          <td class="num-cell"><%# Eval("QuantityReceived") %></td>
          <td class="num-cell">R <%# ((decimal)Eval("UnitCost")).ToString("N2") %></td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>

    <asp:PlaceHolder ID="phStoreNote" runat="server" Visible="false">
      <div style="margin-top:14px;padding:12px 14px;background:var(--color-bg-warm);border-radius:6px;font-size:13.5px;">
        <strong>Note from the store:</strong> <asp:Literal ID="litStoreNote" runat="server" />
      </div>
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phActions" runat="server" Visible="false">
      <div style="margin-top:18px;padding-top:16px;border-top:1px dashed var(--color-line);">
        <asp:PlaceHolder ID="phConfirmFields" runat="server" Visible="false">
          <div class="field-group">
            <asp:Label runat="server" AssociatedControlID="txtRevisedDate" Text="Delivery date you can commit to" />
            <asp:TextBox ID="txtRevisedDate" runat="server" TextMode="Date" ValidationGroup="Confirm" />
            <span class="hint">Leave as-is to accept the date the store asked for.</span>
          </div>
        </asp:PlaceHolder>

        <div class="field-group">
          <asp:Label runat="server" AssociatedControlID="txtSupplierNote" Text="Note to the store" />
          <asp:TextBox ID="txtSupplierNote" runat="server" TextMode="MultiLine" Rows="2" MaxLength="300"
                       placeholder="Anything they should know - a short-shipped line, a delay, a substitution." />
        </div>

        <asp:Button ID="btnConfirm" runat="server" Text="Confirm this order" CssClass="btn btn-primary"
                    style="width:100%;justify-content:center;padding:13px;"
                    OnClick="btnConfirm_Click" ValidationGroup="Confirm" Visible="false" />
        <asp:Button ID="btnDispatch" runat="server" Text="Mark as dispatched" CssClass="btn btn-accent"
                    style="width:100%;justify-content:center;padding:13px;"
                    OnClick="btnDispatch_Click" CausesValidation="false" Visible="false" />
      </div>
    </asp:PlaceHolder>

    <asp:Button ID="btnCloseDetail" runat="server" Text="Close" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCloseDetail_Click" CausesValidation="false" />
  </div>
</div>

</asp:Content>
