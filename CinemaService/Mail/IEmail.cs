using CinemaService.Models;

namespace CinemaService.Mail;

public interface IEmail
{
    void SendOrderInfo(string recipientMail, Order order);
    void UpdateOrderInfo(string recipientMail, Order order);
}