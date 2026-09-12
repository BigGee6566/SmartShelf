<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="ProductEdit.aspx.cs" Inherits="SmartShelf.Manager.ProductEdit" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Edit product &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/Products.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; All products" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="dash-grid">
  <div class="app-card">
    <div class="form-grid">

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtBarcode" Text="Barcode *" />
        <asp:TextBox ID="txtBarcode" runat="server" MaxLength="30" ValidationGroup="Product" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtBarcode" Display="Dynamic"
             CssClass="field-error" ValidationGroup="Product" ErrorMessage="A barcode is required." />
        <span class="hint">Must be unique. This is what the till scans.</span>
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtName" Text="Product name *" />
        <asp:TextBox ID="txtName" runat="server" MaxLength="120" ValidationGroup="Product" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" Display="Dynamic"
             CssClass="field-error" ValidationGroup="Product" ErrorMessage="A product name is required." />
      </div>

      <div class="field-group full">
        <asp:Label runat="server" AssociatedControlID="txtDescription" Text="Description" />
        <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="2" MaxLength="300" />
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlCategory" Text="Category *" />
        <asp:DropDownList ID="ddlCategory" runat="server" ValidationGroup="Product" />
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlSupplier" Text="Preferred supplier" />
        <asp:DropDownList ID="ddlSupplier" runat="server" />
        <span class="hint">Used to pre-fill purchase orders raised from the low-stock report.</span>
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtCost" Text="Cost price (R) *" />
        <asp:TextBox ID="txtCost" runat="server" TextMode="Number" step="0.01" min="0" ValidationGroup="Product" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCost" Display="Dynamic"
             CssClass="field-error" ValidationGroup="Product" ErrorMessage="Enter the cost price." />
        <asp:RangeValidator runat="server" ControlToValidate="txtCost" MinimumValue="0" MaximumValue="1000000"
             Type="Currency" Display="Dynamic" CssClass="field-error" ValidationGroup="Product"
             ErrorMessage="Enter a cost between 0 and 1 000 000." />
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtPrice" Text="Selling price (R) *" />
        <asp:TextBox ID="txtPrice" runat="server" TextMode="Number" step="0.01" min="0" ValidationGroup="Product" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPrice" Display="Dynamic"
             CssClass="field-error" ValidationGroup="Product" ErrorMessage="Enter the selling price." />
        <asp:RangeValidator runat="server" ControlToValidate="txtPrice" MinimumValue="0.01" MaximumValue="1000000"
             Type="Currency" Display="Dynamic" CssClass="field-error" ValidationGroup="Product"
             ErrorMessage="The selling price must be greater than zero." />
        <span class="hint" id="marginHint" runat="server"><asp:Literal ID="litMargin" runat="server" /></span>
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlVat" Text="VAT rate *" />
        <asp:DropDownList ID="ddlVat" runat="server">
          <asp:ListItem Value="0.15" Text="Standard rated — 15%" />
          <asp:ListItem Value="0" Text="Zero rated — 0% (basic foodstuffs)" />
        </asp:DropDownList>
      </div>

      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtReorder" Text="Reorder level *" />
        <asp:TextBox ID="txtReorder" runat="server" TextMode="Number" min="0" step="1" ValidationGroup="Product" />
        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtReorder" Display="Dynamic"
             CssClass="field-error" ValidationGroup="Product" ErrorMessage="Enter a reorder level." />
        <asp:RangeValidator runat="server" ControlToValidate="txtReorder" MinimumValue="0" MaximumValue="100000"
             Type="Integer" Display="Dynamic" CssClass="field-error" ValidationGroup="Product"
             ErrorMessage="Enter a whole number between 0 and 100 000." />
        <span class="hint">Low-stock alerts fire at or below this figure.</span>
      </div>

      <div class="field-group full">
        <label style="display:flex;align-items:center;gap:8px;font-size:13.5px;">
          <asp:CheckBox ID="chkActive" runat="server" Checked="true" /> Listed for sale
        </label>
        <span class="hint">Unlisting keeps all history but hides the product from the till.</span>
      </div>

    </div>

    <div class="form-actions">
      <asp:Button ID="btnSave" runat="server" Text="Save product" CssClass="btn btn-primary"
                  ValidationGroup="Product" OnClick="btnSave_Click" />
      <asp:HyperLink runat="server" NavigateUrl="~/Manager/Products.aspx" CssClass="btn btn-ghost" Text="Cancel" />
    </div>
  </div>

  <%-- Image: a CDN URL is enough for most products; uploading a photo overrides it. --%>
  <div class="app-card">
    <h4 style="font-size:16px;margin-bottom:16px;">Product image</h4>

    <div style="border-radius:8px;overflow:hidden;border:1px solid var(--color-line);margin-bottom:16px;">
      <asp:Image ID="imgPreview" runat="server" style="width:100%;height:200px;object-fit:cover;" AlternateText="Current product image" />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtImageUrl" Text="Image URL" />
      <asp:TextBox ID="txtImageUrl" runat="server" MaxLength="400" placeholder="https://..." />
      <span class="hint">A full http:// or https:// address. Used when no file has been uploaded.</span>
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="fuImage" Text="Or upload a photo" />
      <asp:FileUpload ID="fuImage" runat="server" accept="image/png,image/jpeg,image/gif,image/webp" />
      <span class="hint">JPEG, PNG, GIF or WebP, up to 2 MB. An uploaded photo takes precedence over the URL.</span>
    </div>

    <asp:PlaceHolder ID="phHasUpload" runat="server" Visible="false">
      <asp:Button ID="btnClearImage" runat="server" Text="Remove uploaded photo" CssClass="btn btn-ghost btn-sm"
                  OnClick="btnClearImage_Click" CausesValidation="false"
                  OnClientClick="return confirm('Remove the uploaded photo and fall back to the image URL?');" />
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phStock" runat="server" Visible="false">
      <div style="margin-top:24px;padding-top:18px;border-top:1px dashed var(--color-line);">
        <h4 style="font-size:15px;margin-bottom:10px;">Current stock</h4>
        <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:6px;">
          <span class="muted">On hand</span><span style="font-weight:600;"><asp:Literal ID="litOnHand" runat="server" /></span>
        </div>
        <asp:HyperLink ID="lnkAdjust" runat="server" CssClass="btn btn-ghost btn-sm" Text="Adjust stock &rarr;" />
      </div>
    </asp:PlaceHolder>
  </div>
</div>

</asp:Content>
