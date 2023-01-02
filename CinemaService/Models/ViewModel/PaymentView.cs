namespace CinemaService.Models.ViewModel;

public class PaymentView
{
    public Order Order { get; set; }
    public bool IsCancel { get; set; }
    public string Email { get; set; }
}