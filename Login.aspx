<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="SmartShelf.Login" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Sign in &mdash; SmartShelf</title>
    <link rel="stylesheet" href="<%: ResolveUrl("~/Styles/site.css") %>" />
</head>
<body>
<form id="loginForm" runat="server" defaultbutton="btnLogin" defaultfocus="txtUsername">

<div class="login-screen">
  <div class="login-visual">
    <img src="https://images.pexels.com/photos/28907758/pexels-photo-28907758.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1400" alt="Modern supermarket produce aisle" />
    <div class="overlay">
      <a href="<%: ResolveUrl("~/Default.aspx") %>" class="brand" style="color:#fff;margin-bottom:auto;"><span class="dot"></span>SmartShelf</a>
      <blockquote>&ldquo;Stock, till and suppliers finally agreeing with each other.&rdquo;</blockquote>
      <cite>One entry point for every role &mdash; SmartShelf takes you to your own workspace.</cite>
    </div>
  </div>

  <div class="login-form-side">
    <div class="login-form">
      <span class="eyebrow">Welcome back</span>
      <h1 style="font-size:32px;margin-bottom:8px;">Sign in to SmartShelf</h1>
      <p class="muted" style="font-size:14.5px;margin-bottom:28px;">Your role decides where you land &mdash; the till, the dashboard, the supplier portal or the admin console.</p>

      <asp:PlaceHolder ID="phError" runat="server" Visible="false">
        <div class="login-error"><asp:Literal ID="litError" runat="server" /></div>
      </asp:PlaceHolder>

      <div class="field-group">
        <asp:Label ID="lblUser" runat="server" AssociatedControlID="txtUsername" Text="Username" />
        <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" autocomplete="username" placeholder="a.booi" />
        <asp:RequiredFieldValidator ID="rfvUsername" runat="server" ControlToValidate="txtUsername"
            Display="Dynamic" CssClass="field-error" ErrorMessage="Enter your username." ValidationGroup="Login" />
      </div>

      <div class="field-group">
        <asp:Label ID="lblPass" runat="server" AssociatedControlID="txtPassword" Text="Password" />
        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="current-password" />
        <asp:RequiredFieldValidator ID="rfvPassword" runat="server" ControlToValidate="txtPassword"
            Display="Dynamic" CssClass="field-error" ErrorMessage="Enter your password." ValidationGroup="Login" />
      </div>

      <label style="display:flex;align-items:center;gap:8px;font-size:13px;margin-bottom:18px;color:var(--color-muted);">
        <asp:CheckBox ID="chkRemember" runat="server" /> Keep me signed in on this device
      </label>

      <asp:Button ID="btnLogin" runat="server" Text="Login &rarr;" OnClick="btnLogin_Click"
                  ValidationGroup="Login" CssClass="btn btn-primary"
                  style="width:100%;justify-content:center;padding:14px;" />

      <div style="display:flex;justify-content:space-between;margin-top:18px;font-size:13px;">
        <span class="muted">Forgotten your password? Ask an administrator to reset it.</span>
        <a href="<%: ResolveUrl("~/Default.aspx") %>" class="muted">&larr; Back to site</a>
      </div>

      <%-- Coursework build: the demo accounts are published so a marker can sign in as
           every role. Passwords are still stored only as PBKDF2 hashes. --%>
      <div class="demo-creds">
        <strong>Demo accounts</strong>
        <table>
          <tr><td>Cashier</td><td>a.booi / Cashier@123</td></tr>
          <tr><td>Store Manager</td><td>a.right / Manager@123</td></tr>
          <tr><td>Supplier</td><td>s.mkhize / Supplier@123</td></tr>
          <tr><td>Admin</td><td>admin / Admin@123</td></tr>
        </table>
      </div>
    </div>
  </div>
</div>

</form>
</body>
</html>
