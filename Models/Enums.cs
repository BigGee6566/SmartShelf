namespace SmartShelf.Models
{
    /// <summary>Why a product's stock level changed. Every change writes one of these.</summary>
    public enum MovementType
    {
        Sale = 1,
        Adjustment = 2,
        Receipt = 3,
        Waste = 4,
        VoidReversal = 5
    }

    /// <summary>Lifecycle of a purchase order, from the manager raising it to goods received.</summary>
    public enum PurchaseOrderStatus
    {
        Draft = 0,
        Sent = 1,
        Confirmed = 2,
        PartiallyDelivered = 3,
        Delivered = 4,
        Cancelled = 5,

        /// <summary>
        /// The supplier says the goods have left them. Distinct from Delivered, which only the
        /// store can declare by counting the delivery in - the two are separate facts and the
        /// store's count is the one that moves stock.
        /// </summary>
        Dispatched = 6
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Card = 2,
        Mobile = 3
    }

    public enum SaleStatus
    {
        Completed = 1,
        Voided = 2
    }

    public enum ShiftStatus
    {
        Open = 1,
        Closed = 2
    }

    /// <summary>Role names are also the strings used in Forms auth tickets and Web.config.</summary>
    public static class RoleNames
    {
        public const string Cashier = "Cashier";
        public const string Manager = "Manager";
        public const string Supplier = "Supplier";
        public const string Admin = "Admin";
    }

    /// <summary>EventType values written to SecurityEvent by SecurityEventService.</summary>
    public static class SecurityEventTypes
    {
        public const string SignIn = "SignIn";
        public const string SignInFailed = "SignInFailed";
        public const string SignOut = "SignOut";
        public const string AccountCreated = "AccountCreated";
        public const string AccountUpdated = "AccountUpdated";
        public const string PasswordReset = "PasswordReset";
        public const string AccountActivated = "AccountActivated";
        public const string AccountDeactivated = "AccountDeactivated";
    }
}
