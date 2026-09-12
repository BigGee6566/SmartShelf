using System;

namespace SmartShelf.Services
{
    /// <summary>
    /// A rule the user broke, phrased so the message can be shown to them directly.
    /// Anything that is *not* one of these is a bug and goes to the error page instead.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>Raised whenever a sale, adjustment or write-off would take stock below zero.</summary>
    public class InsufficientStockException : BusinessRuleException
    {
        public string ProductName { get; private set; }
        public int Available { get; private set; }
        public int Requested { get; private set; }

        public InsufficientStockException(string productName, int available, int requested)
            : base(string.Format(
                "Not enough stock for {0}: {1} on hand but {2} requested. The sale was not saved.",
                productName, available, requested))
        {
            ProductName = productName;
            Available = available;
            Requested = requested;
        }
    }
}
