using Microsoft.EntityFrameworkCore;

namespace CinemaService.Models
{
    public class CinemaContext : DbContext
    {
        public CinemaContext(DbContextOptions<CinemaContext> options) : base(options) 
        {
        }

        public DbSet<Country> Country { get; set; }
        public DbSet<Genre> Genre { get; set; }
        public DbSet<Hall> Hall { get; set; }
        public DbSet<Movie> Movie { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<Seat> Seat { get; set; }
        public DbSet<SeatType> SeatType { get; set; }
        public DbSet<Session> Session { get; set; }
        public DbSet<Theatre> Theatre { get; set; }
        public DbSet<Ticket> Ticket { get; set; }
        public DbSet<User> User { get; set; }
    }
}
