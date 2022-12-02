namespace CinemaService.Models
{
    public class Movie
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Director { get; set; }
        public int Year { get; set; }
        public string? Description { get; set; }
        public int Length { get; set; }

        public virtual ICollection<Genre> Genres { get; set; }
        public virtual ICollection<Country> Countries { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }
    }
}