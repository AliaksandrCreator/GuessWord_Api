public class GameSession
{
    public long Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Masked { get; set; } = string.Empty;
    public int AttemptsLeft { get; set; }
    public string Status { get; set; } = "START";

    public long UserId { get; set; }
    public User User { get; set; } = null!;
}

