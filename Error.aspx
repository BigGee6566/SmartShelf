<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="SmartShelf.ErrorPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Something went wrong &mdash; SmartShelf</title>
    <link rel="stylesheet" href="<%: ResolveUrl("~/Styles/site.css") %>" />
</head>
<body>
<form id="errForm" runat="server">
  <section style="min-height:100vh;display:flex;align-items:center;">
    <div class="wrap" style="max-width:640px;">
      <span class="eyebrow"><asp:Literal ID="litCode" runat="server" /></span>
      <h1 style="font-size:clamp(30px,5vw,46px);"><asp:Literal ID="litHeading" runat="server" /></h1>
      <p class="muted" style="margin-top:18px;font-size:17px;"><asp:Literal ID="litBody" runat="server" /></p>
      <div class="hero-actions">
        <asp:HyperLink ID="lnkHome" runat="server" CssClass="btn btn-primary" Text="Back to my workspace" />
        <asp:HyperLink ID="lnkSite" runat="server" NavigateUrl="~/Default.aspx" CssClass="btn btn-ghost" Text="Go to the home page" />
      </div>
    </div>
  </section>
</form>
</body>
</html>
