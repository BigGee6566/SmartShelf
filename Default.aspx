<%@ Page Title="" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="SmartShelf.Default" %>

<asp:Content ID="cTitle" ContentPlaceHolderID="TitleContent" runat="server">SmartShelf &mdash; Run your till, stock and suppliers from one screen</asp:Content>

<asp:Content ID="cMain" ContentPlaceHolderID="MainContent" runat="server">

<%--<header class="hero">--%>
  <div class="wrap hero-grid">
    <div>
      <span class="eyebrow">Built for Forte Supermarket &amp; stores like it</span>
      <h1>One shelf.<br />One till.<br /><em>One system.</em></h1>
      <p class="hero-sub">SmartShelf brings checkout, stock and supplier orders into a single screen &mdash; so a busy Saturday trading day never turns into a Monday of chasing missing stock.</p>
      <div class="hero-actions">
        <a href="<%: ResolveUrl("~/Login.aspx") %>" class="btn btn-primary">Open till</a>
        <a href="#how" class="btn btn-ghost">See how it works</a>
      </div>
      <%-- These three figures come from the running system, not from the brochure copy. --%>
      <div class="hero-stats">
        <div><span class="num"><%: ProductCount %></span><span class="lbl">Products tracked</span></div>
        <div><span class="num"><%: SaleCount %></span><span class="lbl">Sales on record</span></div>
        <div><span class="num">4</span><span class="lbl">Staff roles, 1 login screen</span></div>
      </div>
    </div>
    <div class="hero-visual">
      <div class="badge-corner">Sprint 1<br />Complete</div>
      <div class="main-photo">
        <img src="https://images.pexels.com/photos/28907758/pexels-photo-28907758.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1200" alt="Modern supermarket produce aisle" />
      </div>
      <div class="float-card">
        <img src="https://images.pexels.com/photos/37321079/pexels-photo-37321079.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=600" alt="Fresh vegetables display" />
        <div style="font-size:12.5px;color:var(--color-muted);font-weight:600;">Stock synced to the till in real time</div>
      </div>
    </div>
  </div>
</header>

<div class="trust-strip">
  <div class="wrap">
    <span class="label">Designed around the people who actually run the store</span>
    <div class="names">
      <span>Cashiers</span><span>Store Managers</span><span>Suppliers</span><span>Admins</span>
    </div>
  </div>
</div>

<section id="features">
  <div class="wrap">
    <div class="section-head">
      <span class="eyebrow">What it replaces</span>
      <h2>The three jobs a paper-and-spreadsheet till can&#39;t do at once.</h2>
      <p>Forte Supermarket&#39;s problem wasn&#39;t a lack of effort &mdash; it was a POS system that couldn&#39;t talk to the stockroom, and a stockroom that couldn&#39;t talk to suppliers. SmartShelf closes that loop.</p>
    </div>

    <div class="feature-row reveal">
      <div class="feature-media">
        <img src="https://images.pexels.com/photos/4199490/pexels-photo-4199490.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1000" alt="Cashier and customer at checkout using mobile payment" />
      </div>
      <div class="feature-copy">
        <span class="feature-num">01 &mdash; Checkout</span>
        <h3>A till that keeps up with the queue.</h3>
        <p>Scan, search, or tap an item &mdash; the running total updates instantly, and every sale is written straight through to stock, no end-of-day reconciliation guesswork.</p>
        <ul class="feature-list">
          <li>Barcode scan or manual product search on the same screen</li>
          <li>Cash, card or mobile payment, with the receipt on the spot</li>
          <li>Change due calculated server-side, never trusted from the till</li>
        </ul>
      </div>
    </div>

    <div class="feature-row reverse reveal">
      <div class="feature-media">
        <img src="https://images.pexels.com/photos/4484149/pexels-photo-4484149.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1000" alt="Warehouse worker checking inventory on a tablet" />
      </div>
      <div class="feature-copy">
        <span class="feature-num">02 &mdash; Stock</span>
        <h3>Know what&#39;s on the shelf before the customer does.</h3>
        <p>Every sale, adjustment and delivery updates one inventory record. Low-stock and expiry warnings surface before they become empty shelves or wasted stock.</p>
        <ul class="feature-list">
          <li>Live stock levels tied directly to sales and receiving</li>
          <li>Manual adjustments logged with a reason and the manager&#39;s name</li>
          <li>Reorder thresholds flag products before they run out</li>
        </ul>
      </div>
    </div>

    <div class="feature-row reveal">
      <div class="feature-media">
        <img src="https://images.pexels.com/photos/16211537/pexels-photo-16211537.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=1000" alt="Grocery store shelves stocked with goods" />
      </div>
      <div class="feature-copy">
        <span class="feature-num">03 &mdash; Suppliers &amp; Reporting</span>
        <h3>Orders, deliveries and cash-ups, on the record.</h3>
        <p>Suppliers see and confirm their own orders. Managers reconcile a shift&#39;s cash against system totals in minutes, not a stack of till slips.</p>
        <ul class="feature-list">
          <li>Supplier portal for order confirmation and delivery status</li>
          <li>Tamper-evident cash-up: the expected total is never editable</li>
          <li>Top-sellers, low-stock, expiry and supplier performance reports</li>
        </ul>
      </div>
    </div>
  </div>
