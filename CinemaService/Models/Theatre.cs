namespace CinemaService.Models
{
    public class Theatre
    {
        public long Id { get; set; }
        public string City { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Hall> Halls { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }
}
