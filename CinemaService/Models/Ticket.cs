namespace CinemaService.Models
{
    public class Ticket
    {
        public long Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Cost { get; set; }
        public TicketState State { get; set; }

        public long SeatId { get; set; }
        public Seat Seat { get; set; }
        public long OrderId { get; set; }
        public Order Order { get; set; }
    }
}