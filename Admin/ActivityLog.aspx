<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="ActivityLog.aspx.cs" Inherits="SmartShelf.Admin.ActivityLog" %>
<%@ Register Src="~/Controls/AlertBox.ascx" TagPrefix="ss" TagName="AlertBox" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">System activity &mdash; SmartShelf</asp:Content>

<asp:Content ID="cTopbar" ContentPlaceHolderID="TopbarContent" runat="server">
  <asp:LinkButton ID="btnExport" runat="server" CssClass="btn btn-ghost btn-sm"
                  OnClick="btnExport_Click" CausesValidation="false" Text="Export CSV" />
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

  <ss:AlertBox ID="alert" runat="server" />

  <div class="stat-card-row">
    <div class="stat-card">
      <div class="sc-lbl">Events shown</div>
      <div class="sc-num"><asp:Literal ID="litCount" runat="server" /></div>
      <div class="sc-delta muted">Matching the filters below</div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Sales recorded</div>
      <div class="sc-num"><asp:Literal ID="litSales" runat="server" /></div>
      <div class="sc-delta muted">All time</div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Stock movements</div>
      <div class="sc-num"><asp:Literal ID="litMovements" runat="server" /></div>
      <div class="sc-delta muted">Every change, with who made it</div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Cash-ups</div>
      <div class="sc-num"><asp:Literal ID="litCashUps" runat="server" /></div>
      <div class="sc-delta muted"><asp:Literal ID="litVariances" runat="server" /></div>
    </div>
  </div>

  <div class="app-card">
    <div class="filter-bar">
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtFrom" Text="From" />
        <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" />
      </div>
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="txtTo" Text="To" />
        <asp:TextBox ID="txtTo" runat="server" TextMode="Date" />
      </div>
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlUser" Text="Staff member" />
        <asp:DropDownList ID="ddlUser" runat="server" />
      </div>
      <div class="field-group">
        <asp:Label runat="server" AssociatedControlID="ddlCategory" Text="Activity type" />
        <asp:DropDownList ID="ddlCategory" runat="server" />
      </div>
      <asp:Button ID="btnFilter" runat="server" Text="Apply" CssClass="btn btn-primary btn-sm"
                  OnClick="btnFilter_Click" CausesValidation="false" />
      <asp:LinkButton ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-ghost btn-sm"
                      OnClick="btnReset_Click" CausesValidation="false" />
    </div>

    <table class="data-table">
      <thead>
        <tr>
          <th>When</th>
          <th>Who</th>
          <th>Type</th>
          <th>Action</th>
          <th>Detail</th>
          <th>Reference</th>
        </tr>
      </thead>
      <tbody>
        <asp:Repeater ID="rptActivity" runat="server">
          <ItemTemplate>
            <tr>
              <td style="white-space:nowrap;"><%# Eval("When", "{0:dd MMM yyyy, HH:mm}") %></td>
              <td>
                <%# Server.HtmlEncode((string)Eval("Who")) %>
                <div class="muted" style="font-size:12px;"><%# Server.HtmlEncode((string)Eval("Role")) %></div>
              </td>
              <td><span class="pill <%# Eval("PillCss") %>"><%# Server.HtmlEncode((string)Eval("Category")) %></span></td>
              <td><%# Server.HtmlEncode((string)Eval("Action")) %></td>
              <td><%# Server.HtmlEncode((string)Eval("Detail")) %></td>
              <td style="white-space:nowrap;"><%# Server.HtmlEncode((string)Eval("Reference")) %></td>
            </tr>
          </ItemTemplate>
        </asp:Repeater>
      </tbody>
    </table>

    <asp:PlaceHolder ID="phNone" runat="server" Visible="false">
      <div class="empty-state">Nothing was recorded in this period for the filters you chose.</div>
    </asp:PlaceHolder>

    <asp:PlaceHolder ID="phCapped" runat="server" Visible="false">
      <div class="alert alert-info" style="margin-top:18px;margin-bottom:0;">
        <asp:Literal ID="litCapped" runat="server" />
      </div>
    </asp:PlaceHolder>
  </div>

  <%-- Being explicit about the blind spot matters: a reader must not treat an empty log as
       proof that nothing happened. --%>
  <div class="app-card" style="margin-top:22px;">
    <div class="card-head"><h4>What this log does and doesn&#39;t cover</h4></div>
    <p class="muted" style="font-size:14px;">
      Every entry above is drawn from the records the system writes as it runs &mdash; stock movements,
      sales, cash-ups and purchase orders. Each one names the staff member responsible, and none of it
      can be edited from any screen.
    </p>
    <p class="muted" style="font-size:14px;margin-top:12px;">
      <strong>Not recorded anywhere, so not shown here:</strong> sign-outs, failed sign-in attempts,
      and changes made to a user account. Sign-ins appear only as the <em>most recent</em> one per
      account, because that is all the system stores. Absence of an entry is not evidence that
      nothing happened.
    </p>
  </div>

</asp:Content>
