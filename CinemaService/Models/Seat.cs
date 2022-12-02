namespace CinemaService.Models
{
    public class Seat
    {
        public long Id { get; set; }
        public short Row { get; set; }
        public short Number { get; set; }

        public long HallId { get; set; }
        public Hall Hall { get; set; }
        public long TypeId { get; set; }
        public SeatType Type { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}