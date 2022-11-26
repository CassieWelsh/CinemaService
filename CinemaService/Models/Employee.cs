namespace CinemaService.Models
{
    public class Employee : User
    {
        public EmployeeRole Role { get; set; }

        public long? TheatreId { get; set; }
        public virtual Theatre Theatre { get; set; }
    }
}
