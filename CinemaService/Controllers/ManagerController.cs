using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CinemaService.Models.ViewModel.DurationModels;
using Microsoft.EntityFrameworkCore;

namespace CinemaService.Controllers;

/// <summary>
/// Controller for manager control panel 
/// </summary>
[Authorize(Roles = "Manager")]
public class ManagerController : Controller
{
    private readonly ILogger<ManagerController> _logger;
    private readonly CinemaContext _context;

    /// <summary>
    /// Creates an instance of <see cref="ManagerController"/>.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="context">Derived Entity framework class of <see cref="CinemaContext"/> type.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="context"/> or <paramref name="logger"/> is null.</exception>
    public ManagerController(ILogger<ManagerController> logger, CinemaContext context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Index page for manager control panel.
    /// </summary>
    /// <returns>Index page.</returns>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Page to add a new movie.
    /// </summary>
    /// <returns>Movie form page.</returns>
    [HttpGet]
    public IActionResult AddMovie()
    {
        return View(
            new MovieView()
            {
                Genres = _context.Genre.ToList(),
                Countries = _context.Country.ToList()
            }
        );
    }

    /// <summary>
    /// POST-method to add a movie.
    /// </summary>
    /// <param name="movieView">Movie view model.</param>
    /// <returns>Redirect to manager control panel.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="movieView"/> is null.</exception>
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
                Genres = _context.Genre.Where(g => movieView.ChosenGenreIds.Contains(g.Id)).ToArray(),
                Countries = _context.Country.Where(c => movieView.ChosenCountryIds.Contains(c.Id)).ToArray()
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

    /// <summary>
    /// Page to add a new session.
    /// </summary>
    /// <returns>Session form page.</returns>
    /// <exception cref="ArgumentNullException">If <see cref="Claim"/> instance or <see cref="User"/> don't exist.</exception>
    [HttpGet]
    public IActionResult AddSession()
    {
        try
        {
            var claim = ((User.Identity as ClaimsIdentity)?.Claims).First(c => c.Type == "LOCAL AUTHORITY");
            if (claim is null) throw new ArgumentNullException();
            var user = _context.User.FirstOrDefault(u => u.Email == claim.Value);
            if (user is null) throw new ArgumentNullException();

            return View(
                new SessionView()
                {
                    SessionTime = DateTime.Now,
                    Movies = _context.Movie.OrderBy(m => m.Title).ToList(),
                    Halls = _context.Hall.Where(h => h.TheatreId == user.TheatreId).ToList()
                }
            );
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// POST-method to add a session.
    /// </summary>
    /// <param name="sessionView">Session view model.</param>
    /// <returns>Redirect to manager control panel.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="sessionView"/> is null.</exception>
    [HttpPost]
    public IActionResult AddSession(SessionView sessionView)    
    {
        if (sessionView is null) throw new ArgumentNullException();

        try
        {
            var claim = ((User.Identity as ClaimsIdentity)?.Claims).FirstOrDefault(c => c.Type == "LOCAL AUTHORITY");
            if (claim is null) throw new ArgumentNullException();
            var user = _context.User.FirstOrDefault(u => u.Email == claim.Value);
            if (user is null) throw new ArgumentNullException();
            
            var session = new Session()
            {
                Date = sessionView.SessionTime.ToUniversalTime(),
                Is3d = sessionView.Is3d,
                MovieId = sessionView.MovieId,
                HallId = sessionView.HallId,
                UserId = user.Id
            };

            _context.Session.Add(session);
            _context.SaveChanges();
            return Redirect("/Manager/Index");
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Opens default statistics page in manager panel.
    /// </summary>
    /// <returns>Stats page.</returns>
    [HttpGet]
    public IActionResult Stats()
    {
        return View();
    }

    /// <summary>
    /// Forms sells formatted statistics for a given time range.
    /// </summary>
    /// <param name="model">Form model</param>
    /// <returns>Sequence of <see cref="SellsModel"/> (Sales statistics for each session).</returns>
    [HttpPost]
    public IEnumerable<SellsModel> DurationSells(DurationModel model)
    {
        if (model.From > model.To)
        {
            return new[] { new SellsModel { Movie = "Некорректная дата", Date = DateTime.MaxValue } };
        }

        try
        {
            var sessionGroups = _context.Order
                .Where(o =>
                    (o.State == OrderState.Refundable || o.State == OrderState.NonRefundable) &&
                    o.PurchaseDate.ToLocalTime() >= model.From && o.PurchaseDate.ToLocalTime() <= model.To)
                .Include(o => o.Tickets)
                .Include(o => o.Session)
                .ThenInclude(s => s.Movie)
                .AsEnumerable()
                .GroupBy(o => o.SessionId);

            var result = sessionGroups.Select(s => new SellsModel()
            {
                SessionId = s.First().SessionId,
                Movie = s.First().Session.Movie.Title,
                Date = s.First().Session.Date,
                Summary = s.SelectMany(ss => ss.Tickets).Sum(t => t.Cost)
            }).ToArray();

            return result;
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Enumerable.Repeat(new SellsModel{Movie = "Произошла ошибка при обработке запроса", Date = DateTime.MaxValue}, 1);
        }

    }
}