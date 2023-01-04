using CinemaService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderInfoUpdateService;

/// <summary>
/// Service type with a database update methods.
/// </summary>
public class OrderUpdater
{
    private readonly CinemaContext _context;
    private readonly int _orderPaymentTimeout;
    private readonly int _refundTimeout;

    /// <summary>
    /// Creates a new instance of <see cref="OrderUpdater"/>.
    /// </summary>
    /// <param name="context">Derived Entity framework class of <see cref="CinemaContext"/> type.</param>
    /// <param name="paymentTimeout">A timeout after which the unpaid order will be cancelled.</param>
    /// <param name="refundTimeout">A timeout after which refunds for a session will be unavailable.</param>
    /// <exception cref="ArgumentNullException">If <see cref="context"/> is null.</exception>
    public OrderUpdater(CinemaContext context, int paymentTimeout, int refundTimeout)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _orderPaymentTimeout = paymentTimeout;
        _refundTimeout = refundTimeout;
    }

    /// <summary>
    /// Updates order and ticket states in database in accordance with current time.
    /// </summary>
    public void UpdateOrders()
    {
        var orders = _context.Order
            .Include(o => o.Tickets)
            .Include(o => o.Session)
            .Where(o => o.State == OrderState.Created || o.State == OrderState.Refundable);
        foreach (var order in orders)
        {
            if (order.State == OrderState.Created && DateTime.Now > order.PurchaseDate.AddMinutes(_orderPaymentTimeout).ToLocalTime())
            {
                order.State = OrderState.Cancelled;
                foreach (var ticket in order.Tickets)
                {
                    ticket.State = TicketState.Cancelled;
                }
                continue;
            }

            if (order.State == OrderState.Refundable && DateTime.Now > order.Session.Date.AddMinutes(-_refundTimeout).ToLocalTime())
            {
                order.State = OrderState.NonRefundable;
            }
        }
        _context.SaveChanges();
    }
}