<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Users.aspx.cs" Inherits="SmartShelf.Admin.Users" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">User accounts &mdash; SmartShelf</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="dash-grid">
  <div class="app-card">
    <div class="card-head"><h4>Staff and supplier accounts</h4></div>

    <asp:Repeater ID="rptUsers" runat="server" OnItemCommand="rptUsers_ItemCommand">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Name</th><th>Username</th><th>Role</th><th>Last signed in</th><th>Status</th><th></th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td>
            <strong><%# Server.HtmlEncode((string)Eval("FullName")) %></strong><br />
            <span class="muted" style="font-size:12px;"><%# Server.HtmlEncode(Convert.ToString(Eval("Email"))) %></span>
          </td>
          <td style="font-family:ui-monospace,Menlo,monospace;font-size:13px;"><%# Server.HtmlEncode((string)Eval("Username")) %></td>
          <td><%# RoleCell(Container.DataItem) %></td>
          <td class="muted"><%# LastLogin(Container.DataItem) %></td>
          <td><span class="pill <%# (bool)Eval("IsActive") ? "pill-green" : "pill-grey" %>"><%# (bool)Eval("IsActive") ? "Active" : "Disabled" %></span></td>
          <td class="text-right" style="white-space:nowrap;">
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="edit"
                            CommandArgument='<%# Eval("UserId") %>' Text="Edit" CausesValidation="false" />
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="toggle"
                            CommandArgument='<%# Eval("UserId") %>'
                            Text='<%# (bool)Eval("IsActive") ? "Disable" : "Enable" %>' CausesValidation="false"
                            OnClientClick="return confirm('Change this account’s status?');" />
          </td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>

    <p class="hint" style="margin-top:16px;">
      Accounts are disabled rather than deleted. Sales, stock adjustments and cash-ups all point at
      the person who made them, and that history has to stay readable.
    </p>
  </div>

  <div class="app-card">
    <h4 style="font-size:16px;margin-bottom:16px;"><asp:Literal ID="litFormTitle" runat="server" /></h4>
    <asp:HiddenField ID="hfUserId" runat="server" Value="0" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtFullName" Text="Full name *" />
      <asp:TextBox ID="txtFullName" runat="server" MaxLength="100" ValidationGroup="User" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFullName" Display="Dynamic"
           CssClass="field-error" ValidationGroup="User" ErrorMessage="A full name is required." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtUsername" Text="Username *" />
      <asp:TextBox ID="txtUsername" runat="server" MaxLength="50" ValidationGroup="User" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtUsername" Display="Dynamic"
           CssClass="field-error" ValidationGroup="User" ErrorMessage="A username is required." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" />
      <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" ValidationGroup="User" />
      <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" Display="Dynamic"
           CssClass="field-error" ValidationGroup="User"
           ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Enter a valid email address." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlRole" Text="Role *" />
      <asp:DropDownList ID="ddlRole" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlRole_Changed" />
    </div>

    <asp:PlaceHolder ID="phSupplier" runat="server" Visible="false">
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlSupplier" Text="Supplier company *" />
        <asp:DropDownList ID="ddlSupplier" runat="server" />
        <span class="hint">A supplier login only ever sees orders belonging to this company.</span>
      </div>
    </asp:PlaceHolder>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtPassword" Text="Password" />
      <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" MaxLength="128" autocomplete="new-password" />
      <span class="hint"><asp:Literal ID="litPasswordHint" runat="server" /></span>
    </div>

    <div class="field-group">
      <label style="display:flex;align-items:center;gap:8px;font-size:13.5px;">
        <asp:CheckBox ID="chkActive" runat="server" Checked="true" /> Account is active
      </label>
    </div>

    <div class="form-actions">
      <asp:Button ID="btnSave" runat="server" Text="Save account" CssClass="btn btn-primary"
                  ValidationGroup="User" OnClick="btnSave_Click" />
      <asp:Button ID="btnCancel" runat="server" Text="Clear" CssClass="btn btn-ghost"
                  OnClick="btnCancel_Click" CausesValidation="false" />
    </div>

    <p class="hint" style="margin-top:14px;">
      Passwords are hashed with PBKDF2 before they are stored. Nobody, including you, can read an
      existing password back &mdash; you can only set a new one.
    </p>
  </div>
</div>

</asp:Content>
