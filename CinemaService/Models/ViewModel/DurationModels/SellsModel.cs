namespace CinemaService.Models.ViewModel.DurationModels;

public class SellsModel
{
    public long SessionId { get; set; }
    public string Movie { get; set; } 
    public DateTime Date { get; set; }
    public decimal Summary { get; set; }
}