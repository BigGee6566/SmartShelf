<%@ Page Title="" Language="C#" MasterPageFile="~/App.master" AutoEventWireup="true" CodeBehind="SystemAudit.aspx.cs" Inherits="SmartShelf.Admin.SystemAudit" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">System audit &mdash; SmartShelf</asp:Content>

<asp:Content ID="cHead" ContentPlaceHolderID="HeadContent" runat="server">
<style>
  .finding { border: 1px solid var(--color-line); border-radius: 8px; padding: 18px 20px; margin-bottom: 14px; background: #fff; }
  .finding.fixed-item { border-left: 3px solid var(--color-primary); }
  .finding-head { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 10px; }
  .finding-head .where { font-family: var(--font-display); font-weight: 600; font-size: 15.5px; color: var(--color-primary-dark); }
  .finding dl { margin: 0; display: grid; grid-template-columns: 92px 1fr; gap: 6px 14px; font-size: 13.5px; }
  @media (max-width: 620px) { .finding dl { grid-template-columns: 1fr; gap: 2px; } .finding dt { margin-top: 8px; } }
  .finding dt { color: var(--color-muted); font-weight: 700; text-transform: uppercase; font-size: 11px; letter-spacing: 0.05em; padding-top: 2px; }
  .finding dd { margin: 0; }
  .verified-list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 11px; }
  .verified-list li { display: flex; gap: 11px; font-size: 14px; }
  .verified-list li::before { content: "\2713"; color: var(--color-primary); font-weight: 700; flex-shrink: 0; }
  .facts-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 14px; }
  .fact { border: 1px solid var(--color-line); border-radius: 6px; padding: 12px 14px; }
  .fact .fv { font-family: var(--font-display); font-size: 21px; font-weight: 600; color: var(--color-primary-dark); }
  .fact .fl { font-size: 12px; color: var(--color-muted); text-transform: uppercase; letter-spacing: 0.04em; }
  .cov-table td { vertical-align: top; font-size: 13.5px; }
  .cov-table .note { color: var(--color-muted); font-size: 12.5px; line-height: 1.5; }
