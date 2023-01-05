using System.ComponentModel;

namespace CinemaService.Models
{
    public enum OrderState
    {
        Created,
        Cancelled,
        Refundable,
        NonRefundable
    }
}