# SmartShelf — System Overview

Supermarket management system built as coursework for **IFS303E, Systems Development Project** at the University of Fort Hare, under the project name **CoreLink**.

Location: `CoreLink\SmartShelf\SmartShelf`

---

## 1. What the system is

SmartShelf is a single-store supermarket management platform covering four workflows end to end:

1. **Point of Sale** — cashiers ring up sales, take payment, print receipts.
2. **Inventory & Catalogue** — managers maintain products/categories/suppliers, track stock with batch-level expiry (FEFO), and log every stock movement.
3. **Purchasing** — managers raise purchase orders; suppliers confirm/dispatch them through their own portal; managers receive goods in against the order.
4. **Cash & Reporting** — cashiers reconcile their till at end of shift (cash-up); managers review variances and pull sales/stock/supplier reports.

An **Admin** role sits above all of this for user management and system oversight (activity log, self-audit report).

The app ships with ~45–46 days of realistic seeded trading data, so every dashboard/report has real numbers from the first login rather than an empty database.

---

## 2. Tech stack

| Layer | Choice |
|---|---|
| Framework | ASP.NET **Web Forms**, .NET Framework **4.8.1**, C# |
| Data access | **Entity Framework 6.5.1**, Code-First POCOs, `System.Data.SqlClient` |
| Database | SQL Server (SQL Express in current `Web.config`, LocalDB per README example) |
| Auth | **Forms Authentication** — custom login, no ASP.NET Membership provider |
| Password storage | PBKDF2 (`Rfc2898DeriveBytes`), 16-byte salt, 100,000 iterations, fixed-time comparison |
| Session | InProc, 60-min sliding timeout |
| Culture | Pinned to `en-GB` (dates `dd/MM/yyyy`, money `12,480.50`) |
| Other package | `Microsoft.CodeDom.Providers.DotNetCompilerPlatform 2.0.1` (Roslyn build support) |

**Key `Web.config` settings:**
- `Database.SetInitializer<SmartShelfContext>(null)` in `Global.asax.cs` — **EF never creates or migrates the schema.** `App_Data\SmartShelf_Schema.sql` is the single source of truth for the database, and must be kept in sync with `Models\Entities.cs` by hand.
- `ValidationSettings:UnobtrusiveValidationMode = None` — classic Web Forms validators, no jQuery CDN dependency.
- `DefaultVatRate=0.15`, `CashUpVarianceThreshold=0`, `DefaultOpeningFloat=500.00`, `ExpiryWarningDays=14`, `MaxImageUploadBytes=2097152` (2MB).
- `<authorization><deny users="?" /></authorization>` at the root — everything requires login except `Default.aspx`, `Login.aspx`, `Error.aspx`, `AccessDenied.aspx`, and static asset folders.
- Per-folder `<location>` blocks gate `Cashier/` (Cashier+Manager), `Manager/` (Manager), `Supplier/` (Supplier), `Admin/` (Admin) — **enforced twice**, also in code (see §5).
- ⚠️ **Config drift to be aware of:** the live `Web.config` connection string points at `LAPTOP-B5HU784A\SQLEXPRESS`, a specific developer machine, while the README's documented example uses `(localdb)\MSSQLLocalDB`. Reset this before running on another machine.

---

## 3. Roles & demo credentials

Four roles (`Models\Enums.cs → RoleNames`): **Cashier, Manager, Supplier, Admin**.

| Role | Lands on | Can access |
|---|---|---|
| Cashier | POS Terminal | POS, My Sales, Cash-Up |
| Manager ("Store Manager") | Store Dashboard | Dashboard, Products, Categories, Inventory, Stock Movements, Suppliers, Purchase Orders, Cash-Up Review, Reports, **plus** POS (managers can also ring up sales) |
| Supplier | My Orders | Only their own company's purchase orders (enforced in `PurchaseOrderService`) |
| Admin | User Accounts | User Accounts, Role Permissions (docs page), Activity Log, System Audit |

**Demo logins** (from `README.md`, hashes match the seed script):

| Username | Password | Role |
|---|---|---|
| `a.booi` | `Cashier@123` | Cashier |
| `n.dlamini` | `Cashier@123` | Cashier |
| `a.right` | `Manager@123` | Manager |
| `t.jacobs` | `Manager@123` | Manager |
| `s.mkhize` | `Supplier@123` | Supplier (Amathole Fresh Produce) |
| `f.pillay` | `Supplier@123` | Supplier (Sunrise Wholesale) |
| `admin` | `Admin@123` | Admin |

