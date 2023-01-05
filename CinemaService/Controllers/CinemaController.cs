using CinemaService.Models;
using CinemaService.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CinemaService.Mail;
using CinemaService.Models.ViewModel.OrderViews;

namespace CinemaService.Controllers;

public class CinemaController : Controller
{
    private readonly ILogger<CinemaController> _logger;
    private readonly CinemaContext _context;
    private readonly IEmail _mail;

    public CinemaController(ILogger<CinemaController> logger, CinemaContext context, IEmail email)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mail = email ?? throw new ArgumentNullException(nameof(email));
    }

    public IActionResult Index()
    {
        var movies = _context.Session.Include(s => s.Movie).Select(s => s.Movie).AsEnumerable()
            .DistinctBy(m => m.Id);
        return View(new IndexPageView() { Movies = movies });
    }

    [HttpGet]
    [Route("/Cinema/SessionList/{movieId}")]
    public IActionResult SessionList(long movieId)
    {
        var sessions = _context.Session
            .Where(s => s.MovieId == movieId && DateTime.Now.ToUniversalTime() < s.Date)
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

    [HttpPost]
    public IActionResult Order(OrderView orderView)
    {
        if (orderView is null) throw new ArgumentNullException(nameof(orderView));

        User? user = null;
        if (User.Identity.IsAuthenticated)
        {
            var emailClaim = (User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "LOCAL AUTHORITY");
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

    [HttpGet("/Cinema/Repay/{orderId}")]
    public IActionResult Repay(long orderId)
    {
        var order = _context.Order.Include(o => o.Tickets).ThenInclude(t => t.Seat).First(o => o.Id == orderId);
        return View("Payment", new PaymentView { Order = order });
    } 
    
    [HttpPost("/Cinema/confirmPayment")]
    public IActionResult Payment(PaymentView paymentView)
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
            _mail.SendOrderInfo(paymentView.Email, order);
        }

        _context.SaveChanges();
        return Redirect("/");
    }

    [HttpGet("/Cinema/Refund/{orderId}")]
    public IActionResult Refund(long orderId)
    {
        var order = _context.Order
            .Include(o => o.Tickets)
            .ThenInclude(t => t.Seat)
            .First(o => o.Id == orderId);

        return View(new RefundView() { Order = order });
    }

    [HttpPost]
    public IActionResult Refund(RefundView view)
    {
        var tickets = _context.Ticket.Where(t => t.OrderId == view.Order.Id);
        var order = _context.Order.First(o => o.Id == view.Order.Id);
        order.State = OrderState.Cancelled;

        var newOrder = new Order()
        {
            PurchaseDate = DateTime.Now.ToUniversalTime(),
            SessionId = order.SessionId,
            State = OrderState.Refundable,
            UserId = order.UserId ?? null,
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

        return Redirect("/");
    }

    [HttpGet]
    public IActionResult OrderList()
    {
        User user = null;
        if (User.Identity.IsAuthenticated)
        {
            var emailClaim = (User.Identity as ClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == "LOCAL AUTHORITY");
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
        };
        
        return View(orderListView);
    }
}