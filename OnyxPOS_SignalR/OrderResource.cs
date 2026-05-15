namespace OnyxPOS_SignalR
{
    public class OrderResource
    {
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        public int LocationId { get; set; }

        public int ResourceTypeId { get; set; }

        public int ResourceId { get; set; }

        public DateTime? CheckoutOn { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
