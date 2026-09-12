<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="StatCard.ascx.cs" Inherits="SmartShelf.Controls.StatCard" %>
<div class="stat-card">
  <div class="sc-lbl"><%: Label %></div>
  <div class="sc-num" style="<%: NumberStyle %>"><%: Value %></div>
  <div class="sc-delta <%: DeltaCss %>"><%: Delta %></div>
</div>