</section>

<section id="roles" style="background:var(--color-bg-warm);">
  <div class="wrap">
    <div class="section-head">
      <span class="eyebrow">One login, four roles</span>
      <h2>Everyone gets exactly the screen they need.</h2>
      <p>There&#39;s a single login screen for the whole store &mdash; SmartShelf routes each person to their own workspace the moment they sign in.</p>
    </div>
    <div class="card-grid">
      <div class="role-card">
        <div class="icon-tag">C</div>
        <h4>Cashier</h4>
        <p>POS terminal, payment, receipts and end-of-shift cash-up.</p>
      </div>
      <div class="role-card">
        <div class="icon-tag">M</div>
        <h4>Store Manager</h4>
        <p>Stock levels, adjustments, catalogue, purchase orders and reports.</p>
      </div>
      <div class="role-card">
        <div class="icon-tag">S</div>
        <h4>Supplier</h4>
        <p>View and confirm orders, update delivery status.</p>
      </div>
      <div class="role-card">
        <div class="icon-tag">A</div>
        <h4>Admin</h4>
        <p>User accounts, roles and permissions across the system.</p>
      </div>
    </div>
  </div>
</section>

<section id="how">
  <div class="wrap">
    <div class="gallery-strip reveal">
      <img src="https://images.pexels.com/photos/9070110/pexels-photo-9070110.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=900" alt="Grocery basket with fresh greens and milk" />
      <div class="stack">
        <img src="https://images.pexels.com/photos/20008782/pexels-photo-20008782.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=700" alt="Bread selection on a grocery shelf" />
        <img src="https://images.pexels.com/photos/4971916/pexels-photo-4971916.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=700" alt="Woman weighing fresh produce in a supermarket" />
      </div>
    </div>
  </div>
</section>

<section>
  <div class="wrap">
    <div class="stat-band reveal">
      <div class="wrap-inner">
        <div class="stat"><span class="num"><%: CategoryCount %></span><span class="lbl">Product categories priced</span></div>
        <div class="stat"><span class="num">R20</span><span class="lbl">Cash-up variance review threshold</span></div>
        <div class="stat"><span class="num"><%: SupplierCount %></span><span class="lbl">Suppliers ordering through the portal</span></div>
        <div class="stat"><span class="num">4</span><span class="lbl">Key reports, live off the data</span></div>
      </div>
    </div>
  </div>
</section>

<section>
  <div class="wrap">
    <div class="testimonial reveal">
      <img src="https://images.pexels.com/photos/4199490/pexels-photo-4199490.jpeg?auto=compress&amp;cs=tinysrgb&amp;w=300" alt="Store team at the checkout counter" />
      <div>
        <blockquote>&ldquo;We didn&#39;t want another generic dashboard. We wanted the till, the stockroom and the supplier calls to finally agree with each other.&rdquo;</blockquote>
        <cite>&mdash; CoreLink, on why SmartShelf started with the checkout counter, not a spreadsheet</cite>
      </div>
    </div>
  </div>
</section>

<section>
  <div class="wrap">
    <div class="cta-band reveal">
      <h2>See it running, not just described.</h2>
      <p>Sign in as a Cashier and ring up a real sale &mdash; the stock level moves as you do it.</p>
      <div class="hero-actions">
        <a href="<%: ResolveUrl("~/Login.aspx") %>" class="btn btn-accent">Launch the live demo &rarr;</a>
      </div>
    </div>
  </div>
</section>

<script>
  // Fade sections in as they scroll into view - progressive enhancement only, the page
  // is perfectly readable if this never runs.
  (function () {
    var els = document.querySelectorAll('.reveal');
    if (!('IntersectionObserver' in window)) {
      for (var i = 0; i < els.length; i++) els[i].classList.add('in');
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
      });
    }, { threshold: 0.15 });
    for (var j = 0; j < els.length; j++) io.observe(els[j]);
  })();
</script>

</asp:Content>