**Login mechanics:** `Login.aspx` → `AuthService.SignIn` verifies via `PasswordHasher`, issues a Forms ticket whose `UserData` packs `userId|role|fullName|supplierId`. `Global.asax.cs` rebuilds the `IPrincipal` from that ticket on every request — no DB hit per request. `Services\CurrentUser.cs` is a static facade over the ticket data.

**Authorization enforced twice:**
1. `Web.config` `<location>` rules per folder.
2. Every secured page inherits `Services\SecurePageBase.cs`, which redirects unauthenticated users to Login and unauthorized ones to `AccessDenied.aspx` via an `AllowedRoles` override — "so a mis-edited config can never quietly expose a screen."

---

## 4. Navigation structure

- **`Site.master`** — public shell for `Default.aspx` (anonymous landing page): Features/Who it's for/How it works sections, CTA that changes based on sign-in state, footer credits the IFS303E/CoreLink project.
- **`App.master`** — signed-in app shell; sidebar built dynamically per role in `App.master.cs BuildNav(role)`:
  - **Cashier:** POS Terminal · My Sales · Cash-Up
  - **Manager:** Dashboard · *Catalogue* (Products, Categories) · *Stock* (Inventory, Stock Movements) · *Supply* (Suppliers, Purchase Orders) · *Money* (Cash-Up Review, Reports) · *Till* (POS Terminal)
  - **Supplier:** My Orders
  - **Admin:** User Accounts · Role Permissions · *Oversight* (System Activity, System Audit)

---

## 5. Page-by-page breakdown

**Root**
- `Default.aspx` — public landing/marketing page.
- `Login.aspx` — sign-in, redirects to each role's home page.
- `Error.aspx` / `AccessDenied.aspx` — error and 403 pages.

