using System.ComponentModel.DataAnnotations;

namespace CinemaService.Models.ViewModel
{
    public class LoginView
    {
        [Required(ErrorMessage = "Не указан Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
