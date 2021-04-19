using System.Collections.Generic;
using Telegram.Bot.Types;

public class Game
{
    public List<Player> Players { get; private set; }
    public long ID;
    public Message joinGameMessage;
    public Game(long id, Message msg)
    {
        ID = id;
        joinGameMessage = msg;
        Players = new List<Player>();
    }

    public void AddPlayer(User user)
    {
        var player = new Player(user);
        
        Players.Add(player);
        GamesManager.currentPlayers.Add(player.User);
    }

    public bool TryStartGame()
    {
        if(Players.Count < GameConfiguration.MinimumPlayers)
        {
            GamesManager.ForceEndGame(this);
            return false;
        }
        else
        {
            StartGame();
            return true;
        }
    }

    private void StartGame()
    {
        GiveRoles();
    }

    private void GiveRoles()
    {

    }
}
