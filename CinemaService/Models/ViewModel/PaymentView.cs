using System.ComponentModel.DataAnnotations;

namespace CinemaService.Models.ViewModel;

public class PaymentView
{
    public Order Order { get; set; }
    public bool IsCancel { get; set; }
    [Required(ErrorMessage = "Укажите почту")]
    public string Email { get; set; }
}