<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Suppliers.aspx.cs" Inherits="SmartShelf.Manager.Suppliers" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Suppliers &mdash; SmartShelf</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="dash-grid">
  <div class="app-card">
    <div class="card-head"><h4>Suppliers</h4></div>

    <asp:Repeater ID="rptSuppliers" runat="server" OnItemCommand="rptSuppliers_ItemCommand">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Supplier</th><th>Contact</th><th class="text-right">Lead time</th><th class="text-right">Products</th><th>Status</th><th></th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td>
            <strong><%# Server.HtmlEncode((string)Eval("Name")) %></strong><br />
            <span class="muted" style="font-size:12px;"><%# Server.HtmlEncode(Convert.ToString(Eval("Email"))) %></span>
          </td>
          <td>
            <%# Server.HtmlEncode(Convert.ToString(Eval("ContactPerson"))) %><br />
            <span class="muted" style="font-size:12px;"><%# Server.HtmlEncode(Convert.ToString(Eval("Phone"))) %></span>
          </td>
          <td class="num-cell"><%# Eval("LeadTimeDays") %> d</td>
          <td class="num-cell"><%# ProductCount((int)Eval("SupplierId")) %></td>
          <td><span class="pill <%# (bool)Eval("IsActive") ? "pill-green" : "pill-grey" %>"><%# (bool)Eval("IsActive") ? "Active" : "Inactive" %></span></td>
          <td class="text-right">
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="edit"
                            CommandArgument='<%# Eval("SupplierId") %>' Text="Edit" CausesValidation="false" />
          </td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>

    <p class="hint" style="margin-top:16px;">
      A supplier&#39;s lead time sets the expected delivery date on every order raised for them,
      which is what the supplier performance report measures against.
    </p>
  </div>

  <div class="app-card">
    <h4 style="font-size:16px;margin-bottom:16px;"><asp:Literal ID="litFormTitle" runat="server" /></h4>
    <asp:HiddenField ID="hfSupplierId" runat="server" Value="0" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtName" Text="Company name *" />
      <asp:TextBox ID="txtName" runat="server" MaxLength="100" ValidationGroup="Supplier" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Supplier" ErrorMessage="A company name is required." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtContact" Text="Contact person" />
      <asp:TextBox ID="txtContact" runat="server" MaxLength="100" />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" />
      <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" ValidationGroup="Supplier" />
      <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Supplier"
           ValidationExpression="^[^@\s]+@[^@\s]+\.[^@\s]+$" ErrorMessage="Enter a valid email address." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtPhone" Text="Phone" />
      <asp:TextBox ID="txtPhone" runat="server" MaxLength="30" />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtLeadTime" Text="Lead time (days) *" />
      <asp:TextBox ID="txtLeadTime" runat="server" TextMode="Number" min="0" step="1" ValidationGroup="Supplier" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtLeadTime" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Supplier" ErrorMessage="Enter a lead time." />
      <asp:RangeValidator runat="server" ControlToValidate="txtLeadTime" MinimumValue="0" MaximumValue="365"
           Type="Integer" Display="Dynamic" CssClass="field-error" ValidationGroup="Supplier"
           ErrorMessage="Enter a whole number of days between 0 and 365." />
    </div>

    <div class="field-group">
      <label style="display:flex;align-items:center;gap:8px;font-size:13.5px;">
        <asp:CheckBox ID="chkActive" runat="server" Checked="true" /> Accepting new orders
      </label>
    </div>

    <div class="form-actions">
      <asp:Button ID="btnSave" runat="server" Text="Save supplier" CssClass="btn btn-primary"
                  ValidationGroup="Supplier" OnClick="btnSave_Click" />
      <asp:Button ID="btnCancel" runat="server" Text="Clear" CssClass="btn btn-ghost"
                  OnClick="btnCancel_Click" CausesValidation="false" />
    </div>
  </div>
</div>

</asp:Content>
