using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CinemaService.Models.ViewModel
{
    public class MovieView
    {
        [Required(ErrorMessage = "Не указано название")]
        public string Title { get; init; }
        [Required(ErrorMessage = "Не указан режиссер(ы)")]
        public string Director { get; init; }
        [Required(ErrorMessage = "Не указан год")]
        public int? Year { get; init; }
        public string Description { get; init; }
        [Required(ErrorMessage = "Не указана длительность фильма")]
        public int? Length { get; init; }
        [BindProperty]
        public List<Genre> Genres { get; init; }
        public List<short> ChosenGenreIds { get; init; }
        public List<Country> Countries { get; init; }
        public List<short> ChosenCountryIds { get; init; }
    }
}
