namespace SmartShelf.Services
{
    /// <summary>
    /// One definition of what "low", "reorder soon" and "healthy" mean, so the dashboard, the
    /// product list, the inventory grid and the POS all agree.
    /// </summary>
    public static class StockStatus
    {
        public static string CssFor(int onHand, int reorderLevel)
        {
            if (onHand <= reorderLevel) return "pill-red";
            // Within half a reorder level of the threshold: worth ordering before it bites.
            if (onHand <= reorderLevel * 1.5) return "pill-amber";
            return "pill-green";
        }

        public static string TextFor(int onHand, int reorderLevel)
        {
            if (onHand <= 0) return "Out of stock";
            if (onHand <= reorderLevel) return "Low stock";
            if (onHand <= reorderLevel * 1.5) return "Reorder soon";
            return "Healthy";
        }
    }
}
