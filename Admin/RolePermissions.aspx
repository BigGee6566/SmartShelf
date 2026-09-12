<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="RolePermissions.aspx.cs" Inherits="SmartShelf.Admin.RolePermissions" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Role permissions &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Admin/Users.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; User accounts" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<div class="app-card">
  <div class="card-head">
    <div>
      <h4>What each role can reach</h4>
      <p class="muted" style="font-size:13.5px;margin-top:4px;max-width:680px;">
        Permissions in SmartShelf are per role, not per person &mdash; you grant access by giving
        someone a role on the <a class="link" href="<%: ResolveUrl("~/Admin/Users.aspx") %>">accounts</a> page.
        This matrix is generated from the rules the application actually enforces.
      </p>
    </div>
  </div>

  <asp:Repeater ID="rptMatrix" runat="server">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Screen</th><th>What it does</th>
          <th style="text-align:center;">Cashier</th><th style="text-align:center;">Store Manager</th>
          <th style="text-align:center;">Supplier</th><th style="text-align:center;">Admin</th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td><strong><%# Server.HtmlEncode((string)Eval("Screen")) %></strong></td>
        <td class="muted"><%# Server.HtmlEncode((string)Eval("Purpose")) %></td>
        <td style="text-align:center;"><%# Tick((bool)Eval("Cashier")) %></td>
        <td style="text-align:center;"><%# Tick((bool)Eval("Manager")) %></td>
        <td style="text-align:center;"><%# Tick((bool)Eval("Supplier")) %></td>
        <td style="text-align:center;"><%# Tick((bool)Eval("Admin")) %></td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>
</div>

<div class="app-card" style="margin-top:22px;">
  <h4 style="font-size:16px;margin-bottom:14px;">How access is enforced</h4>
  <ul class="feature-list">
    <li>Each folder is gated by an authorization rule in <code>Web.config</code>, so an unauthorised request never reaches the page.</li>
    <li>Every page behind the login also inherits a server-side role check, so a mis-edited config cannot quietly expose a screen.</li>
    <li>Supplier accounts are scoped further: the service layer refuses any action on an order belonging to a different company.</li>
    <li>A cashier can reprint only their own receipts; a manager can reprint any of them.</li>
    <li>Sign-in issues an encrypted Forms ticket carrying the role. The browser cannot alter it without invalidating it.</li>
  </ul>
</div>

</asp:Content>
