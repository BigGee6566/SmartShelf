<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="CashUp.aspx.cs" Inherits="SmartShelf.Cashier.CashUp" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Cash-Up &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:HyperLink runat="server" NavigateUrl="~/Cashier/MySales.aspx" CssClass="btn btn-ghost btn-sm"
                 Text="&larr; My sales" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<asp:PlaceHolder ID="phNoShift" runat="server" Visible="false">
  <div class="app-card empty-state">
    You have no open shift to cash up.<br />
    A shift opens automatically the moment you ring up your first sale.
  </div>
</asp:PlaceHolder>

<asp:PlaceHolder ID="phForm" runat="server">
<div class="dash-grid">

  <%-- Left: what the system says was taken. Read-only by design - see the note below. --%>
  <div class="app-card">
    <div class="card-head">
      <h4>What the system recorded</h4>
      <span class="tag"><asp:Literal ID="litTill" runat="server" /></span>
    </div>

    <table class="data-table">
      <tbody>
        <tr><td>Shift opened</td><td class="num-cell"><asp:Literal ID="litOpened" runat="server" /></td></tr>
        <tr><td>Sales rung up</td><td class="num-cell"><asp:Literal ID="litSaleCount" runat="server" /></td></tr>
        <tr><td>Opening float</td><td class="num-cell"><asp:Literal ID="litFloat" runat="server" /></td></tr>
        <tr><td>Cash sales</td><td class="num-cell"><asp:Literal ID="litCash" runat="server" /></td></tr>
        <tr><td>Card sales <span class="muted">(not in the drawer)</span></td><td class="num-cell muted"><asp:Literal ID="litCard" runat="server" /></td></tr>
        <tr><td>Mobile sales <span class="muted">(not in the drawer)</span></td><td class="num-cell muted"><asp:Literal ID="litMobile" runat="server" /></td></tr>
      </tbody>
    </table>

    <div style="margin-top:18px;padding-top:16px;border-top:1px dashed var(--color-line);display:flex;justify-content:space-between;align-items:baseline;">
      <span style="font-weight:600;">Expected in the drawer</span>
      <span style="font-family:var(--font-display);font-size:26px;font-weight:600;color:var(--color-primary-dark);">
        <asp:Literal ID="litExpected" runat="server" />
      </span>
    </div>
    <p class="hint" style="margin-top:8px;">
      Float plus every completed cash sale on this shift. Calculated on the server each time this
      page loads and again when you submit &mdash; it is never sent from the browser, so it cannot be edited.
    </p>
  </div>

  <%-- Right: the one number the cashier actually supplies. --%>
  <div class="app-card">
    <h4 style="font-size:17px;margin-bottom:6px;">Count the drawer</h4>
    <p class="muted" style="font-size:13.5px;margin-bottom:20px;">
      Count the cash, including the float, and enter the total. SmartShelf works out the variance.
    </p>

    <div class="field-group">
      <asp:Label ID="lblCounted" runat="server" AssociatedControlID="txtCounted" Text="Counted cash (R)" />
      <asp:TextBox ID="txtCounted" runat="server" TextMode="Number" step="0.01" min="0"
                   ValidationGroup="CashUp" AutoPostBack="true" OnTextChanged="txtCounted_TextChanged" />
      <asp:RequiredFieldValidator ID="rfvCounted" runat="server" ControlToValidate="txtCounted"
           Display="Dynamic" CssClass="field-error" ValidationGroup="CashUp"
           ErrorMessage="Enter the amount you counted." />
      <asp:CompareValidator ID="cvCounted" runat="server" ControlToValidate="txtCounted"
           Operator="DataTypeCheck" Type="Currency" Display="Dynamic" CssClass="field-error"
           ValidationGroup="CashUp" ErrorMessage="Enter a valid amount, for example 4362.50" />
      <asp:RangeValidator ID="rvCounted" runat="server" ControlToValidate="txtCounted"
           MinimumValue="0" MaximumValue="1000000" Type="Currency" Display="Dynamic"
           CssClass="field-error" ValidationGroup="CashUp"
           ErrorMessage="That amount looks wrong - please check it." />
    </div>

    <div class="app-card" style="background:var(--color-bg-warm);border:none;padding:16px;margin-bottom:18px;">
      <div style="display:flex;justify-content:space-between;font-size:14px;font-weight:600;">
        <span>Variance</span>
        <span id="varianceValue" style="color:<%= VarianceColour %>;"><asp:Literal ID="litVariance" runat="server" /></span>
      </div>
      <div class="hint" style="margin-top:6px;"><asp:Literal ID="litVarianceHint" runat="server" /></div>
    </div>

    <div class="field-group">
      <asp:Label ID="lblNotes" runat="server" AssociatedControlID="txtNotes" Text="Notes (optional)" />
      <asp:TextBox ID="txtNotes" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300"
                   placeholder="Anything that explains a difference - a refund paid out, a miscount, a note from a customer." />
      <span class="hint">If the drawer is out, a note here saves the manager a phone call.</span>
    </div>

    <asp:Button ID="btnSubmit" runat="server" Text="Close shift &amp; submit cash-up"
                CssClass="btn btn-primary" style="width:100%;justify-content:center;padding:14px;"
                ValidationGroup="CashUp" OnClick="btnSubmit_Click"
                OnClientClick="return confirm('Submit this cash-up and close your shift? You cannot undo this.');" />
    <p class="hint" style="margin-top:10px;">
      Submitting closes your shift. Any variance is recorded against your name and goes to the
      manager&#39;s reconciliation queue.
    </p>
  </div>
</div>
</asp:PlaceHolder>

<%-- Confirmation shown after a successful cash-up --%>
<asp:PlaceHolder ID="phDone" runat="server" Visible="false">
  <div class="app-card" style="max-width:520px;">
    <span class="eyebrow">Shift closed</span>
    <h4 style="font-size:20px;margin-bottom:14px;">Cash-up recorded</h4>
    <div style="display:flex;justify-content:space-between;font-size:14px;margin-bottom:8px;">
      <span class="muted">Expected</span><span style="font-weight:600;"><asp:Literal ID="litDoneExpected" runat="server" /></span>
    </div>
    <div style="display:flex;justify-content:space-between;font-size:14px;margin-bottom:8px;">
      <span class="muted">Counted</span><span style="font-weight:600;"><asp:Literal ID="litDoneCounted" runat="server" /></span>
    </div>
    <div style="display:flex;justify-content:space-between;font-size:14px;padding-top:8px;border-top:1px dashed var(--color-line);">
      <span class="muted">Variance</span>
      <span style="font-weight:700;"><asp:Literal ID="litDoneVariance" runat="server" /></span>
    </div>
    <p class="muted" style="font-size:13.5px;margin-top:16px;"><asp:Literal ID="litDoneMessage" runat="server" /></p>
    <div class="form-actions">
      <asp:HyperLink runat="server" NavigateUrl="~/Cashier/POS.aspx" CssClass="btn btn-primary" Text="Back to the till" />
      <asp:HyperLink runat="server" NavigateUrl="~/Cashier/MySales.aspx" CssClass="btn btn-ghost" Text="View my sales" />
    </div>
  </div>
</asp:PlaceHolder>

</asp:Content>
