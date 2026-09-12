<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="PurchaseOrders.aspx.cs" Inherits="SmartShelf.Manager.PurchaseOrders" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Purchase orders &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/PurchaseOrderEdit.aspx" CssClass="btn btn-primary btn-sm"
                 Text="+ Raise an order" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="app-card">
  <div class="filter-bar">
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlSupplier" Text="Supplier" />
      <asp:DropDownList ID="ddlSupplier" runat="server" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlStatus" Text="Status" />
      <asp:DropDownList ID="ddlStatus" runat="server">
        <asp:ListItem Value="" Text="All statuses" />
        <asp:ListItem Value="Draft" Text="Draft" />
        <asp:ListItem Value="Sent" Text="Sent" />
        <asp:ListItem Value="Confirmed" Text="Confirmed" />
        <asp:ListItem Value="Dispatched" Text="Dispatched" />
        <asp:ListItem Value="PartiallyDelivered" Text="Part delivered" />
        <asp:ListItem Value="Delivered" Text="Delivered" />
        <asp:ListItem Value="Cancelled" Text="Cancelled" />
      </asp:DropDownList>
    </div>
    <asp:Button ID="btnFilter" runat="server" Text="Apply" CssClass="btn btn-ghost btn-sm" OnClick="btnFilter_Click" />
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">No purchase orders matched those filters.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptOrders" runat="server" OnItemCommand="rptOrders_ItemCommand">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Order</th><th>Supplier</th><th>Raised</th><th>Expected</th>
          <th class="text-right">Lines</th><th class="text-right">Value</th>
          <th>Status</th><th>Progress</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("OrderNumber")) %></strong></td>
        <td><%# SupplierName(Container.DataItem) %></td>
        <td style="white-space:nowrap;"><%# ((DateTime)Eval("CreatedAt")).ToString("dd MMM yyyy") %></td>
        <td style="white-space:nowrap;"><%# ExpectedCell(Container.DataItem) %></td>
        <td class="num-cell"><%# LineCount(Container.DataItem) %></td>
        <td class="num-cell">R <%# OrderValue(Container.DataItem) %></td>
        <td><span class="pill <%# StatusCss(Container.DataItem) %>"><%# StatusLabel(Container.DataItem) %></span></td>
        <td class="muted"><%# Progress(Container.DataItem) %></td>
        <td class="text-right" style="white-space:nowrap;">
          <a class="btn btn-ghost btn-sm" href='<%# ResolveUrl("~/Manager/PurchaseOrderEdit.aspx?id=" + Eval("PurchaseOrderId")) %>'>Open</a>
          <asp:LinkButton runat="server" CssClass="btn btn-primary btn-sm" CommandName="send"
                          CommandArgument='<%# Eval("PurchaseOrderId") %>' Text="Send"
                          Visible='<%# IsDraft(Container.DataItem) %>' CausesValidation="false"
                          OnClientClick="return confirm('Send this order to the supplier?');" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>
</div>

</asp:Content>
