<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="MySales.aspx.cs" Inherits="SmartShelf.Cashier.MySales" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">My Sales &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" ID="lnkPos" NavigateUrl="~/Cashier/POS.aspx" CssClass="btn btn-primary btn-sm"
                 Text="Open POS Terminal &rarr;" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="stat-card-row">
  <div class="stat-card">
    <div class="sc-lbl">Sales this shift</div>
    <div class="sc-num"><asp:Literal ID="litShiftCount" runat="server" /></div>
    <div class="sc-delta up"><asp:Literal ID="litShiftTill" runat="server" /></div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Taken this shift</div>
    <div class="sc-num"><asp:Literal ID="litShiftTotal" runat="server" /></div>
    <div class="sc-delta muted">All payment methods</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Cash in the drawer</div>
    <div class="sc-num"><asp:Literal ID="litExpectedCash" runat="server" /></div>
    <div class="sc-delta muted">Float plus cash sales</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Shift opened</div>
    <div class="sc-num" style="font-size:22px;"><asp:Literal ID="litShiftOpened" runat="server" /></div>
    <div class="sc-delta muted"><asp:Literal ID="litShiftState" runat="server" /></div>
  </div>
</div>

<div class="app-card">
  <div class="card-head">
    <h4>Recent sales</h4>
    <asp:HyperLink runat="server" NavigateUrl="~/Cashier/CashUp.aspx" CssClass="btn btn-ghost btn-sm"
                   Text="Cash up this shift &rarr;" />
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">You haven&#39;t rung up any sales yet.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptSales" runat="server">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Receipt</th><th>When</th><th>Items</th><th>Payment</th>
          <th class="text-right">Total</th><th>Status</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("ReceiptNumber")) %></strong></td>
        <td><%# ((DateTime)Eval("SaleDate")).ToString("dd MMM, HH:mm") %></td>
        <td><%# ItemCount(Container.DataItem) %></td>
        <td><%# Eval("PaymentMethod") %></td>
        <td class="num-cell">R <%# ((decimal)Eval("Total")).ToString("N2") %></td>
        <td><span class="pill <%# StatusCss(Container.DataItem) %>"><%# Eval("Status") %></span></td>
        <td class="text-right">
          <a class="btn btn-ghost btn-sm" target="_blank"
             href='<%# ResolveUrl("~/Cashier/Receipt.aspx?id=" + Eval("SaleId")) %>'>Receipt</a>
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>
</div>

</asp:Content>
