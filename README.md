# SmartShelf — Supermarket Management System

A web-based supermarket management system for the Forte Supermarket scenario: one system covering
the checkout counter, the stockroom and the manager's office, with a supplier portal attached.

**Stack:** ASP.NET Web Forms · .NET Framework 4.8.1 · C# · SQL Server via Entity Framework 6
(Code-First POCOs) · Master Page + a single themed stylesheet.

---

## 1. Getting it running

### Prerequisites
- Visual Studio 2022 with the *ASP.NET and web development* workload
- SQL Server LocalDB (installed with Visual Studio) or any SQL Server instance

### Steps

1. **Create the database.** Open `App_Data\SmartShelf_Schema.sql` and run the whole file against
   your server. From a terminal:

   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -i App_Data\SmartShelf_Schema.sql
   ```

   It drops and recreates the `SmartShelf` database, all 13 tables, and about 45 days of trading
   history so every report has real data to aggregate from the first login. Re-running it is safe
   and resets everything.

2. **Check the connection string** in `Web.config`. The default points at LocalDB:

   ```xml
   <add name="SmartShelfDb"
        connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SmartShelf;Integrated Security=True;MultipleActiveResultSets=True"
        providerName="System.Data.SqlClient" />
   ```

3. **Restore NuGet packages and build.** The only package is EntityFramework 6.5.1. In Visual
   Studio, `Build → Rebuild Solution`, then F5.

### Deploying to IIS or SmarterASP.NET

Change the one connection string to the host's SQL Server details and publish. Nothing else is
environment-specific — there are no external scripts, CDNs or file-system dependencies. Run
`SmartShelf_Schema.sql` against the hosted database once.

---

## 2. Demo accounts

Passwords are stored only as PBKDF2 hashes; these are the plain values for the seeded accounts.

| Role | Username | Password | Lands on |
|---|---|---|---|
| Cashier | `a.booi` | `Cashier@123` | POS Terminal |
| Cashier | `n.dlamini` | `Cashier@123` | POS Terminal |
| Store Manager | `a.right` | `Manager@123` | Store Dashboard |
| Store Manager | `t.jacobs` | `Manager@123` | Store Dashboard |
| Supplier (Amathole Fresh Produce) | `s.mkhize` | `Supplier@123` | My Orders |
| Supplier (Sunrise Wholesale) | `f.pillay` | `Supplier@123` | My Orders |
| Admin | `admin` | `Admin@123` | User Accounts |

---

## 3. The five focus areas, and where they live

| Focus | Screens | Service |
|---|---|---|
| **A — Product catalogue & pricing** | `Manager/Products.aspx`, `ProductEdit.aspx`, `Categories.aspx`, `Suppliers.aspx` | `CatalogService` |
| **B — Real-time stock control** | `Manager/Inventory.aspx`, `StockMovements.aspx`, `Dashboard.aspx` | `InventoryService` |
| **C — Supplier order processing** | `Manager/PurchaseOrders.aspx`, `PurchaseOrderEdit.aspx`, `Supplier/Orders.aspx` | `PurchaseOrderService` |
| **D — POS sales transactions** | `Cashier/POS.aspx`, `Receipt.aspx`, `MySales.aspx` | `SaleService` |
| **E — Cash-up & reconciliation** | `Cashier/CashUp.aspx`, `Manager/CashUpReview.aspx` | `CashUpService` |

### The four key reports

All four are on `Manager/Reports.aspx`, one per tab, each with a date filter and a CSV export.
Every figure is aggregated live from the ordinary tables the rest of the system writes to —
nothing is stored, cached or pre-calculated, so a sale rung up a minute ago is already counted.

1. **Top-selling products** — `SaleLine` grouped by product over a date range, by units, revenue
   and gross profit.
2. **Low-stock alerts** — active products at or below their reorder level, with the preferred
   supplier, a suggested order quantity and what it would cost.
3. **Expiry & waste** — `StockBatch` rows near or past their expiry date with the value at risk,
   plus everything already written off as waste in the period.
4. **Supplier performance** — per supplier: orders placed, delivered, on-time rate, fill rate
   (units received ÷ ordered) and average time to confirm.

---

## 4. How the code is arranged

```
Models/            EF6 POCOs (Entities.cs, Enums.cs) and SmartShelfContext
Services/          All business rules. Pages call these; nothing else touches the database.
Controls/          Reusable .ascx: AlertBox, StatCard, StockStatusPill
Handlers/          ProductImage.ashx — serves uploaded images, falls back to a URL
Cashier/ Manager/ Supplier/ Admin/    One folder per role, gated in Web.config
App.master         Signed-in shell; the sidebar is built from the user's role
Site.master        Public shell for the landing page
Styles/site.css    The whole design system, one file
App_Data/          SmartShelf_Schema.sql — schema + seed, the source of truth for the database
```

**Why a service layer.** Every rule that matters — recalculating a sale, refusing to drive stock
negative, computing expected cash — lives in exactly one place. Code-behind reads the form,
calls a service, and shows either a success banner or the rule's own message. That is also why
the same protections apply no matter which screen triggers them.

**Data access** is entirely EF6 / LINQ. There is no string-concatenated SQL anywhere in the C#;
the only hand-written SQL in the project is the schema and seed script.

### Admin oversight screens

Two screens under **Admin** exist for checking the system rather than running it:

- **System Activity** (`Admin/ActivityLog.aspx`) — one timeline of everything the system has
  recorded: sales, stock movements and adjustments, write-offs, cash-ups and their sign-off,
  and purchase order progress, plus an **Access** category covering sign-ins, sign-outs, failed
  sign-in attempts and account changes. Trading activity is assembled by `Services/ActivityService.cs`
  straight from the tables the system already writes; Access events are written as they happen to a
  dedicated `SecurityEvent` table (see `Services/SecurityEventService.cs`, called from `AuthService`
  and `UserService`). `SecurityEvent` only holds history from the point it was introduced, so a page
  note still flags that an empty Access log from before then is not proof nothing happened.
- **System Audit** (`Admin/SystemAudit.aspx`) — the written verification of this build against the
  System Matrix: a coverage row per use case, the findings by severity with the evidence behind
  each one, and a live panel of row counts so a reader can tell whether the system still matches
  what was reviewed. The content lives in `Services/AuditReport.cs`.

---

## 5. Business rules the system enforces

- **Passwords are never stored or shown in plain text.** PBKDF2 (`Rfc2898DeriveBytes`, 16-byte
  random salt, 100 000 iterations), compared in fixed time. There is no screen anywhere that can
  display an existing password — an administrator can only set a new one.

- **A sale's change due is always calculated server-side.** The POS posts back only the cart's
  product IDs and quantities, the payment method and the cash tendered. `SaleService` re-reads
  every price from the database and recomputes the subtotal, VAT, total and change. A tampered
  total or change value in the form is simply not read.

- **A cash-up's expected total is always calculated server-side.** The expected figure is shown
  read-only and never posted; the form sends only the counted cash and a note. `CashUpService`
  recomputes `opening float + completed cash sales` from the sales themselves before working out
  the variance, so no client-side edit can make a discrepancy disappear.

- **Stock can never go negative.** Checked inside the same transaction that writes the sale, and
  again on manual adjustments and waste write-offs. A sale that would overdraw any line is
  rejected in full rather than partially applied, and the cashier is told which product and how
  many are left. The database carries a `CHECK (QuantityOnHand >= 0)` constraint as a backstop.

- **Every stock change and every cash-up variance names a person.** Each sale line, adjustment,
  goods receipt and write-off writes a `StockMovement` row carrying the acting user's ID, in the
  same transaction as the change it explains. Cash-ups record the cashier who counted and, once
  reviewed, the manager who signed it off. `Manager/StockMovements.aspx` is that audit trail.

- **Roles are separated twice over.** `Web.config` gates each folder, and every page behind the
  login also inherits a server-side role check from `SecurePageBase`, so a mis-edited config
  cannot quietly expose a screen. Supplier accounts are scoped further: the service layer refuses
  any action on an order belonging to another company.

- **Every form validates on the server.** ASP.NET validators handle the browser side; the service
  layer re-checks independently before any write. Known rule violations surface as a readable
  message in the page's alert banner; anything unexpected is logged and the visitor sees
  `Error.aspx` rather than a stack trace.

---

## 6. Verifying it end to end

1. **Sign in as each role** and confirm each lands on its own home page. Hand-type another role's
   URL (e.g. a cashier requesting `/Admin/Users.aspx`) and confirm you get *Access denied*.

2. **Ring up a sale.** As `a.booi`, note a product's stock on `Manager/Inventory.aspx` first, then
   scan or tap it twice on the POS, pay cash with more than the total, and check the change. Then
   confirm: stock dropped by two, a movement row appeared under the cashier's name in
   `Manager/StockMovements.aspx`, and the dashboard's revenue tile and top-sellers report both
   moved.

3. **Try to oversell.** Add more units than are on hand — the till refuses with the quantity
   remaining, and nothing is written.

4. **Try to tamper.** With browser dev tools, change the posted total or change on the payment
   form, and the expected-cash field on the cash-up form. The saved record matches the server's
   own calculation, not what was posted.

5. **Run an order through both sides.** As a manager raise a purchase order for Amathole Fresh
   Produce and send it → sign in as `s.mkhize`, confirm it → back as the manager, receive the
   goods with an expiry date. Stock rises, a batch appears in the expiry report, and the supplier
   performance report reflects the new order.

6. **Close a shift short.** Cash up counting R20 less than expected; the variance is recorded
   against the cashier and appears in the manager's review queue.

7. **Run all four reports** with different date ranges and export each to CSV.

---

## 7. Notes and known limits

- The seeded 45 days of trading is generated by the schema script into the ordinary tables. It is
  starting data, not report output — everything downstream is computed from it, and new activity
  changes the reports immediately.
- Product images may be a URL (the seeded rows use Pexels) or an uploaded file stored in the
  database. Uploads are capped at 2 MB and validated by file signature, not just extension.
- Session state is in-process, which is right for a single store on one server. A web farm would
  need `SQLServer` mode instead.
- `Web.config` pins the culture to `en-GB` so money and dates read the same on every machine.
  Amounts posted from number inputs are parsed with the invariant culture regardless.
