public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<GameSession> Games { get; set; } = new();
}
