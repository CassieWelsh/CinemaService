namespace CinemaService.Models
{
    public class Session
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public bool Is3d {get; set; }

        public long MovieId { get; set; }
        public Movie Movie { get; set; }
        public long HallId { get; set; }
        public Hall Hall { get; set; }
        public long UserId { get; set; } 
        public User User { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}