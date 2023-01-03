namespace CinemaService.Models.ViewModel;

public class RefundView
{
    public Order Order { get; set; }
    public List<long> RefundTickets { get; set; }
}