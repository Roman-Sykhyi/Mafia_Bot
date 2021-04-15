using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

public static class GamesManager
{
    public static List<Game> games { get; private set; } = new List<Game>();
    public static List<User> currentPlayers = new List<User>();

    public static Game GetGame(long id)
    {
        var game = games.FirstOrDefault(s => s.ID == id);
        return game;
    }

    public static bool GameExists(long id)
    {
        Game game = GetGame(id);
        return game != null;
    }

    public static void NewGame(long id)
    {
        Game game = new Game(id);
        games.Add(game);
    }

    public static bool IsPlayerAlreadyInGame(User player)
    {
        return currentPlayers.Contains(player);
    }
}
