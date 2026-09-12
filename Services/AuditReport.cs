using System;
using System.Collections.Generic;
using System.Linq;
using SmartShelf.Models;

namespace SmartShelf.Services
{
    /// <summary>One row of the specification-matrix coverage check.</summary>
    public class CoverageRow
    {
        public string UseCase { get; set; }
        public string Screen { get; set; }
        public string Implemented { get; set; }
        public string Tables { get; set; }
        public string ScreenMatch { get; set; }
        public string ReportWorks { get; set; }
        public string Notes { get; set; }

        /// <summary>True when all four checks are a clean yes.</summary>
        public bool IsClean
        {
            get
            {
                return Implemented == Yes && Tables == Yes && ScreenMatch == Yes && ReportWorks == Yes;
            }
        }

        public const string Yes = "Yes";
        public const string No = "No";
        public const string Partial = "Partial";

        public static string CssFor(string verdict)
        {
            switch (verdict)
            {
                case Yes: return "pill-green";
                case Partial: return "pill-amber";
                case No: return "pill-red";
                default: return "pill-grey";
            }
        }
    }

    /// <summary>One verification finding, in the format the audit brief asks for.</summary>
    public class AuditFinding
    {
        public string Severity { get; set; }
        public string Where { get; set; }
        public string What { get; set; }
        public string Evidence { get; set; }
        public int Confidence { get; set; }
        public string SuggestedFix { get; set; }
        public bool Fixed { get; set; }

        public const string Critical = "Critical";
        public const string Major = "Major";
        public const string Minor = "Minor";
        public const string Polish = "Polish";

        public static string CssFor(string severity)
        {
            switch (severity)
            {
                case Critical: return "pill-red";
                case Major: return "pill-amber";
                case Minor: return "pill-blue";
                default: return "pill-grey";
            }
        }
    }

    /// <summary>Live row counts, so the page cannot claim a healthy system it has not checked.</summary>
    public class SystemFacts
    {
        public int Roles { get; set; }
        public int Users { get; set; }
        public int Categories { get; set; }
        public int Suppliers { get; set; }
        public int Products { get; set; }
        public int StockBatches { get; set; }
        public int StockMovements { get; set; }
        public int PurchaseOrders { get; set; }
        public int PurchaseOrderLines { get; set; }
        public int Shifts { get; set; }
        public int Sales { get; set; }
        public int SaleLines { get; set; }
        public int CashUps { get; set; }
        public bool Reachable { get; set; }
    }

    /// <summary>
    /// The verification audit carried out against the System Matrix, the System Overview and the
    /// use case diagram, published inside the application so the group can read it where the
    /// system actually runs.
    /// <para>
    /// The coverage rows and findings below are the auditor's conclusions and are deliberately
    /// static - they describe a point-in-time review. The <see cref="GetFacts"/> panel beside them
    /// is queried live, so a reader can always tell whether the system still looks as it did when
    /// this was written.
    /// </para>
    /// </summary>
    public static class AuditReport
    {
        public static readonly DateTime ReviewedOn = new DateTime(2026, 8, 5);

        public static SystemFacts GetFacts()
        {
            try
            {
                using (var db = new SmartShelfContext())
                {
                    return new SystemFacts
                    {
                        Reachable = true,
                        Roles = db.Roles.Count(),
                        Users = db.Users.Count(),
                        Categories = db.Categories.Count(),
                        Suppliers = db.Suppliers.Count(),
                        Products = db.Products.Count(),
                        StockBatches = db.StockBatches.Count(),
                        StockMovements = db.StockMovements.Count(),
                        PurchaseOrders = db.PurchaseOrders.Count(),
                        PurchaseOrderLines = db.PurchaseOrderLines.Count(),
                        Shifts = db.Shifts.Count(),
                        Sales = db.Sales.Count(),
                        SaleLines = db.SaleLines.Count(),
                        CashUps = db.CashUps.Count()
                    };
                }
            }
            catch (Exception)
            {
                return new SystemFacts { Reachable = false };
            }
        }

