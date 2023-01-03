using System.Net;
using System.Net.Mail;
using System.Text;
using CinemaService.Models;

namespace CinemaService.Mail;

public class YandexMail : IEmail
{
    private readonly SmtpClient _client;
    private readonly string _senderMail;

    public YandexMail()
    {
        var login = Environment.GetEnvironmentVariable("YANDEXLOGIN", EnvironmentVariableTarget.Machine) ?? throw new ArgumentNullException();
        var password = Environment.GetEnvironmentVariable("YANDEXPASSWORD", EnvironmentVariableTarget.Machine) ?? throw new ArgumentNullException();
        var smtpAddress = Environment.GetEnvironmentVariable("YANDEXSMTPADDRESS", EnvironmentVariableTarget.Machine) ?? throw new ArgumentNullException();
        var smtpPort = int.Parse(Environment.GetEnvironmentVariable("YANDEXSMTPPORT", EnvironmentVariableTarget.Machine) ?? throw new ArgumentNullException());
        var authInfo = new NetworkCredential(login, password);

        _client = new SmtpClient(smtpAddress, smtpPort)
        {
            UseDefaultCredentials = false,
            EnableSsl = true,
            Credentials = authInfo
        };
        _senderMail = login + "@yandex.ru";
    }

    public void SendOrderInfo(string recipientMail, Order order)
    {
        string mailBody = $"Дата и время сеанса: {order.Session.Date}\n" +
                          "Информация о билетах:\n";
        foreach (var ticket in order.Tickets)
        {
            mailBody += $"Ряд: {ticket.Seat.Row}, Место: {ticket.Seat.Number}, Стоимость: {ticket.Cost}₽\n";
        }

        mailBody += $"Ссылка для управления заказом: https://localhost:7074/Cinema/Refund/{order.Id}";
        
        var mail = new MailMessage(new MailAddress(_senderMail, "CinemaService"), new MailAddress(recipientMail))
        {
            Subject = "Информация о билетах",
            SubjectEncoding = Encoding.UTF8,
            Body = mailBody,
            BodyEncoding = Encoding.UTF8
        };
        _client.Send(mail);
    }
}