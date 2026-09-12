<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Categories.aspx.cs" Inherits="SmartShelf.Manager.Categories" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Categories &mdash; SmartShelf</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="dash-grid">
  <div class="app-card">
    <div class="card-head"><h4>Categories</h4></div>

    <asp:Repeater ID="rptCategories" runat="server" OnItemCommand="rptCategories_ItemCommand">
      <HeaderTemplate>
        <table class="data-table">
          <thead><tr><th>Name</th><th>Description</th><th class="text-right">Products</th><th>Status</th><th></th></tr></thead>
          <tbody>
      </HeaderTemplate>
      <ItemTemplate>
        <tr>
          <td><strong><%# Server.HtmlEncode((string)Eval("Name")) %></strong></td>
          <td class="muted"><%# Server.HtmlEncode(Convert.ToString(Eval("Description"))) %></td>
          <td class="num-cell"><%# ProductCount((int)Eval("CategoryId")) %></td>
          <td><span class="pill <%# (bool)Eval("IsActive") ? "pill-green" : "pill-grey" %>"><%# (bool)Eval("IsActive") ? "Active" : "Hidden" %></span></td>
          <td class="text-right" style="white-space:nowrap;">
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="edit"
                            CommandArgument='<%# Eval("CategoryId") %>' Text="Edit" CausesValidation="false" />
            <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="delete"
                            CommandArgument='<%# Eval("CategoryId") %>' Text="Delete" CausesValidation="false"
                            OnClientClick="return confirm('Delete this category? It must have no products.');" />
          </td>
        </tr>
      </ItemTemplate>
      <FooterTemplate></tbody></table></FooterTemplate>
    </asp:Repeater>
  </div>

  <div class="app-card">
    <h4 style="font-size:16px;margin-bottom:16px;"><asp:Literal ID="litFormTitle" runat="server" /></h4>
    <asp:HiddenField ID="hfCategoryId" runat="server" Value="0" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtName" Text="Name *" />
      <asp:TextBox ID="txtName" runat="server" MaxLength="60" ValidationGroup="Category" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Category" ErrorMessage="A category name is required." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtDescription" Text="Description" />
      <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="2" MaxLength="200" />
    </div>

    <div class="field-group">
      <label style="display:flex;align-items:center;gap:8px;font-size:13.5px;">
        <asp:CheckBox ID="chkActive" runat="server" Checked="true" /> Available when adding products
      </label>
    </div>

    <div class="form-actions">
      <asp:Button ID="btnSave" runat="server" Text="Save category" CssClass="btn btn-primary"
                  ValidationGroup="Category" OnClick="btnSave_Click" />
      <asp:Button ID="btnCancel" runat="server" Text="Clear" CssClass="btn btn-ghost"
                  OnClick="btnCancel_Click" CausesValidation="false" />
    </div>
  </div>
</div>

</asp:Content>