        // ------------------------------------------------------------------
        // Matrix coverage - one row per use case in the System Matrix
        // ------------------------------------------------------------------
        public static List<CoverageRow> Coverage()
        {
            const string Y = CoverageRow.Yes, N = CoverageRow.No, P = CoverageRow.Partial;

            return new List<CoverageRow>
            {
                new CoverageRow { UseCase = "UC1 Register", Screen = "SC1 Register Screen",
                    Implemented = N, Tables = N, ScreenMatch = N, ReportWorks = N,
                    Notes = "No public sign-up page exists. Accounts are created by an Admin on Admin/Users.aspx. " +
                            "No SecurityRole or RolePermission table; one Role table. No registration audit log." },

                new CoverageRow { UseCase = "UC2 Login", Screen = "SC1 Login Screen",
                    Implemented = Y, Tables = Y, ScreenMatch = P, ReportWorks = N,
                    Notes = "Login.aspx works, PBKDF2-verified, one login screen for all four roles. Reads User + Role. " +
                            "The Matrix gives SC1 to both Register and Login - two different screens cannot share a code. " +
                            "No user audit log report; only User.LastLoginAt is stored." },

                new CoverageRow { UseCase = "UC3 Forgot password", Screen = "SC3 Password Reset",
                    Implemented = N, Tables = N, ScreenMatch = N, ReportWorks = N,
                    Notes = "No recovery flow at all. Login.aspx tells the user to ask an administrator. " +
                            "No ForgotPassword table, no reset log. UserService.ResetPassword exists but no page calls it." },

                new CoverageRow { UseCase = "UC4 Process sale", Screen = "SC2 POS Terminal",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "SaleService.CompleteSale writes Sale + SaleLine in one transaction, re-pricing every line " +
                            "from the database. Payment is columns on Sale rather than a separate Payment table." },

                new CoverageRow { UseCase = "UC5 Search product", Screen = "SC3 Product Search",
                    Implemented = Y, Tables = Y, ScreenMatch = P, ReportWorks = P,
                    Notes = "Search works on the POS, Products and Inventory screens via CatalogService.SearchProducts. " +
                            "There is no dedicated Product Search screen, and the Matrix reuses SC3 for Password Reset." },

                new CoverageRow { UseCase = "UC6 Scan product", Screen = "SC2 POS Terminal",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "CatalogService.FindByBarcode does an exact barcode match from the POS search box. " +
                            "Barcode is a unique column on Product, not a separate Barcode table." },

                new CoverageRow { UseCase = "UC7 Receive payment", Screen = "SC4 Payment Modal",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = N,
                    Notes = "Cash/card/mobile, change calculated server-side and never trusted from the browser. " +
                            "No Payment or PaymentType table, and no Payment Breakdown Report anywhere. Split payment is not supported." },

                new CoverageRow { UseCase = "UC8 Print receipt", Screen = "SC2 POS Terminal",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "Cashier/Receipt.aspx renders a print-friendly slip from the Sale and its lines. " +
                            "No Receipt table - the receipt number is a column on Sale. Reprints are not logged." },

                new CoverageRow { UseCase = "UC9 Update stock", Screen = "SC5 Stock Update",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "InventoryService.AdjustStock refuses negatives, demands a reason, and writes a StockMovement " +
                            "naming the user. Manager/StockMovements.aspx is the adjustment log." },

                new CoverageRow { UseCase = "UC10 Perform cash-up", Screen = "SC6 Cash-up Form",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "Expected cash is recomputed server-side from that shift's completed cash sales; only the " +
                            "counted figure is accepted from the browser. Opening float comes from configuration, not a count." },

                new CoverageRow { UseCase = "UC11 View inventory levels", Screen = "SC7 Inventory Dashboard",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "Manager/Inventory.aspx lists live levels from Product, with movement history behind it." },

                new CoverageRow { UseCase = "UC12 Monitor low stock alert", Screen = "SC7 Inventory Dashboard",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "ReportService.LowStockAlerts drives both the dashboard tile and the report, verified equal to " +
                            "direct SQL. Thresholds are Product.ReorderLevel per product - there is no AlertConfig table." },

                new CoverageRow { UseCase = "UC13 Track expiry date", Screen = "SC8 Expiry Tracking",
                    Implemented = Y, Tables = P, ScreenMatch = N, ReportWorks = Y,
                    Notes = "ReportService.ExpiringBatches reads StockBatch, and waste comes from StockMovement. " +
                            "No dedicated expiry screen - it is a tab on Reports. No ExpiryLog table (ProductBatch is StockBatch)." },

                new CoverageRow { UseCase = "UC14 Update product catalogue", Screen = "SC9 Catalogue Manager",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "Products and Categories screens cover create, edit, price, categorise and deactivate. " +
                            "Stock is deliberately not settable here so every change keeps an audit trail." },

                new CoverageRow { UseCase = "UC15 Place supplier order", Screen = "SC10 Supplier Order Form",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "PurchaseOrderService.CreateOrder writes PurchaseOrder + PurchaseOrderLine. " +
                            "The Matrix's OrderItem is PurchaseOrderLine." },

                new CoverageRow { UseCase = "UC16 View reports", Screen = "SC11 Management Reports",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "All four key reports run live off real data and export to CSV; top sellers verified to the cent " +
                            "against SQL. The Matrix says this touches CashUp - it does not. Cash-up data lives on its own screen." },

                new CoverageRow { UseCase = "UC17 Run reconciliation", Screen = "SC12 Reconciliation",
                    Implemented = Y, Tables = P, ScreenMatch = P, ReportWorks = Y,
                    Notes = "Manager/CashUpReview.aspx is the reconciliation screen and records who signed off what. " +
                            "It reads only the stored CashUp row, so a manager cannot drill from a variance into the sales " +
                            "behind it. No Ledger or Reconciliation table." },

                new CoverageRow { UseCase = "UC18 View supplier orders", Screen = "SC13 Supplier Portal",
                    Implemented = Y, Tables = Y, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "Supplier/Orders.aspx is scoped to the signed-in supplier's own company and hides drafts. " +
                            "Verified a supplier cannot reach another supplier's order." },

                new CoverageRow { UseCase = "UC19 Confirm supplier order", Screen = "SC13 Supplier Portal",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = Y,
                    Notes = "ConfirmOrder sets status, timestamp and note, and the manager sees it immediately. " +
                            "Status is a column, not an OrderStatus table." },

                new CoverageRow { UseCase = "UC20 Update delivery status", Screen = "SC14 Delivery Tracker",
                    Implemented = Y, Tables = P, ScreenMatch = N, ReportWorks = P,
                    Notes = "Fixed during this audit: marking an order dispatched previously changed nothing the manager " +
                            "could see. There is no Delivery table and no dedicated tracker screen - delivery lives on the " +
                            "order itself, split across the supplier portal and the purchase order screen." },

                new CoverageRow { UseCase = "UC21 Manage system users", Screen = "SC15 User Management",
                    Implemented = Y, Tables = P, ScreenMatch = Y, ReportWorks = P,
                    Notes = "Admin/Users.aspx creates users, assigns roles, resets passwords and deactivates accounts, and " +
                            "refuses to strand the last administrator. RolePermissions.aspx is hard-coded, not read from a " +
                            "RolePermission table. The security log this row promises is the new System Activity screen." }
            };
        }

