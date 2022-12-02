using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaService.Controllers
{
    /// <summary>
    /// Controller for manager control panel 
    /// </summary>
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ILogger<ManagerController> _logger;
        private readonly CinemaContext _context;

        public ManagerController(ILogger<ManagerController> logger, CinemaContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Index page for manager control panel
        /// </summary>
        /// <returns>Index page</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Page to add a new movie 
        /// </summary>
        /// <returns>Movie form page</returns>
        [HttpGet]
        public IActionResult AddMovie()
        {
            return View(
                new MovieView()
                {
                    Genres = _context.Genre.ToList()
                }
            );
        }

        /// <summary>
        /// POST-method to add a movie
        /// </summary>
        /// <param name="movieView">Movie view model</param>
        /// <returns>Redirect to manager control panel</returns>
        /// <exception cref="ArgumentNullException"></exception>
        [HttpPost]
        public IActionResult AddMovie(MovieView movieView)
        {
            if (movieView is null) throw new ArgumentNullException();
            try
            {
                _context.Movie.Add(new Movie()
                {
                    Title = movieView.Title,
                    Director = movieView.Director,
                    Description = movieView.Description,
                    Year = movieView.Year.GetValueOrDefault(),
                    Length = movieView.Length.GetValueOrDefault(),
                    Genres = _context.Genre.Where(g => movieView.ChosenIds.Contains(g.Id)).ToArray()
                });
                _context.SaveChanges();
                return Redirect("/Manager/Index");
            }
            catch (InvalidOperationException e)
            {
                _logger.LogError(e.ToString());
                return Redirect("/Cinema/Error");
            }
        }

        [HttpGet]
        public IActionResult AddSession()
        {
            return View();
        }
    }
}
