using System.Collections.Generic;
using Telegram.Bot.Types;

public class Game
{
    private List<User> players;

    public IReadOnlyList<User> Players { get => players.AsReadOnly(); }
    public long ID;
    public Message joinGameMessage;
    public Game(long id, Message msg)
    {
        ID = id;
        joinGameMessage = msg;
        players = new List<User>();
    }

    public void AddPlayer(User player)
    {
        players.Add(player);
        GamesManager.currentPlayers.Add(player);
    }
}
