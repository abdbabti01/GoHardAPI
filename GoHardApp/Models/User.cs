namespace GoHardApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public string? Goals { get; set; }
    }
}
