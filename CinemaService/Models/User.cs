namespace CinemaService.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateOnly Birthdate { get; set; }
        public DateTime RegisterDate { get; set; }
        public UserRole Role { get; set; }

        public virtual ICollection<Order> Orders { get; set; }

        public long? TheatreId { get; set; }
        public virtual Theatre? Theatre { get; set; }
    }
}