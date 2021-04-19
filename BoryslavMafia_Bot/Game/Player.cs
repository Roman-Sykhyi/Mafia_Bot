using Telegram.Bot.Types;

public class Player
{
    public User User { get; private set; }

    public Player(User user)
    {
        User = user;
    }
}