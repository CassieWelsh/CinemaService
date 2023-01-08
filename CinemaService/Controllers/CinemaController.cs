using System.Diagnostics;
using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CinemaService.Mail;
using CinemaService.Models.ViewModel.OrderViews;
using NuGet.Protocol;
using InvalidOperationException = System.InvalidOperationException;

namespace CinemaService.Controllers;

public class CinemaController : Controller
{
    private readonly ILogger<CinemaController> _logger;
    private readonly CinemaContext _context;
    private readonly IEmail _mail;

    /// <summary>
    /// Creates an instance of <see cref="CinemaController"/>.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="context">Derived Entity framework class of <see cref="CinemaContext"/> type.</param>
    /// <param name="email">Email sender.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="context"/>, <paramref name="logger"/> or <paramref name="email"/> is null.</exception>
    public CinemaController(ILogger<CinemaController> logger, CinemaContext context, IEmail email)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mail = email ?? throw new ArgumentNullException(nameof(email));
    }

    /// <summary>
    /// Index page with movie list.
    /// </summary>
    /// <returns>Index page for the whole service</returns>
    [HttpGet]
    public IActionResult Index()
    {
        try
        {
            var city = Request.Cookies["CinemaCity"];
            if (city is null)
            {
                city = _context.Theatre.First().City;
                Response.Cookies.Append("CinemaCity", city);
            }

            var theatre = _context.Theatre.First(t => t.City == city);
            var movies = _context.Session
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Where(s => s.Date.ToLocalTime() > DateTime.Now && s.Hall.TheatreId == theatre.Id)
                .Select(s => s.Movie)
                .AsEnumerable()
                .DistinctBy(m => m.Id).ToArray();
            
            return View(new IndexPageView() { Movies = movies });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Returns oncoming session list view for a movie.
    /// </summary>
    /// <param name="movieId">Movie id.</param>
    [HttpGet]
    [Route("/Cinema/SessionList/{movieId}")]
    public IActionResult SessionList(long movieId)
    {
        try
        {
            var city = Request.Cookies["CinemaCity"];
            if (city is null)
            {
                city = _context.Theatre.First().City;
                Response.Cookies.Append("CinemaCity", city);
            }
            
            var sessions = _context.Session
                .Include(s => s.Hall).ThenInclude(h => h.Theatre)
                .Where(s => s.MovieId == movieId && DateTime.Now.ToUniversalTime() < s.Date && s.Hall.Theatre.City == city)
                .OrderBy(s => s.Date)
                .Select(s => new Session()
                    {
                        Id = s.Id,
                        Date = s.Date.ToLocalTime(),
                        HallId = s.HallId,
                        Is3d = s.Is3d,
                        MovieId = s.MovieId,
                        UserId = s.UserId
                    }
                );

            var movie = _context.Movie.Include(m => m.Genres).Include(m => m.Countries).Single(m => m.Id == movieId);
            return View(new SessionListView() { Movie = movie, Sessions = sessions });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Returns seat choosing page for session.
    /// </summary>
    /// <param name="sessionId">Session id.</param>
    [HttpGet]
    [Route("/Cinema/Order/{sessionId}")]
    public IActionResult Order(long sessionId)
    {
        try
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
                .Include(s => s.Type)
                .Where(s => s.HallId == session.HallId)
                .Select(s => new SeatView()
                {
                    Seat = s,
                    Available = !_context.Ticket
                        .Include(t => t.Order)
                        .Any(t => t.SeatId == s.Id && t.Order.SessionId == session.Id && t.State == TicketState.Active)
                })
                .ToList();

            return View(new OrderView()
            {
                Movie = movie,
                Session = session,
                Seats = seats
            });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Creates an order.
    /// </summary>
    /// <param name="orderView">Form model with order data.</param>
    [HttpPost]
    public IActionResult Order(OrderView orderView)
    {
        if (orderView is null) throw new ArgumentNullException(nameof(orderView));
        if (orderView.ChosenSeatIds is null) return Redirect($"/Cinema/Order/{orderView.Session.Id}");
        
        try
        {
            User? user = null;
            if (User.Identity.IsAuthenticated)
            {
                var emailClaim =
                    (User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "LOCAL AUTHORITY");
                if (emailClaim is null) throw new ArgumentNullException();
                user = _context.User.First(u => u.Email == emailClaim.Value);
            }

            var purchaseDate = DateTime.Now.ToUniversalTime();
            var tickets = new List<Ticket>();
            var order = new Order()
            {
                PurchaseDate = purchaseDate,
                Session = _context.Session.First(s => s.Id == orderView.Session.Id),
                State = OrderState.Created,
                Tickets = tickets,
                User = user
            };

            foreach (var seatId in orderView.ChosenSeatIds)
            {
                tickets.Add(new Ticket()
                    {
                        PurchaseDate = purchaseDate,
                        Seat = _context.Seat.First(s => s.Id == seatId),
                        Order = order,
                        State = TicketState.Active,
                        Cost = _context.Seat
                            .Include(s => s.Type)
                            .Where(s => s.Id == seatId)
                            .Select(s => orderView.Session.Is3d ? s.Type.Cost3d : s.Type.Cost2d)
                            .Single()
                    }
                );
            }

            _context.Order.Add(order);
            _context.SaveChanges();

            return View("Payment", new PaymentView { Order = order });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Opens repay page for unpaid order.
    /// </summary>
    /// <param name="orderId">Repayable order id.</param>
    [HttpGet("/Cinema/Repay/{orderId}")]
    public IActionResult Repay(long orderId)
    {
        try
        {
            var order = _context.Order.Include(o => o.Tickets).ThenInclude(t => t.Seat).First(o => o.Id == orderId);
            return View("Payment", new PaymentView { Order = order });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    } 
    
    /// <summary>
    /// Confirms order payment.
    /// </summary>
    /// <param name="paymentView">Payment form view model.</param>
    /// <returns>Index page.</returns>
    [HttpPost("/Cinema/confirmPayment")]
    public IActionResult Payment(PaymentView paymentView)
    {
        try
        {
            var order = _context.Order
                .Include(o => o.Session)
                .Include(o => o.Tickets)
                .ThenInclude(o => o.Seat)
                .First(o => o.Id == paymentView.Order.Id);
            if (paymentView.IsCancel)
            {
                order.State = OrderState.Cancelled;
                var tickets = _context.Ticket.Where(t => t.OrderId == order.Id);
                foreach (var ticket in tickets)
                {
                    ticket.State = TicketState.Cancelled;
                }

                _context.Ticket.UpdateRange(tickets);
            }
            else
            {
                order.State = OrderState.Refundable;
                if (User.Identity is { IsAuthenticated: false }) order.Email = paymentView.Email;
                _mail.SendOrderInfo(paymentView.Email, order);
            }

            _context.SaveChanges();
            return Redirect("/");
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Return refund page for a specified order.
    /// </summary>
    /// <param name="orderId">Refundable order id.</param>
    [HttpGet("/Cinema/Refund/{orderId}")]
    public IActionResult Refund(long orderId)
    {
        try
        {
            var order = _context.Order
                .Include(o => o.Tickets)
                .ThenInclude(t => t.Seat)
                .First(o => o.Id == orderId);

            return View(new RefundView() { Order = order });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Refunds chosen tickets from order.
    /// </summary>
    /// <param name="view">Refund form view model.</param>
    [HttpPost]
    public IActionResult Refund(RefundView view)
    {
        try
        {
            var tickets = _context.Ticket.Where(t => t.OrderId == view.Order.Id);
            var order = _context.Order.First(o => o.Id == view.Order.Id);
            order.State = OrderState.Cancelled;

            var newOrder = new Order()
            {
                PurchaseDate = DateTime.Now.ToUniversalTime(),
                SessionId = order.SessionId,
                State = OrderState.Refundable,
                UserId = order.UserId,
                Email = order.Email
            };

            foreach (var ticket in tickets)
            {
                if (view.RefundTickets.Contains(ticket.Id))
                {
                    ticket.State = TicketState.Cancelled;
                }
                else
                {
                    ticket.Order = newOrder;
                }
            }
            _context.SaveChanges();

            var userMail = newOrder.UserId == null ? newOrder.Email : _context.User.First(u => u.Id == newOrder.UserId).Email;
            newOrder.Session = _context.Session.First(s => s.Id == newOrder.SessionId);
            newOrder.Tickets = _context.Ticket.Where(t => t.OrderId == newOrder.Id).Include(t => t.Seat).ToList();
            _mail.UpdateOrderInfo(userMail!, newOrder);

            return Redirect("/");
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    /// <summary>
    /// Returns order list page for a specified user.
    /// </summary>
    [HttpGet]
    public IActionResult OrderList()
    {
        try
        {
            User user = null;
            if (User.Identity.IsAuthenticated)
            {
                var emailClaim =
                    (User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "LOCAL AUTHORITY");
                if (emailClaim is null) throw new ArgumentNullException();
                user = _context.User.First(u => u.Email == emailClaim.Value);
            }

            if (user is null) return Redirect("/Cinema/Error");

            var orderListView = new OrderListView()
            {
                Orders = _context.Order
                    .Where(o => o.UserId == user.Id)
                    .Include(o => o.Session)
                    .ThenInclude(s => s.Movie)
                    .OrderByDescending(o => o.PurchaseDate)
                    .ToList()
                    .Select(o =>
                    {
                        var order = o;
                        order.PurchaseDate = o.PurchaseDate.ToLocalTime();
                        order.Session.Date = o.Session.Date.ToLocalTime();
                        return order;
                    })
            };

            return View(orderListView);
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }
    
    /// <summary>
    /// Returns error page.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>
    /// Opens theatres list.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult ChooseCinema()
    {
        try
        {
            var theatres = _context.Theatre;
            return View(new { Theatres = theatres });
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Error");
        }
    }

    [HttpGet("ChangeTheatre/{theatreId}")]
    public IActionResult ChangeTheatre(long theatreId)
    {
        try
        {
            var city = _context.Theatre.First(t => t.Id == theatreId).City;
            Response.Cookies.Delete("CinemaCity");
            Response.Cookies.Append("CinemaCity", city);
            return Redirect("/");
        }
        catch (InvalidOperationException e)
        {
            _logger.LogError(e.ToString());
            return Redirect("/Cinema/Index");
        }
    }
}