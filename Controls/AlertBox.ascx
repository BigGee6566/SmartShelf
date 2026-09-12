<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AlertBox.ascx.cs" Inherits="SmartShelf.Controls.AlertBox" %>
<asp:PlaceHolder ID="phAlert" runat="server" Visible="false">
  <div class="alert <%: CssModifier %>" role="alert">
    <asp:Literal ID="litMessage" runat="server" />
  </div>
</asp:PlaceHolder>
