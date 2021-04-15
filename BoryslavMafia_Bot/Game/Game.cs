using System.Collections.Generic;
using Telegram.Bot.Types;

public class Game
{
    public long ID;
    private List<User> players;

    public IReadOnlyList<User> Players { get => players.AsReadOnly(); }

    public Game(long id)
    {
        ID = id;
        players = new List<User>();
    }

    public void AddPlayer(User player)
    {
        players.Add(player);
        GamesManager.currentPlayers.Add(player);
    }
}
