<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Reports.aspx.cs" Inherits="SmartShelf.Manager.Reports" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Reports &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:Button ID="btnExport" runat="server" Text="Export this report to CSV" CssClass="btn btn-ghost btn-sm"
              OnClick="btnExport_Click" CausesValidation="false" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="tab-row">
  <asp:Repeater ID="rptTabs" runat="server">
    <ItemTemplate>
      <a href='<%# ResolveUrl("~/Manager/Reports.aspx?tab=" + Eval("Key")) %>'
         class='<%# (bool)Eval("IsActive") ? "active" : "" %>'><%# Server.HtmlEncode((string)Eval("Label")) %></a>
    </ItemTemplate>
  </asp:Repeater>
</div>

<div class="app-card">
  <div class="card-head">
    <div>
      <h4><asp:Literal ID="litReportTitle" runat="server" /></h4>
      <p class="muted" style="font-size:13.5px;margin-top:4px;max-width:640px;"><asp:Literal ID="litReportBlurb" runat="server" /></p>
    </div>
  </div>

  <%-- Date filters only matter to the reports that cover a period. --%>
  <asp:PlaceHolder ID="phDateFilter" runat="server">
    <div class="filter-bar">
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtFrom" Text="From" />
        <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
      </div>
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtTo" Text="To" />
        <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
      </div>
      <asp:Button ID="btnRun" runat="server" Text="Run report" CssClass="btn btn-ghost btn-sm" OnClick="btnRun_Click" />
      <asp:LinkButton ID="btnLast7" runat="server" CssClass="btn btn-ghost btn-sm" Text="Last 7 days"
                      CommandArgument="7" OnCommand="QuickRange" CausesValidation="false" />
      <asp:LinkButton ID="btnLast30" runat="server" CssClass="btn btn-ghost btn-sm" Text="Last 30 days"
                      CommandArgument="30" OnCommand="QuickRange" CausesValidation="false" />
      <asp:LinkButton ID="btnLast90" runat="server" CssClass="btn btn-ghost btn-sm" Text="Last 90 days"
                      CommandArgument="90" OnCommand="QuickRange" CausesValidation="false" />
    </div>
  </asp:PlaceHolder>

  <asp:PlaceHolder ID="phExpiryFilter" runat="server" Visible="false">
    <div class="filter-bar">
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlHorizon" Text="Show stock expiring within" />
        <asp:DropDownList ID="ddlHorizon" runat="server" AutoPostBack="true" OnSelectedIndexChanged="btnRun_Click">
          <asp:ListItem Value="3" Text="3 days" />
          <asp:ListItem Value="7" Text="7 days" />
          <asp:ListItem Value="14" Text="14 days" Selected="True" />
          <asp:ListItem Value="30" Text="30 days" />
          <asp:ListItem Value="90" Text="90 days" />
        </asp:DropDownList>
      </div>
    </div>
  </asp:PlaceHolder>

  <asp:PlaceHolder ID="phEmpty" runat="server" Visible="false">
    <div class="empty-state"><asp:Literal ID="litEmpty" runat="server" /></div>
  </asp:PlaceHolder>

  <%-- ============ 1. Top sellers ============ --%>
  <asp:Repeater ID="rptTopSellers" runat="server" Visible="false">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr><th>#</th><th>Product</th><th>Category</th>
          <th class="text-right">Units sold</th><th class="text-right">Revenue</th>
          <th class="text-right">Gross profit</th><th style="width:130px;">Share</th></tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td class="muted"><%# Container.ItemIndex + 1 %></td>
        <td><strong><%# Server.HtmlEncode((string)Eval("ProductName")) %></strong></td>
        <td class="muted"><%# Server.HtmlEncode((string)Eval("CategoryName")) %></td>
        <td class="num-cell"><%# ((int)Eval("UnitsSold")).ToString("N0") %></td>
        <td class="num-cell"><strong>R <%# ((decimal)Eval("Revenue")).ToString("N2") %></strong></td>
        <td class="num-cell"><%# ProfitCell(Container.DataItem) %></td>
        <td><div class="bar-track"><div class="bar-fill" style="width:<%# ShareWidth(Container.DataItem) %>%;"></div></div></td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <%-- ============ 2. Low stock ============ --%>
  <asp:Repeater ID="rptLowStock" runat="server" Visible="false">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr><th>Product</th><th>Category</th><th>Preferred supplier</th>
          <th class="text-right">On hand</th><th class="text-right">Reorder at</th>
          <th class="text-right">Suggest ordering</th><th class="text-right">Est. cost</th><th></th></tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("ProductName")) %></strong></td>
        <td class="muted"><%# Server.HtmlEncode((string)Eval("CategoryName")) %></td>
        <td class="muted"><%# Server.HtmlEncode((string)Eval("SupplierName")) %></td>
        <td class="num-cell" style="color:var(--color-accent-dark);font-weight:700;"><%# Eval("QuantityOnHand") %></td>
        <td class="num-cell"><%# Eval("ReorderLevel") %></td>
        <td class="num-cell"><strong><%# Eval("SuggestedOrderQty") %></strong></td>
        <td class="num-cell">R <%# ((decimal)Eval("EstimatedCost")).ToString("N2") %></td>
        <td class="text-right"><%# OrderLink(Container.DataItem) %></td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <%-- ============ 3. Expiry and waste ============ --%>
  <asp:PlaceHolder ID="phExpiry" runat="server" Visible="false">
    <asp:Repeater ID="rptExpiry" runat="server">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Product</th><th>Batch</th><th class="text-right">Units left</th>
            <th>Expiry date</th><th>Status</th><th class="text-right">Value at risk</th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td><strong><%# Server.HtmlEncode((string)Eval("ProductName")) %></strong></td>
          <td class="muted"><%# Server.HtmlEncode(Convert.ToString(Eval("BatchCode"))) %></td>
          <td class="num-cell"><%# Eval("QuantityRemaining") %></td>
          <td><%# ExpiryDateText(Container.DataItem) %></td>
          <td><span class="pill <%# ExpiryPillCss(Container.DataItem) %>"><%# ExpiryPillText(Container.DataItem) %></span></td>
          <td class="num-cell">R <%# ((decimal)Eval("ValueAtRisk")).ToString("N2") %></td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>

    <h4 style="font-size:16px;margin:28px 0 6px;">Waste already written off</h4>
    <p class="muted" style="font-size:13.5px;margin-bottom:14px;">
      Stock removed from the shelf in the selected period, valued at cost.
    </p>

    <asp:PlaceHolder ID="phNoWaste" runat="server" Visible="false">
      <div class="empty-state">Nothing has been written off in this period.</div>
    </asp:PlaceHolder>

    <asp:Repeater ID="rptWaste" runat="server">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Product</th><th class="text-right">Units wasted</th>
            <th class="text-right">Write-offs</th><th class="text-right">Cost of waste</th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td><%# Server.HtmlEncode((string)Eval("ProductName")) %></td>
          <td class="num-cell"><%# Eval("UnitsWasted") %></td>
          <td class="num-cell"><%# Eval("Incidents") %></td>
          <td class="num-cell" style="color:var(--color-accent-dark);font-weight:600;">R <%# ((decimal)Eval("CostOfWaste")).ToString("N2") %></td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>
  </asp:PlaceHolder>

  <%-- ============ 4. Supplier performance ============ --%>
  <asp:Repeater ID="rptSuppliers" runat="server" Visible="false">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr><th>Supplier</th><th class="text-right">Orders</th><th class="text-right">Delivered</th>
          <th class="text-right">On time</th><th class="text-right">Fill rate</th>
          <th class="text-right">Avg. confirm</th><th class="text-right">Order value</th></tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("SupplierName")) %></strong></td>
        <td class="num-cell"><%# Eval("OrdersPlaced") %></td>
        <td class="num-cell"><%# Eval("OrdersDelivered") %></td>
        <td class="num-cell"><%# RateCell((double)Eval("OnTimeRate"), (int)Eval("OrdersDelivered")) %></td>
        <td class="num-cell"><%# RateCell((double)Eval("FillRate"), (int)Eval("UnitsOrdered")) %></td>
        <td class="num-cell muted"><%# ConfirmCell(Container.DataItem) %></td>
        <td class="num-cell">R <%# ((decimal)Eval("OrderValue")).ToString("N2") %></td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <p class="hint" style="margin-top:18px;">
    Every figure on this page is aggregated from the sales, stock movements, batches and purchase
    orders the system recorded &mdash; nothing here is stored or pre-calculated, so a sale rung up a
    minute ago is already included.
  </p>
</div>

</asp:Content>
