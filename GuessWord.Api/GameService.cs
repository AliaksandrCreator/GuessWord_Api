using Microsoft.EntityFrameworkCore;

public class GameService
{
    private readonly AppDbContext _db;

    public GameService(AppDbContext db) => _db = db;

    // Старт новой игры с учётом пользователя
    public async Task<long> StartNewGameAsync(string userName)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Name == userName);
        if (user == null)
        {
            user = new User { Name = userName };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var words = File.ReadAllLines(Configuration.WordListPath);
        var word = words[new Random().Next(words.Length)].Trim().ToUpper();
        var masked = string.Join(" ", word.Select(_ => "_"));

        var session = new GameSession
        {
            Word = word,
            Masked = masked,
            AttemptsLeft = 6,
            Status = "START",
            UserId = user.Id
        };

        _db.WordGame.Add(session);
        await _db.SaveChangesAsync();
        return session.Id;
    }

    // Угадывание буквы
    public async Task<string> GuessAsync(long id, char letter)
    {
        var session = await _db.WordGame.FindAsync(id);
        if (session == null) return $"Сессия {id} не найдена.";
        if (session.Status != "START") return "Игра уже завершена.";

        var word = session.Word;
        var masked = session.Masked.Split(' ');
        var upperLetter = char.ToUpper(letter);
        var correct = false;

        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] == upperLetter && masked[i] == "_")
            {
                masked[i] = upperLetter.ToString();
                correct = true;
            }
        }

        if (!correct) session.AttemptsLeft--;

        session.Masked = string.Join(" ", masked);
        session.Status = masked.Contains("_") && session.AttemptsLeft > 0 ? "START" :
                         !masked.Contains("_") ? "WON" : "LOST";

        await _db.SaveChangesAsync();
        var result = correct ? "Верно!" : "Неверно.";
        return $"Буква: {upperLetter} → {result} Слово: {session.Masked} Осталось попыток: {session.AttemptsLeft}";
    }

    // Статистика по статусу и пользователю
    public async Task<List<string>> GetStatisticsAsync(string? status = null, string? userName = null)
    {
        var query = _db.WordGame.Include(g => g.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status.ToUpper());

        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(s => s.User.Name == userName);

        var sessions = await query.ToListAsync();

        return sessions.Select(s =>
            $"Игра {s.Id}: Слово = {s.Word}, Маска = {s.Masked}, Попыток = {s.AttemptsLeft}, Статус = {s.Status}, Пользователь = {s.User.Name}"
        ).ToList();
    }

    // удаленние игр игрока
    public async Task<string> DeleteUserAsync(string userName)
    {
        var user = await _db.Users.Include(u => u.Games)
                                  .FirstOrDefaultAsync(u => u.Name == userName);

        if (user == null)
            return $"Пользователь '{userName}' не найден.";

        _db.WordGame.RemoveRange(user.Games);
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();
        return $"Пользователь '{userName}' и его игры удалены.";
    }


}

