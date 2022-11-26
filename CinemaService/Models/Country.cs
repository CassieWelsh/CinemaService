namespace CinemaService.Models
{
    public class Country
    {
        public short Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<Movie> Movies { get; set; }
    }
}
