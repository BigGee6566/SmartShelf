<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="Inventory.aspx.cs" Inherits="SmartShelf.Manager.Inventory" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Inventory &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/StockMovements.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="Movement history" />
  <asp:HyperLink runat="server" NavigateUrl="~/Manager/PurchaseOrders.aspx" CssClass="btn btn-primary btn-sm"
                 Text="Receive a delivery &rarr;" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="app-card">
  <div class="filter-bar">
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtSearch" Text="Search" />
      <asp:TextBox ID="txtSearch" runat="server" placeholder="Name or barcode" />
    </div>
    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlView" Text="Show" />
      <asp:DropDownList ID="ddlView" runat="server">
        <asp:ListItem Value="low" Text="Needs attention (low stock)" />
        <asp:ListItem Value="all" Text="Everything" />
        <asp:ListItem Value="expiring" Text="Has stock expiring soon" />
      </asp:DropDownList>
    </div>
    <asp:Button ID="btnFilter" runat="server" Text="Apply" CssClass="btn btn-ghost btn-sm" OnClick="btnFilter_Click" />
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">Nothing matched. Try &quot;Everything&quot; to see the full shelf.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptStock" runat="server" OnItemCommand="rptStock_ItemCommand">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>Product</th><th class="text-right">On hand</th><th class="text-right">Reorder at</th>
          <th>Status</th><th>Soonest expiry</th><th class="text-right">Stock value</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td>
          <div class="prod-thumb">
            <img src='<%# ImageFor(Container.DataItem) %>' alt="" />
            <span>
              <%# Server.HtmlEncode((string)Eval("Name")) %><br />
              <span class="muted" style="font-size:12px;"><%# Server.HtmlEncode((string)Eval("Barcode")) %></span>
            </span>
          </div>
        </td>
        <td class="num-cell"><strong><%# Eval("QuantityOnHand") %></strong></td>
        <td class="num-cell"><%# Eval("ReorderLevel") %></td>
        <td><span class="pill <%# PillCss(Container.DataItem) %>"><%# PillText(Container.DataItem) %></span></td>
        <td><%# ExpiryCell(Container.DataItem) %></td>
        <td class="num-cell">R <%# StockValue(Container.DataItem) %></td>
        <td class="text-right" style="white-space:nowrap;">
          <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="adjust"
                          CommandArgument='<%# Eval("ProductId") %>' Text="Adjust" CausesValidation="false" />
          <asp:LinkButton runat="server" CssClass="btn btn-ghost btn-sm" CommandName="waste"
                          CommandArgument='<%# Eval("ProductId") %>' Text="Write off" CausesValidation="false" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>
</div>

</asp:Content>

<asp:Content ID="cModal" ContentPlaceHolderID="ModalContent" runat="server">

<%-- Stock take correction. The manager enters the counted figure, not a delta, because that
     is what a physical count actually produces. --%>
<div class="modal-backdrop <%= AdjustModalVisible ? "show" : "" %>">
  <div class="modal-box">
    <span class="eyebrow">Stock adjustment</span>
    <h3 style="font-size:21px;margin-bottom:4px;"><asp:Literal ID="litAdjustProduct" runat="server" /></h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:18px;">
      System says <strong><asp:Literal ID="litAdjustCurrent" runat="server" /></strong> on hand.
    </p>

    <asp:HiddenField ID="hfAdjustProductId" runat="server" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtCounted" Text="Counted quantity *" />
      <asp:TextBox ID="txtCounted" runat="server" TextMode="Number" min="0" step="1" ValidationGroup="Adjust" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCounted" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Adjust" ErrorMessage="Enter what you counted." />
      <asp:RangeValidator runat="server" ControlToValidate="txtCounted" MinimumValue="0" MaximumValue="1000000"
           Type="Integer" Display="Dynamic" CssClass="field-error" ValidationGroup="Adjust"
           ErrorMessage="Enter a whole number of 0 or more." />
      <span class="hint">Stock can never be set below zero.</span>
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtAdjustReason" Text="Reason *" />
      <asp:TextBox ID="txtAdjustReason" runat="server" TextMode="MultiLine" Rows="2" MaxLength="250"
                   ValidationGroup="Adjust" placeholder="Stock take correction, breakage, theft, returned goods..." />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtAdjustReason" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Adjust" ErrorMessage="A reason is required." />
      <span class="hint">Recorded against your name so this change can be traced later.</span>
    </div>

    <asp:Button ID="btnSaveAdjust" runat="server" Text="Save adjustment" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;padding:13px;"
                ValidationGroup="Adjust" OnClick="btnSaveAdjust_Click" />
    <asp:Button ID="btnCancelAdjust" runat="server" Text="Cancel" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCancelModal_Click" CausesValidation="false" />
  </div>
</div>

<%-- Waste write-off, optionally against a specific batch so the expiry report stays honest. --%>
<div class="modal-backdrop <%= WasteModalVisible ? "show" : "" %>">
  <div class="modal-box">
    <span class="eyebrow">Write off waste</span>
    <h3 style="font-size:21px;margin-bottom:4px;"><asp:Literal ID="litWasteProduct" runat="server" /></h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:18px;">
      <strong><asp:Literal ID="litWasteCurrent" runat="server" /></strong> on hand.
    </p>

    <asp:HiddenField ID="hfWasteProductId" runat="server" />

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="ddlWasteBatch" Text="Batch" />
      <asp:DropDownList ID="ddlWasteBatch" runat="server" />
      <span class="hint">Pick the batch if you know it; otherwise the oldest stock is used.</span>
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtWasteQty" Text="Units to write off *" />
      <asp:TextBox ID="txtWasteQty" runat="server" TextMode="Number" min="1" step="1" ValidationGroup="Waste" />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtWasteQty" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Waste" ErrorMessage="Enter how many units." />
      <asp:RangeValidator runat="server" ControlToValidate="txtWasteQty" MinimumValue="1" MaximumValue="1000000"
           Type="Integer" Display="Dynamic" CssClass="field-error" ValidationGroup="Waste"
           ErrorMessage="Enter a whole number of 1 or more." />
    </div>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtWasteReason" Text="Reason *" />
      <asp:TextBox ID="txtWasteReason" runat="server" TextMode="MultiLine" Rows="2" MaxLength="250"
                   ValidationGroup="Waste" placeholder="Expired, damaged, spoiled..." />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtWasteReason" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Waste" ErrorMessage="A reason is required." />
    </div>

    <asp:Button ID="btnSaveWaste" runat="server" Text="Write off" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;padding:13px;"
                ValidationGroup="Waste" OnClick="btnSaveWaste_Click" />
    <asp:Button ID="btnCancelWaste" runat="server" Text="Cancel" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCancelModal_Click" CausesValidation="false" />
  </div>
</div>

</asp:Content>
