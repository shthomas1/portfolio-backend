namespace portfolio_backend.Models
{
    public class Feedback
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int Usability { get; set; }
        public int Design { get; set; }
        public int Content { get; set; }
        public string? Comments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}