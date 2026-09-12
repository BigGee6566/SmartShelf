using System.Web.UI;

namespace SmartShelf.Controls
{
    /// <summary>One headline figure on a dashboard. Used instead of copy-pasting the card markup.</summary>
    public partial class StatCard : UserControl
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Delta { get; set; }

        /// <summary>"up" tints the delta green, "down" tints it the accent orange.</summary>
        public string Direction { get; set; }

        /// <summary>Set to "alert" to render the figure itself in the warning colour.</summary>
        public bool Highlight { get; set; }

        protected string DeltaCss
        {
            get { return Direction == "up" ? "up" : Direction == "down" ? "down" : "muted"; }
        }

        protected string NumberStyle
        {
            get { return Highlight ? "color:var(--color-accent-dark);" : string.Empty; }
        }
    }
}