        // ------------------------------------------------------------------
        // Findings
        // ------------------------------------------------------------------
        public static List<AuditFinding> Findings()
        {
            return new List<AuditFinding>
            {
                new AuditFinding
                {
                    Severity = AuditFinding.Critical,
                    Where = "UC1 Register / SC1 Register Screen",
                    What = "The use case is entirely absent. There is no public sign-up page, and no anonymous route to one.",
                    Evidence = "Web.config denies anonymous users everywhere except Default, Login, Error, AccessDenied and the " +
                               "static asset folders. No Register.aspx exists. Site.master and Default.aspx link only to Login.aspx.",
                    Confidence = 100,
                    SuggestedFix = "Either build a Register screen and open it to anonymous users, or amend the Matrix to record " +
                                   "that account creation is administrator-only by design - which is the safer choice for a store."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Critical,
                    Where = "UC3 Forgot password / SC3 Password Reset Screen",
                    What = "No password recovery exists. A locked-out user has no self-service route back in.",
                    Evidence = "Login.aspx reads \"Forgotten your password? Ask an administrator to reset it.\" There is no " +
                               "ForgotPassword table, no token, no reset page. UserService.ResetPassword is never called by any page.",
                    Confidence = 100,
                    SuggestedFix = "Build a reset flow, or amend the Matrix to describe the administrator-driven reset that " +
                                   "actually exists and wire Admin/Users.aspx to UserService.ResetPassword explicitly."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "Supplier/Orders.aspx - mark as dispatched",
                    What = "A supplier marking an order dispatched changed nothing the store could see, and silently " +
                           "overwrote the date the store had recorded for the delivery.",
                    Evidence = "Confirmed live: after marking PO-01014 dispatched, the database still read Status=2 (Confirmed) " +
                               "while DeliveredAt had been set to the current time. The manager's list was unchanged. " +
                               "PurchaseOrderService.UpdateDeliveryStatus never assigned order.Status.",
                    Confidence = 100,
                    Fixed = true,
                    SuggestedFix = "Fixed: added a Dispatched status which the supplier's action now sets, and stopped writing " +
                                   "DeliveredAt from the supplier side so it keeps meaning \"the store counted this in\"."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "Cashier/CashUp.aspx - submitting twice",
                    What = "Submitting the cash-up form a second time produced an unhandled exception and a server error page.",
                    Evidence = "Confirmed live: replaying the submit POST returned HTTP 500 with an empty body. The event log gives " +
                               "the cause - a NullReferenceException in UpdateVariancePreview (CashUp.aspx.cs:77), reached from the " +
                               "txtCounted TextChanged handler, which still fires on the repeated post after Page_Load has set the " +
                               "shift to null. Only one cash-up row was ever created, so no data was corrupted.",
                    Confidence = 100,
                    Fixed = true,
                    SuggestedFix = "Fixed: the variance preview now returns early when there is no open shift, which covers every " +
                                   "caller, and the submit handler reports \"This shift has already been cashed up\" rather than failing."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "Supplier/Orders.aspx - revised delivery date",
                    What = "An unreadable delivery date was discarded in silence. The supplier was told the order was confirmed, " +
                           "while the date they typed never reached the store.",
                    Evidence = "Confirmed live: confirming with the date \"not-a-date-at-all\" returned \"Order confirmed. The store " +
                               "can see your commitment\" while ExpectedDate stayed unchanged. DateTime.TryParse failed and the " +
                               "null result was treated as \"no change requested\".",
                    Confidence = 100,
                    Fixed = true,
                    SuggestedFix = "Fixed: a non-empty date that will not parse is now rejected with a message, and dates more " +
                                   "than two years out are refused."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "Manager/Reports.aspx and Manager/StockMovements.aspx - date filters",
                    What = "Unreadable dates in the filter boxes were ignored without warning, so the screen showed a different " +
                           "period than the one displayed in the filter.",
                    Evidence = "Confirmed live: running the top-sellers report with \"garbage\" and \"!!!\" in the date boxes returned " +
                               "HTTP 200 with 30 rows of last-30-days data and no message at all.",
                    Confidence = 100,
                    Fixed = true,
                    SuggestedFix = "Fixed: an empty box still means \"use the default\", but anything present that will not parse " +
                                   "now stops the query and explains itself."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "UC16 View reports - Matrix row",
                    What = "The Matrix says this use case touches Sale, InventoryStock and CashUp. The reports screen never " +
                           "reads CashUp, so no report reflects cash-up data.",
                    Evidence = "ReportService.cs contains no reference to CashUps in any of its nine methods, and Reports.aspx.cs " +
                               "never calls CashUpService. Cash-up figures appear only on the dashboard tiles and CashUpReview.aspx.",
                    Confidence = 100,
                    SuggestedFix = "Either add a cash-up summary tab to the reports screen, or correct the Matrix row to list only " +
                                   "the tables the report genuinely uses."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Major,
                    Where = "Services/SaleService.cs - opening float",
                    What = "The till float that feeds every expected-cash figure is an assumption, not a count. Nobody is ever " +
                           "asked what was actually in the drawer at the start of a shift.",
                    Evidence = "GetOrOpenShift created every shift with a literal R500 float, and all 92 seeded shifts carry exactly " +
                               "500.00. That number is added to expected cash at cash-up, so any real-world difference becomes a variance " +
                               "the cashier is accountable for but did not cause.",
                    Confidence = 95,
                    Fixed = true,
                    SuggestedFix = "Partly fixed: the amount now comes from the DefaultOpeningFloat setting in Web.config rather than " +
                                   "a literal. A complete fix needs a shift-open step where the cashier counts and confirms the float."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Minor,
                    Where = "System Matrix - screen codes",
                    What = "SC1 and SC3 are each used for two different screens, so the codes cannot uniquely identify a screen.",
                    Evidence = "SC1 is both \"Register Screen\" (UC1) and \"Login Screen\" (UC2). SC3 is both \"Password Reset Screen\" " +
                               "(UC3) and \"Product Search\" (UC5). This is a contradiction inside the Matrix itself, not a code defect.",
                    Confidence = 100,
                    SuggestedFix = "Renumber so each screen has one code. This is a documentation fix - no code change is warranted."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Minor,
                    Where = "Use Case Diagram vs System Matrix - numbering",
                    What = "The diagram's numbering runs one ahead of the Matrix from \"Monitor low stock alert\" onward, because " +
                           "the diagram has no UC12.",
                    Evidence = "Diagram UC13 \"Monitor low stock alert\" is Matrix UC12; diagram UC22 \"Manager system users\" is " +
                               "Matrix UC21. The code and its comments use neither numbering - they refer to screens and focus areas - " +
                               "so no code is affected.",
                    Confidence = 100,
                    SuggestedFix = "Correct the diagram to match the Matrix. Fixing the code here would be fixing the wrong artifact."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Minor,
                    Where = "Admin/RolePermissions.aspx",
                    What = "The permission matrix is a hard-coded list in the page's code-behind, not data. If the Web.config rules " +
                           "change, this screen will keep showing the old ones.",
                    Evidence = "RolePermissions.aspx.cs binds an in-code array and reads no table. Its own comment admits it \"mirrors " +
                               "the <location> rules in Web.config ... if those change, update this list with them.\"",
                    Confidence = 100,
                    SuggestedFix = "Either introduce the RolePermission table the Matrix names, or label the screen clearly as a " +
                                   "reference summary of the configured rules rather than a live permission store."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Minor,
                    Where = "Cashier/MySales.aspx seen by a Store Manager",
                    What = "A manager opening My Sales sees an empty screen, because it filters to the signed-in user's own sales " +
                           "and managers rarely ring anything up.",
                    Evidence = "Confirmed live: signed in as a.right the page rendered 0 rows, which matches the 0 sales that user has. " +
                               "Nothing leaks, but the screen gives a manager no reason for the emptiness.",
                    Confidence = 90,
                    SuggestedFix = "Either hide the link for managers, or add a cashier picker for managers so the screen answers a " +
                                   "question a manager would actually ask."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Minor,
                    Where = "UC17 Run reconciliation - Manager/CashUpReview.aspx",
                    What = "A manager reviewing a variance cannot see the sales that produced the expected figure, so sign-off is " +
                           "an act of trust rather than an investigation.",
                    Evidence = "CashUpService.GetCashUps and ReviewCashUp read only the CashUp row. db.Sales is never touched on the " +
                               "review path; ExpectedCash is frozen at the moment of cash-up.",
                    Confidence = 95,
                    SuggestedFix = "Add a drill-down from a variance to that shift's sales list, so the reviewer can check the figure " +
                                   "rather than accept it."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Polish,
                    Where = "Supplier/Orders.aspx - txtRevisedDate",
                    What = "The date box declares ValidationGroup=\"Confirm\" but no validator exists in that group anywhere in the file, " +
                           "so the attribute does nothing.",
                    Evidence = "The file contains no validator control of any kind. ValidationGroup on a TextBox is inert without one.",
                    Confidence = 100,
                    SuggestedFix = "Add a CompareValidator of type Date to that group, or drop the attribute. The server-side check " +
                                   "added during this audit already prevents bad data reaching the database."
                },
                new AuditFinding
                {
                    Severity = AuditFinding.Polish,
                    Where = "Services/UserService.cs",
                    What = "ResetPassword is dead code - no page calls it. Password resets go through SaveUser instead.",
                    Evidence = "A repository-wide search for ResetPassword finds the definition and no call site.",
                    Confidence = 100,
                    SuggestedFix = "Either wire the Admin users screen to it for an explicit reset action, or remove it."
                }
            };
        }

