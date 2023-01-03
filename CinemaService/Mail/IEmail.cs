using CinemaService.Models;

namespace CinemaService.Mail;

public interface IEmail
{
    void SendOrderInfo(string recipientMail, Order order);
}