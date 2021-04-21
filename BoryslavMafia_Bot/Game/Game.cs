using System;
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

    private Random random = new System.Random();

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
        List<Player> playersWithoutRole = new List<Player>(Players);

        int mafiasCount = playersWithoutRole.Count / GameConfiguration.PlayersPerMafia; // визначаємо скільки має бути мафій (на 4 людини - 1 мафія)

        //GivePlayerRole(playersWithoutRole, Role.Doctor);

        if(Players.Count >= GameConfiguration.HomelessPlayersRequired)
        {
            GivePlayerRole(playersWithoutRole, Role.Homeless);
        }

        if(Players.Count >= GameConfiguration.CommisarPlayersRequired)
        {
            GivePlayerRole(playersWithoutRole, Role.Commissar);
        }

        if(Players.Count >= GameConfiguration.ProstitutePlayersRequired)
        {
            GivePlayerRole(playersWithoutRole, Role.Prostitute);
        }

        //TODO: add more roles here if needed

        for(int i = 0; i < mafiasCount; i++)
        {
            GivePlayerRole(playersWithoutRole, Role.Mafia);
        }

        int remainingPlayersCount = playersWithoutRole.Count;
        for(int i = 0; i < remainingPlayersCount; i++)
        {
            GivePlayerRole(playersWithoutRole, Role.Citizen);
        }
    }

    private void GivePlayerRole(List<Player> playersWithoutRole, Role role)
    {
        int index = random.Next(0, playersWithoutRole.Count);

        playersWithoutRole[index].Role = role;

        Console.WriteLine($"User: {playersWithoutRole[index].User.FirstName} is {playersWithoutRole[index].Role}");

        playersWithoutRole.RemoveAt(index);
    }
}
