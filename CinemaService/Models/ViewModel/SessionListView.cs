namespace CinemaService.Models.ViewModel
{
    public class SessionListView
    {
        public Movie Movie { get; set; }
        public IEnumerable<Session> Sessions { get; set; } 
    }
}
