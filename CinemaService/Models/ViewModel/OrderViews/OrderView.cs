namespace CinemaService.Models.ViewModel.OrderViews
{
    public class OrderView
    {
        public Movie Movie { get; set; }
        public Session Session { get; set; }
        public IList<SeatView> Seats { get; set; }
        
        public IList<long> ChosenSeatIds { get; set; }
        public long SessionId { get; set; }
    }
}
