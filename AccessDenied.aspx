<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AccessDenied.aspx.cs" Inherits="SmartShelf.AccessDenied" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Access denied &mdash; SmartShelf</title>
    <link rel="stylesheet" href="<%: ResolveUrl("~/Styles/site.css") %>" />
</head>
<body>
<form id="denyForm" runat="server">
  <section style="min-height:100vh;display:flex;align-items:center;">
    <div class="wrap" style="max-width:640px;">
      <span class="eyebrow">Access denied</span>
      <h1 style="font-size:clamp(30px,5vw,46px);">That screen isn&#39;t part of your role.</h1>
      <p class="muted" style="margin-top:18px;font-size:17px;">
        You&#39;re signed in as <strong><asp:Literal ID="litWho" runat="server" /></strong>.
        SmartShelf keeps each role to its own screens, so this page is off-limits from here.
        If you need it, ask an administrator to change your role.
      </p>
      <div class="hero-actions">
        <asp:HyperLink ID="lnkHome" runat="server" CssClass="btn btn-primary" Text="Back to my workspace" />
        <asp:LinkButton ID="btnSignOut" runat="server" OnClick="btnSignOut_Click" CssClass="btn btn-ghost"
                        Text="Sign in as someone else" CausesValidation="false" />
      </div>
    </div>
  </section>
</form>
</body>
</html>
