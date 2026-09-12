<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Dashboard.aspx.cs" Inherits="SmartShelf.Manager.Dashboard" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Store Dashboard &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/Reports.aspx" CssClass="btn btn-ghost btn-sm" Text="Reports" />
  <asp:HyperLink runat="server" NavigateUrl="~/Cashier/POS.aspx" CssClass="btn btn-primary btn-sm" Text="Open POS Terminal &rarr;" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<div class="hero-photo-card">
  <img src="https://images.pexels.com/photos/4484149/pexels-photo-4484149.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1400" alt="Store manager checking inventory on a tablet" />
  <div class="hpc-overlay">
    <h3><asp:Literal ID="litGreeting" runat="server" /></h3>
    <p><asp:Literal ID="litBriefing" runat="server" /></p>
  </div>
</div>

<div class="stat-card-row">
  <div class="stat-card">
    <div class="sc-lbl">Today&#39;s revenue</div>
    <div class="sc-num"><asp:Literal ID="litRevenue" runat="server" /></div>
    <div class="sc-delta <%= RevenueDeltaCss %>"><asp:Literal ID="litRevenueDelta" runat="server" /></div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Products tracked</div>
    <div class="sc-num"><asp:Literal ID="litProducts" runat="server" /></div>
    <div class="sc-delta muted"><asp:Literal ID="litCategories" runat="server" /></div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Low stock alerts</div>
    <div class="sc-num" style="<%= LowStockStyle %>"><asp:Literal ID="litLowStock" runat="server" /></div>
    <div class="sc-delta <%= LowStockDeltaCss %>"><asp:Literal ID="litLowStockHint" runat="server" /></div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Last cash-up variance</div>
    <div class="sc-num" style="<%= VarianceStyle %>"><asp:Literal ID="litVariance" runat="server" /></div>
    <div class="sc-delta <%= VarianceDeltaCss %>"><asp:Literal ID="litVarianceHint" runat="server" /></div>
  </div>
</div>