**Admin/**
- `Users.aspx` — create/edit/deactivate accounts, reset passwords, assign role/supplier.
- `RolePermissions.aspx` — read-only documentation of the role matrix (not itself enforcement — must be kept in sync by hand).
- `ActivityLog.aspx` — live cross-table activity timeline, filterable, CSV export.
- `SystemAudit.aspx` — a self-verification report of the build against a "System Matrix" (coverage + findings), backed by `Services\AuditReport.cs`.

**Cashier/**
- `POS.aspx` — scan/search products, build cart, take payment, complete sale.
- `Receipt.aspx` — view/print a completed sale.
- `MySales.aspx` — cashier's own sales history.
- `CashUp.aspx` — count drawer, close shift (expected cash always server-computed).

**Manager/**
- `Dashboard.aspx` — KPIs: revenue, low-stock count, open orders, unreviewed variances.
- `Products.aspx` / `ProductEdit.aspx` — catalogue list + create/edit form.
- `Categories.aspx` — category CRUD.
- `Suppliers.aspx` — supplier CRUD (lead time, contact info).
- `Inventory.aspx` — stock levels, manual adjustments, waste write-offs.
- `StockMovements.aspx` — full audit trail of every stock change.
- `PurchaseOrders.aspx` / `PurchaseOrderEdit.aspx` — create/manage POs; also where goods are received in.
- `CashUpReview.aspx` — manager sign-off queue for cash-up variances.
- `Reports.aspx` — top sellers, low stock, expiry/waste, supplier performance — each with date filter + CSV export.

**Supplier/**
- `Orders.aspx` — supplier portal: view orders, confirm/revise expected date, mark dispatched.

**Supporting**
- `Controls\` — `AlertBox`, `StatCard`, `StockStatusPill` reusable controls.
- `Handlers\ProductImage.ashx` — serves product image: uploaded blob → `ImageUrl` fallback → inline SVG placeholder.

---

## 6. Data model (13 tables, EF POCOs match SQL schema 1:1)

- **Role** → Users
- **User** — PBKDF2 hash/salt, RoleId, optional SupplierId (for supplier logins), IsActive, LastLoginAt
- **Category** → Products
- **Supplier** — lead time, contact info → Products, PurchaseOrders
- **Product** — barcode, cost/selling price, per-product VAT rate, reorder level, `QuantityOnHand` (service-layer authoritative), image (URL or blob)
- **StockBatch** — batch code, quantity received/remaining, expiry date, unit cost, linked PO — enables **FEFO** (first-expire-first-out) consumption
- **StockMovement** — every stock change (Sale/Adjustment/Receipt/Waste/VoidReversal), always tied to a UserId for traceability
- **PurchaseOrder** — `PO-#####`, status lifecycle: Draft → Sent → Confirmed → Dispatched → PartiallyDelivered/Delivered → (or Cancelled)
- **PurchaseOrderLine** — quantity ordered/received, unit cost
- **Shift** — cashier, till number, opening float, open/closed → Sales
- **Sale** — `RCT-#######`, subtotal/VAT/total, payment method, server-computed change due
- **SaleLine** — snapshots product name/price/VAT at time of sale (so later price changes don't rewrite history)
- **CashUp** — server-recomputed expected cash, variance, manager review fields

**Schema file:** `App_Data\SmartShelf_Schema.sql` (758 lines) — drops/recreates all tables, adds `CHECK (QuantityOnHand >= 0)`, unique indexes, and seeds roles/suppliers/7 users/categories/products, then **generates ~45 days of synthetic trading history** (movements, shifts, sales, corrections, batches, POs, cash-ups). Safe to re-run.

---

## 7. Services layer (all business logic lives here — pages only read forms and call services)

| Service | Responsibility |
|---|---|
| `AuthService` | Sign-in/out, Forms ticket issuance, role→landing-page map |
| `SecurePageBase` | Base `Page` enforcing sign-in + role check; shared money formatting/parsing |
| `CurrentUser` | Static reader of the signed-in identity from the ticket |
| `PasswordHasher` | PBKDF2 hash/verify |
| `CatalogService` | Product/category/supplier CRUD, barcode lookup, validation; products are deactivated, never deleted |
| `InventoryService` | Manual adjustments, waste write-offs, goods receipt, FEFO batch consumption, low-stock/expiry queries |
| `PurchaseOrderService` | Order lifecycle (create/send/confirm/dispatch/cancel), supplier-scoped access control |
| `SaleService` | Server-side cart pricing, `CompleteSale` (transactional: Sale + SaleLines + StockMovements), `VoidSale` |
| `CashUpService` | Shift summary, cash-up completion, manager review workflow |
| `ReportService` | Top sellers, low stock, expiry/waste, supplier performance, dashboard aggregates |
| `ActivityService` | Merges Sales/StockMovements/CashUps/PO events/logins into one timeline (documents its own blind spots) |
| `AuditReport` | Data backing the Admin self-audit page |
| `UserService` | Admin account management, password reset, safeguards against deactivating the last Admin |
| `CsvWriter` | Shared CSV export logic |
| `ServiceExceptions` | `BusinessRuleException`, `InsufficientStockException` |
| `StockStatus` | Single shared "low / reorder soon / healthy" status definition used across Dashboard, Products, Inventory, POS |

---

## 8. What's done vs. what's not

**Done — this reads as a complete, polished build:**
- No `TODO`/`FIXME`/`NotImplementedException`/stub pages/empty method bodies found anywhere in the codebase.
- Every service method is fully implemented with real transactions and validation.
- Two pages exist specifically to be honest about limitations rather than hide them (`Admin/SystemAudit.aspx`, `Admin/ActivityLog.aspx`).

**Known, documented limitations (from README §7 — deliberate, not bugs):**
1. The 45 days of seeded data is ordinary table data, not synthetic report output.
2. Product images are either a URL or an uploaded blob (2MB cap, signature-validated) — no third option.
3. In-process session state won't scale past a single server (would need `SQLServer` session mode for a web farm).
4. Culture is hard-pinned to `en-GB`.
5. `ActivityService` explicitly does not record sign-outs, failed logins, or account edits — "absence of an entry is not evidence that nothing happened."

**Minor loose ends:**
- Connection string in `Web.config` still points at a specific developer's SQL Express instance (`LAPTOP-B5HU784A\SQLEXPRESS`) rather than the README's documented default — reset before handing off to another machine/marker.
- One commented-out dead link in `Site.master` (line ~24) — cosmetic only.
- `Admin/RolePermissions.aspx` is a hand-maintained doc page, not live enforcement — must be manually kept in sync if roles/folders change.

**Version control note:** this folder is **not currently tracked by its own git repository** — the nearest `.git` root is the user's home directory and belongs to an unrelated project. There is no commit history to mine for SmartShelf specifically. Recommend running `git init` inside the SmartShelf project folder if history tracking is wanted going forward.

---

## 9. Recommended next steps

1. Initialize a dedicated git repo for this project (currently untracked).
2. Reset `Web.config`'s connection string to a portable default (or document the required change clearly) before running on a different machine.
3. Decide whether `Admin/RolePermissions.aspx` should be generated from the same source as the `Web.config`/`SecurePageBase` rules instead of hand-maintained, to remove the "must keep in sync manually" risk.
4. If sign-out/failed-login/account-edit auditing matters for the marking rubric, extend `ActivityService` to capture those events — currently an acknowledged gap.