</style>
</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

  <div class="stat-card-row">
    <div class="stat-card">
      <div class="sc-lbl">Critical</div>
      <div class="sc-num" style="color:#C0392B;"><asp:Literal ID="litCritical" runat="server" /></div>
      <div class="sc-delta down">Use case absent entirely</div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Major</div>
      <div class="sc-num" style="color:var(--color-accent-dark);"><asp:Literal ID="litMajor" runat="server" /></div>
      <div class="sc-delta down"><asp:Literal ID="litMajorFixed" runat="server" /></div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Minor</div>
      <div class="sc-num"><asp:Literal ID="litMinor" runat="server" /></div>
      <div class="sc-delta muted">Confusing or inconsistent</div>
    </div>
    <div class="stat-card">
      <div class="sc-lbl">Matrix rows clean</div>
      <div class="sc-num"><asp:Literal ID="litClean" runat="server" /></div>
      <div class="sc-delta muted">Of 21 use cases</div>
    </div>
  </div>

  <div class="app-card" style="margin-bottom:22px;">
    <div class="card-head">
      <h4>Verdict</h4>
      <span class="tag"><asp:Literal ID="litReviewedOn" runat="server" /></span>
    </div>
    <p style="font-size:15px;line-height:1.6;">
      <asp:Literal ID="litVerdict" runat="server" />
    </p>
  </div>

  <div class="app-card" style="margin-bottom:22px;">
    <div class="card-head">
      <h4>Live system facts</h4>
      <span class="muted" style="font-size:12.5px;">Queried when this page loaded</span>
    </div>
    <asp:PlaceHolder ID="phFactsError" runat="server" Visible="false">
      <div class="alert alert-error" style="margin-bottom:0;">
        The database could not be reached, so these counts are unavailable. Everything else on this page
        is the written record of the review and is still accurate as of the date shown above.
      </div>
    </asp:PlaceHolder>
    <asp:PlaceHolder ID="phFacts" runat="server">
      <div class="facts-grid">
        <asp:Repeater ID="rptFacts" runat="server">
          <ItemTemplate>
            <div class="fact">
              <div class="fv"><%# Eval("Value", "{0:N0}") %></div>
              <div class="fl"><%# Server.HtmlEncode((string)Eval("Label")) %></div>
            </div>
          </ItemTemplate>
        </asp:Repeater>
      </div>
    </asp:PlaceHolder>
  </div>

  <div class="app-card" style="margin-bottom:22px;">
    <div class="card-head">
      <h4>Matrix coverage &mdash; UC1 to UC21</h4>
      <span class="muted" style="font-size:12.5px;">Checked against the actual data-access code, not the screen</span>
    </div>
    <div style="overflow-x:auto;">
      <table class="data-table cov-table">
        <thead>
          <tr>
            <th>Use case</th>
            <th>Built?</th>
            <th>Tables</th>
            <th>Screen</th>
            <th>Report</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          <asp:Repeater ID="rptCoverage" runat="server">
            <ItemTemplate>
              <tr>
                <td style="white-space:nowrap;">
                  <strong><%# Server.HtmlEncode((string)Eval("UseCase")) %></strong>
                  <div class="note"><%# Server.HtmlEncode((string)Eval("Screen")) %></div>
                </td>
                <td><span class="pill <%# GetCss(Eval("Implemented")) %>"><%# Eval("Implemented") %></span></td>
                <td><span class="pill <%# GetCss(Eval("Tables")) %>"><%# Eval("Tables") %></span></td>
                <td><span class="pill <%# GetCss(Eval("ScreenMatch")) %>"><%# Eval("ScreenMatch") %></span></td>
                <td><span class="pill <%# GetCss(Eval("ReportWorks")) %>"><%# Eval("ReportWorks") %></span></td>
                <td class="note" style="min-width:280px;"><%# Server.HtmlEncode((string)Eval("Notes")) %></td>
              </tr>
            </ItemTemplate>
          </asp:Repeater>
        </tbody>
      </table>
    </div>
  </div>

  <div class="app-card" style="margin-bottom:22px;">
    <div class="card-head"><h4>Findings</h4></div>
    <asp:Repeater ID="rptFindings" runat="server">
      <ItemTemplate>
        <div class="finding <%# (bool)Eval("Fixed") ? "fixed-item" : "" %>">
          <div class="finding-head">
            <span class="pill <%# GetSeverityCss(Eval("Severity")) %>"><%# Eval("Severity") %></span>
            <span class="where"><%# Server.HtmlEncode((string)Eval("Where")) %></span>
            <asp:PlaceHolder runat="server" Visible='<%# (bool)Eval("Fixed") %>'>
              <span class="pill pill-green">Fixed in this pass</span>
            </asp:PlaceHolder>
          </div>
          <dl>
            <dt>What</dt><dd><%# Server.HtmlEncode((string)Eval("What")) %></dd>
            <dt>Evidence</dt><dd class="muted"><%# Server.HtmlEncode((string)Eval("Evidence")) %></dd>
            <dt>Confidence</dt><dd><%# Eval("Confidence") %>%</dd>
            <dt>Fix</dt><dd><%# Server.HtmlEncode((string)Eval("SuggestedFix")) %></dd>
          </dl>
        </div>
      </ItemTemplate>
    </asp:Repeater>
  </div>

  <div class="app-card">
    <div class="card-head"><h4>Checked and sound</h4></div>
    <ul class="verified-list">
      <asp:Repeater ID="rptVerified" runat="server">
        <ItemTemplate><li><%# Server.HtmlEncode(Container.DataItem.ToString()) %></li></ItemTemplate>
      </asp:Repeater>
    </ul>
  </div>

</asp:Content>
