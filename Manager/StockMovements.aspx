<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="StockMovements.aspx.cs" Inherits="SmartShelf.Manager.StockMovements" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Stock movements &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/Inventory.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; Inventory" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="app-card">
  <div class="card-head">
    <h4>Every change to stock, and who made it</h4>
  </div>

  <div class="filter-bar">
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlProduct" Text="Product" />
      <asp:DropDownList ID="ddlProduct" runat="server" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlType" Text="Movement type" />
      <asp:DropDownList ID="ddlType" runat="server">
        <asp:ListItem Value="" Text="All types" />
        <asp:ListItem Value="Sale" Text="Sales" />
        <asp:ListItem Value="Adjustment" Text="Manual adjustments" />
        <asp:ListItem Value="Receipt" Text="Deliveries received" />
        <asp:ListItem Value="Waste" Text="Waste write-offs" />
        <asp:ListItem Value="VoidReversal" Text="Voided sales" />
      </asp:DropDownList>
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtFrom" Text="From" />
      <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtTo" Text="To" />
      <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
    </div>
    <asp:Button ID="btnFilter" runat="server" Text="Apply" CssClass="btn btn-ghost btn-sm" OnClick="btnFilter_Click" />
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">No movements matched those filters.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptMovements" runat="server">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>When</th><th>Product</th><th>Type</th>
          <th class="text-right">Change</th><th class="text-right">On hand after</th>
          <th>Reason</th><th>Who</th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td style="white-space:nowrap;"><%# ((DateTime)Eval("CreatedAt")).ToString("dd MMM yyyy, HH:mm") %></td>
        <td><%# ProductName(Container.DataItem) %></td>
        <td><span class="pill <%# TypeCss(Container.DataItem) %>"><%# TypeLabel(Container.DataItem) %></span></td>
        <td class="num-cell" style="<%# DeltaStyle(Container.DataItem) %>"><%# DeltaText(Container.DataItem) %></td>
        <td class="num-cell"><%# Eval("QuantityAfter") %></td>
        <td class="muted"><%# Server.HtmlEncode(Convert.ToString(Eval("Reason"))) %></td>
        <td><%# UserName(Container.DataItem) %></td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <p class="hint" style="margin-top:16px;">
    Showing the most recent <asp:Literal ID="litCount" runat="server" /> movements. Every row here was
    written in the same transaction as the stock change it explains, so this log cannot drift from the shelf.
  </p>
</div>

</asp:Content>
