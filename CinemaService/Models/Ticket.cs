namespace CinemaService.Models
{
    public class Ticket
    {
        public long Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Cost { get; set; }

        public long SeatId { get; set; }
        public virtual Seat Seat { get; set; }
        public long OrderId { get; set; }
        public virtual Order Order { get; set; }
    }
}