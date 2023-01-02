namespace CinemaService.Models
{
    public class Order
    {
        public long Id { get; set; }
        public OrderState State { get; set; }
        public DateTime PurchaseDate { get; set; }

        public long? UserId { get; set; }
        public User? User { get; set; }
        public long SessionId { get; set; }
        public Session Session { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}