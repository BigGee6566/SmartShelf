<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="CashUpReview.aspx.cs" Inherits="SmartShelf.Manager.CashUpReview" %>
<%@ Register TagPrefix="uc" TagName="AlertBox" Src="~/Controls/AlertBox.ascx" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">Cash-up review &mdash; SmartShelf</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<uc:AlertBox ID="alert" runat="server" />

<div class="stat-card-row">
  <div class="stat-card">
    <div class="sc-lbl">Waiting on you</div>
    <div class="sc-num" style="<%= PendingStyle %>"><asp:Literal ID="litPending" runat="server" /></div>
    <div class="sc-delta muted">Unreviewed variances</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Net variance, last 30 days</div>
    <div class="sc-num" style="font-size:24px;"><asp:Literal ID="litNetVariance" runat="server" /></div>
    <div class="sc-delta muted">Shorts and overs combined</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Shifts cashed up</div>
    <div class="sc-num"><asp:Literal ID="litShiftCount" runat="server" /></div>
    <div class="sc-delta muted">Last 30 days</div>
  </div>
  <div class="stat-card">
    <div class="sc-lbl">Balanced exactly</div>
    <div class="sc-num"><asp:Literal ID="litBalancedRate" runat="server" /></div>
    <div class="sc-delta up">Of shifts in the last 30 days</div>
  </div>
</div>

<div class="app-card">
  <div class="card-head">
    <h4>Reconciliation</h4>
    <div class="filter-bar" style="margin-bottom:0;">
      <div class="field-group">
        <asp:DropDownList ID="ddlFilter" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlFilter_Changed">
          <asp:ListItem Value="pending" Text="Variances awaiting review" />
          <asp:ListItem Value="all" Text="Every cash-up" />
        </asp:DropDownList>
      </div>
    </div>
  </div>

  <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
    <div class="empty-state">Nothing to review &mdash; every variance has been signed off.</div>
  </asp:PlaceHolder>

  <asp:Repeater ID="rptCashUps" runat="server" OnItemCommand="rptCashUps_ItemCommand">
    <HeaderTemplate>
      <table class="data-table">
        <thead><tr>
          <th>When</th><th>Cashier</th><th>Till</th>
          <th class="text-right">Expected</th><th class="text-right">Counted</th><th class="text-right">Variance</th>
          <th>Note</th><th>Review</th><th></th>
        </tr></thead>
        <tbody>
    </HeaderTemplate>
    <ItemTemplate>
      <tr>
        <td style="white-space:nowrap;"><%# ((DateTime)Eval("CreatedAt")).ToString("dd MMM yyyy, HH:mm") %></td>
        <td><%# CashierName(Container.DataItem) %></td>
        <td class="muted"><%# TillNumber(Container.DataItem) %></td>
        <td class="num-cell">R <%# ((decimal)Eval("ExpectedCash")).ToString("N2") %></td>
        <td class="num-cell">R <%# ((decimal)Eval("CountedCash")).ToString("N2") %></td>
        <td class="num-cell" style="<%# VarianceStyle(Container.DataItem) %>"><%# VarianceText(Container.DataItem) %></td>
        <td class="muted" style="max-width:220px;"><%# Server.HtmlEncode(Convert.ToString(Eval("Notes"))) %></td>
        <td><%# ReviewCell(Container.DataItem) %></td>
        <td class="text-right">
          <asp:LinkButton runat="server" CssClass="btn btn-primary btn-sm" CommandName="review"
                          CommandArgument='<%# Eval("CashUpId") %>' Text="Review"
                          Visible='<%# NeedsReview(Container.DataItem) %>' CausesValidation="false" />
        </td>
      </tr>
    </ItemTemplate>
    <FooterTemplate></tbody></table></FooterTemplate>
  </asp:Repeater>

  <p class="hint" style="margin-top:16px;">
    Expected cash is the opening float plus every completed cash sale on that shift, recomputed
    from the sales themselves. It was never editable by the cashier, so a variance always means a
    real difference between the drawer and the till.
  </p>
</div>

</asp:Content>

<asp:Content ID="cModal" ContentPlaceHolderID="ModalContent" runat="server">

<div class="modal-backdrop <%= ReviewModalVisible ? "show" : "" %>">
  <div class="modal-box">
    <span class="eyebrow">Sign off a variance</span>
    <h3 style="font-size:21px;margin-bottom:4px;"><asp:Literal ID="litReviewCashier" runat="server" /></h3>
    <p class="muted" style="font-size:13.5px;margin-bottom:18px;"><asp:Literal ID="litReviewMeta" runat="server" /></p>

    <asp:HiddenField ID="hfCashUpId" runat="server" />

    <div class="app-card" style="background:var(--color-bg-warm);border:none;padding:16px;margin-bottom:18px;">
      <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:6px;">
        <span class="muted">Expected</span><span style="font-weight:600;"><asp:Literal ID="litReviewExpected" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:13.5px;margin-bottom:6px;">
        <span class="muted">Counted</span><span style="font-weight:600;"><asp:Literal ID="litReviewCounted" runat="server" /></span>
      </div>
      <div style="display:flex;justify-content:space-between;font-size:14px;padding-top:8px;border-top:1px dashed var(--color-line);">
        <span class="muted">Variance</span>
        <span style="font-weight:700;color:var(--color-accent-dark);"><asp:Literal ID="litReviewVariance" runat="server" /></span>
      </div>
    </div>

    <asp:PlaceHolder ID="phCashierNote" runat="server" Visible="false">
      <div style="margin-bottom:16px;font-size:13.5px;">
        <strong>Cashier&#39;s note:</strong> <asp:Literal ID="litCashierNote" runat="server" />
      </div>
    </asp:PlaceHolder>

    <div class="field-group">
      <asp:Label runat="server" AssociatedControlID="txtReviewNote" Text="What did you find? *" />
      <asp:TextBox ID="txtReviewNote" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300"
                   ValidationGroup="Review"
                   placeholder="Checked against the till roll, found a refund not rung up..." />
      <asp:RequiredFieldValidator runat="server" ControlToValidate="txtReviewNote" Display="Dynamic"
           CssClass="field-error" ValidationGroup="Review" ErrorMessage="Record what you found before signing off." />
      <span class="hint">Stored against your name and the time you signed off.</span>
    </div>

    <asp:Button ID="btnSaveReview" runat="server" Text="Sign off" CssClass="btn btn-primary"
                style="width:100%;justify-content:center;padding:13px;"
                ValidationGroup="Review" OnClick="btnSaveReview_Click" />
    <asp:Button ID="btnCancelReview" runat="server" Text="Cancel" CssClass="btn btn-ghost btn-sm"
                style="width:100%;justify-content:center;margin-top:8px;"
                OnClick="btnCancelReview_Click" CausesValidation="false" />
  </div>
</div>

</asp:Content>
