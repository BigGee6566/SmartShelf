<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Receipt.aspx.cs" Inherits="SmartShelf.Cashier.Receipt" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Receipt &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/Cashier/POS.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; Back to the till" />
  <button type="button" class="btn btn-primary btn-sm no-print" onclick="window.print();">Print</button>
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<asp:PlaceHolder ID="phNotFound" runat="server" Visible="false">
  <div class="app-card empty-state">That receipt could not be found.</div>
</asp:PlaceHolder>

<asp:PlaceHolder ID="phReceipt" runat="server">
  <div class="receipt-sheet">
    <h2>Forte Supermarket</h2>
    <div class="r-meta">
      Powered by SmartShelf<br />
      <asp:Literal ID="litReceiptNo" runat="server" /><br />
      <asp:Literal ID="litWhen" runat="server" />
    </div>

    <div class="r-rule"></div>

    <asp:Repeater ID="rptLines" runat="server">
      <ItemTemplate>
        <div class="r-line">
          <span><%# Eval("Quantity") %> &times; <%# Server.HtmlEncode((string)Eval("ProductName")) %></span>
          <span>R <%# ((decimal)Eval("LineTotal")).ToString("N2") %></span>
        </div>
        <div class="r-line" style="padding-top:0;font-size:11.5px;color:var(--color-muted);">
          <span>&nbsp;&nbsp;@ R <%# ((decimal)Eval("UnitPrice")).ToString("N2") %> each</span>
          <span><%# VatLabel(Container.DataItem) %></span>
        </div>
      </ItemTemplate>
    </asp:Repeater>

    <div class="r-rule"></div>

    <div class="r-line"><span>Subtotal</span><span><asp:Literal ID="litSubtotal" runat="server" /></span></div>
    <div class="r-line"><span>VAT</span><span><asp:Literal ID="litVat" runat="server" /></span></div>
    <div class="r-line r-total"><span>Total</span><span><asp:Literal ID="litTotal" runat="server" /></span></div>

    <div class="r-rule"></div>

    <div class="r-line"><span><asp:Literal ID="litMethod" runat="server" /></span><span><asp:Literal ID="litTendered" runat="server" /></span></div>
    <div class="r-line"><span>Change</span><span><asp:Literal ID="litChange" runat="server" /></span></div>

    <div class="r-rule"></div>

    <div class="r-meta" style="margin:0;">
      Served by <asp:Literal ID="litCashier" runat="server" /><br />
      <asp:Literal ID="litStatus" runat="server" /><br />
      Thank you for shopping with us.
    </div>
  </div>
</asp:PlaceHolder>

</asp:Content>
