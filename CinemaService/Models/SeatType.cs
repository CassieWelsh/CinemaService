namespace CinemaService.Models
{
    public class SeatType
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Cost2d { get; set; }
        public decimal Cost3d { get; set; }

        public virtual ICollection<Seat> Seats { get; set; }
    }
}