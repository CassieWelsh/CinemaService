namespace CinemaService.Models
{
    public class Hall
    {
        public long Id { get; set; }
        public int Number { get; set; }
        public int Rows { get; set; }
        public int SeatsPerRow { get; set; }

        public virtual ICollection<Session> Sessions { get; set; }
        public virtual ICollection<Seat> Seats { get; set; }
        public long TheatreId { get; set; }
        public virtual Theatre Theatre { get; set; }
    }
}