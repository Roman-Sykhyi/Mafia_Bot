using Telegram.Bot.Types;

public class Player
{
    public User User { get; private set; }
    public Role Role = Role.Undefined;

    public Player(User user)
    {
        User = user;
    }
}