<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Products.aspx.cs" Inherits="SmartShelf.Manager.Products" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Products &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/ProductEdit.aspx" CssClass="btn btn-primary btn-sm"
                 Text="+ New product" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="app-card">
  <div class="filter-bar">
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtSearch" Text="Search" />
      <asp:TextBox ID="txtSearch" runat="server" placeholder="Name or barcode" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlCategory" Text="Category" />
      <asp:DropDownList ID="ddlCategory" runat="server" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlStatus" Text="Status" />
      <asp:DropDownList ID="ddlStatus" runat="server">
        <asp:ListItem Value="active" Text="Active only" />
        <asp:ListItem Value="all" Text="Active and delisted" />
      </asp:DropDownList>
    </div>
    <asp:Button ID="btnFilter" runat="server" Text="Apply" CssClass="btn btn-ghost btn-sm" OnClick="btnFilter_Click" />
    <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-ghost btn-sm"
                OnClick="btnClear_Click" CausesValidation="false" />
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">No products matched those filters.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptProducts" runat="server" OnItemCommand="rptProducts_ItemCommand">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Product</th><th>Category</th><th>Supplier</th>
          <th class="text-right">Cost</th><th class="text-right">Price</th><th class="text-right">VAT</th>
          <th class="text-right">On hand</th><th>Status</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td>
          <div class="prod-thumb">
            <img src='<%# ImageFor(Container.DataItem) %>' alt="" />
            <span>
              <%# Server.HtmlEncode((string)Eval("Name")) %><br />
              <span class="muted" style="font-size:12px;"><%# Server.HtmlEncode((string)Eval("Barcode")) %></span>
            </span>
          </div>
        </td>
        <td><%# CategoryName(Container.DataItem) %></td>
        <td class="muted"><%# SupplierName(Container.DataItem) %></td>
        <td class="num-cell">R <%# ((decimal)Eval("CostPrice")).ToString("N2") %></td>
        <td class="num-cell"><strong>R <%# ((decimal)Eval("SellingPrice")).ToString("N2") %></strong></td>
        <td class="num-cell"><%# VatLabel(Container.DataItem) %></td>
        <td class="num-cell"><%# Eval("QuantityOnHand") %></td>
        <td>
          <span class="pill <%# RowPillCss(Container.DataItem) %>"><%# RowPillText(Container.DataItem) %></span>
        </td>
        <td class="text-right" style="white-space:nowrap;">
          <a class="btn btn-ghost btn-sm" href='<%# ResolveUrl("~/Manager/ProductEdit.aspx?id=" + Eval("ProductId")) %>'>Edit</a>
          <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="toggle"
                          CommandArgument='<%# Eval("ProductId") %>' CausesValidation="false"
                          Text='<%# (bool)Eval("IsActive") ? "Delist" : "Relist" %>'
                          OnClientClick="return confirm('Change this product’s listing status?');" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <p class="hint" style="margin-top:16px;">
    Stock levels are read-only here. Every change to a quantity goes through
    <a class="link" href="<%: ResolveUrl("~/Manager/Inventory.aspx") %>">Inventory</a> so it keeps its reason and audit trail.
  </p>
</div>

</asp:Content>