        /// <summary>What the audit checked and found sound, so the report is not only a list of complaints.</summary>
        public static List<string> Verified()
        {
            return new List<string>
            {
                "Report arithmetic is exact. The top-selling products report matched direct SQL to the cent " +
                "(Wicker Basket Fruit Mix, 277 units, R 12,465.00), and the low-stock count matched at 5 products.",

                "Role separation holds in every direction. All sixteen combinations of four roles against five " +
                "protected areas behaved correctly - every unauthorised URL landed on Access Denied.",

                "A sale's totals reconcile. Subtotal, VAT and total are recomputed from the database at the moment " +
                "of payment, and a browser-supplied total is never persisted.",

                "Stock cannot go negative. Sales, adjustments and write-offs all refuse to take a product below zero, " +
                "and each change writes a StockMovement naming the staff member responsible.",

                "Paying twice does not charge twice. Replaying the confirm-payment POST created exactly one sale; " +
                "the second attempt was told the cart was empty.",

                "Bad input is refused without corrupting anything. Negative, non-numeric and 400-character inputs on " +
                "the stock adjustment form all left the quantity untouched.",

                "No password, hash or salt is ever rendered. Both password boxes are write-only and are cleared on " +
                "every postback.",

                "A cashier cannot see another cashier's sales. My Sales filters in SQL on the signed-in user, and the " +
                "receipt page checks ownership before it binds anything.",

                "The cart does not survive sign-out. Signing out and back in produced an empty till.",

                "Control naming is disciplined throughout. A repository-wide search for default names such as TextBox1 " +
                "or Button1 returned nothing; every control uses a typed prefix.",

                "There is exactly one login screen for all four roles, as the System Overview requires."
            };
        }
    }
}
