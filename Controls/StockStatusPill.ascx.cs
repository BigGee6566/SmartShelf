using System.Web.UI;
using SmartShelf.Services;

namespace SmartShelf.Controls
{
    /// <summary>
    /// Renders the shared stock-status pill. The thresholds themselves live in
    /// <see cref="StockStatus"/> so grids that build their own markup stay consistent with this.
    /// </summary>
    public partial class StockStatusPill : UserControl
    {
        public int QuantityOnHand { get; set; }
        public int ReorderLevel { get; set; }

        protected string PillCss { get { return StockStatus.CssFor(QuantityOnHand, ReorderLevel); } }
        protected string PillText { get { return StockStatus.TextFor(QuantityOnHand, ReorderLevel); } }
    }
}