<div class="dash-grid">

  <%-- Left column: what needs attention on the shelf --%>
  <div class="card-stack">
    <div class="app-card">
      <div class="card-head">
        <h4>Stock needing attention</h4>
        <asp:PlaceHolder ID="phLowTag" runat="server"><span class="tag">Needs attention</span></asp:PlaceHolder>
      </div>

      <asp:PlaceHolder ID="phNoLowStock" runat="server" Visible="false">
        <div class="empty-state">Every product is above its reorder level. Nothing to order today.</div>
      </asp:PlaceHolder>

      <asp:Repeater ID="rptLowStock" runat="server">
        <HeaderTemplate>
          <table class="data-table">
            <thead><tr><th>Product</th><th class="text-right">On hand</th><th class="text-right">Reorder at</th><th>Status</th><th></th></tr></thead>
            <tbody>
        </HeaderTemplate>
        <ItemTemplate>
          <tr>
            <td>
              <div class="prod-thumb">
                <img src='<%# ImageFor(Container.DataItem) %>' alt="" />
                <span><%# Server.HtmlEncode((string)Eval("Name")) %></span>
              </div>
            </td>
            <td class="num-cell"><%# Eval("QuantityOnHand") %></td>
            <td class="num-cell"><%# Eval("ReorderLevel") %></td>
            <td><span class="pill <%# PillCss(Container.DataItem) %>"><%# PillText(Container.DataItem) %></span></td>
            <td class="text-right">
              <a class="btn btn-ghost btn-sm"
                 href='<%# ResolveUrl("~/Manager/Inventory.aspx?productId=" + Eval("ProductId")) %>'>Adjust</a>
            </td>
          </tr>
        </ItemTemplate>
        <FooterTemplate></tbody></table></FooterTemplate>
      </asp:Repeater>
    </div>

    <div class="app-card">
      <div class="card-head">
        <h4>Expiring in the next 7 days</h4>
        <asp:HyperLink runat="server" NavigateUrl="~/Manager/Reports.aspx?tab=expiry"
                       CssClass="btn btn-ghost btn-sm" Text="Full expiry report &rarr;" />
      </div>

      <asp:PlaceHolder ID="phNoExpiry" runat="server" Visible="false">
        <div class="empty-state">Nothing on the shelf is close to its expiry date.</div>
      </asp:PlaceHolder>

      <asp:Repeater ID="rptExpiring" runat="server">
        <HeaderTemplate>
          <table class="data-table">
            <thead><tr><th>Product</th><th>Batch</th><th class="text-right">Units</th><th>Expires</th><th class="text-right">Value</th></tr></thead>
            <tbody>
        </HeaderTemplate>
        <ItemTemplate>
          <tr>
            <td><%# Server.HtmlEncode((string)Eval("ProductName")) %></td>
            <td class="muted"><%# Server.HtmlEncode((string)Eval("BatchCode")) %></td>
            <td class="num-cell"><%# Eval("QuantityRemaining") %></td>
            <td><span class="pill <%# ExpiryPillCss(Container.DataItem) %>"><%# ExpiryText(Container.DataItem) %></span></td>
            <td class="num-cell">R <%# ((decimal)Eval("ValueAtRisk")).ToString("N2") %></td>
          </tr>
        </ItemTemplate>
        <FooterTemplate></tbody></table></FooterTemplate>
      </asp:Repeater>
    </div>
  </div>

  <%-- Right column: money and supply --%>
  <div class="card-stack">
    <div class="app-card">
      <div class="card-head">
        <h4 style="font-size:16px;">Top sellers, last 30 days</h4>
      </div>
      <asp:Repeater ID="rptTopSellers" runat="server">
        <ItemTemplate>
          <div class="bar-row">
            <span class="bl-name"><%# Server.HtmlEncode(Shorten((string)Eval("ProductName"))) %></span>
            <div class="bar-track"><div class="bar-fill" style="width:<%# BarWidth(Container.DataItem) %>%;"></div></div>
            <span class="bl-val">R <%# ((decimal)Eval("Revenue")).ToString("N0") %></span>
          </div>
        </ItemTemplate>
      </asp:Repeater>
      <asp:PlaceHolder ID="phNoSales" runat="server" Visible="false">
        <div class="empty-state">No sales in the last 30 days yet.</div>
      </asp:PlaceHolder>
    </div>

    <div class="app-card">
      <div class="card-head">
        <h4 style="font-size:16px;">Cash-up queue</h4>
        <asp:HyperLink runat="server" NavigateUrl="~/Manager/CashUpReview.aspx"
                       CssClass="btn btn-ghost btn-sm" Text="Review &rarr;" />
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:8px;">
        <span class="muted">Awaiting review</span><span style="font-weight:600;"><asp:Literal ID="litQueueCount" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:8px;">
        <span class="muted">Last shift counted</span><span style="font-weight:600;"><asp:Literal ID="litLastCashier" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;padding-top:8px;border-top:1px dashed var(--color-line);">
        <span class="muted">Variance</span><span style="font-weight:700;<%= VarianceStyle %>"><asp:Literal ID="litQueueVariance" runat="server" /></span>
      </div>
    </div>

    <div class="app-card">
      <div class="card-head">
        <h4 style="font-size:16px;">Purchase orders</h4>
        <asp:HyperLink runat="server" NavigateUrl="~/Manager/PurchaseOrders.aspx"
                       CssClass="btn btn-ghost btn-sm" Text="Manage &rarr;" />
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:8px;">
        <span class="muted">Still open</span><span style="font-weight:600;"><asp:Literal ID="litOpenPos" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;">
        <span class="muted">Active suppliers</span><span style="font-weight:600;"><asp:Literal ID="litSuppliers" runat="server" /></span>
      </div>
    </div>
  </div>
</div>

</asp:Content>
