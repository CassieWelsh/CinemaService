namespace CinemaService.Models.ViewModel
{
    public class SessionView
    {
        public long MovieId { get; init; }
        public long HallId { get; init; }
        public DateTime SessionTime { get; init; }
        public bool Is3d { get; init; }
        public List<Movie> Movies { get; init; }
        public List<Hall> Halls { get; init; }
        public List<long> ChosenHallIds { get; init; }
    }
}