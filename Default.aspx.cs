using System;
using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf
{
    /// <summary>
    /// Public landing page. The headline figures are read from the live database rather than
    /// hard-coded, so the site never advertises numbers the system cannot back up.
    /// </summary>
    public partial class Default : Page
    {
        protected string ProductCount { get; private set; }
        protected string SaleCount { get; private set; }
        protected string CategoryCount { get; private set; }
        protected string SupplierCount { get; private set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                ProductCount = ReportService.ActiveProductCount().ToString("N0");
                CategoryCount = ReportService.CategoryCount().ToString("N0");
                SaleCount = ReportService.SaleCountBetween(DateTime.Today.AddYears(-1), DateTime.Today).ToString("N0");
                SupplierCount = CatalogService.GetSuppliers(true).Count.ToString("N0");
            }
            catch (Exception)
            {
                // The marketing page must still render if the database is unreachable - a visitor
                // seeing an error page here would be far worse than seeing a dash.
                ProductCount = CategoryCount = SaleCount = SupplierCount = "–";
            }
        }
    }
}
