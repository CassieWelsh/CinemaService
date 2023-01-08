using System.ComponentModel.DataAnnotations;

namespace CinemaService.Models.ViewModel
{
    public class RegisterView
    {
        [Required(ErrorMessage = "Не указан Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; init; }
        [Required(ErrorMessage = "Не указано имя")]
        public string FirstName { get; init; }

        [Required(ErrorMessage = "Не указано имя")]
        public string LastName { get; init; }

        [Required(ErrorMessage = "Не указана дата рождения")]
        //[DataType(DataType.Date)]
        public DateTime Birthdate { get; init; }

        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; init; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Не указан пароль")]
        [Compare("Password", ErrorMessage = "Пароль введен неверно")]
        public string ConfirmPassword { get; init; }
    }
}