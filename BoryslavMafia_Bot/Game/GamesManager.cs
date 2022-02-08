using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;

public static class GamesManager
{
    public static List<Game> games { get; private set; } = new List<Game>();
    public static List<User> currentPlayers = new List<User>();

    public static async Task<Game> GetGame(long id)
    {
        var game = games.FirstOrDefault(s => s.Id == id);
        return game;
    }

    public static async Task<bool> GameExists(long id)
    {
        Game game = await GetGame(id);
        return game != null;
    }

    public static void NewGame(long id, Message msg)
    {
        Game game = new Game(id, msg);
        games.Add(game);
    }

    public static bool IsPlayerAlreadyInGame(User user)
    {
        return currentPlayers.Contains(user);
    }

    public static void ForceEndGame(Game game)
    {
        foreach(Player player in game.Players)
        {
            currentPlayers.Remove(player.User);
        }

        games.Remove(game);
    }
}