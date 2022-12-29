using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using CinemaService.Models.ViewModel.OrderViews;

namespace CinemaService.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ILogger<CinemaController> _logger;
        private readonly CinemaContext _context;

        public CinemaController(ILogger<CinemaController> logger, CinemaContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            var movies = _context.Session.Include(s => s.Movie).Select(s => s.Movie).AsEnumerable().DistinctBy(m => m.Id);
            return View(new IndexPageView() { Movies = movies });
        }

        [HttpGet]
        [Route("/Cinema/SessionList/{movieId}")]
        public IActionResult SessionList(long movieId)
        {
            var sessions = _context.Session.Where(s => s.MovieId == movieId && DateTime.Now.ToUniversalTime() < s.Date);
            var movie = _context.Movie.Single(m => m.Id == movieId);
            return View(new SessionListView() { Movie = movie, Sessions = sessions });
        }

        [HttpGet]
        [Route("/Cinema/Order/{sessionId}")]
        public IActionResult Order(long sessionId)
        {
            var movie = _context.Session
                .Where(s => s.Id == sessionId)
                .Include(s => s.Movie)
                .Select(s => s.Movie)
                .Single();
            
            var session = _context.Session
                .Include(s => s.Hall)
                .Single(s => s.Id == sessionId);

            var seats = _context.Seat
                .Where(s => s.HallId == session.HallId)
                .Select(s => new SeatView()
                {
                    Seat = s,
                    Available = !_context.Ticket
                        .Include(t => t.Seat)
                        .Any(t => t.Seat.Number == s.Number && t.Seat.Row == s.Row)
                })
                .ToList();

            return View(new OrderView()
            {
                Movie = movie,
                Session = session,
                Seats = seats
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}