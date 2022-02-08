using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;

public class Game
{
    public List<Player> Players { get; private set; }
    public long Id { get; private set; }
    public Message joinGameMessage { get; private set; }
    public Game(long id, Message msg)
    {
        Id = id;
        joinGameMessage = msg;
        Players = new List<Player>();
    }

    private Random random = new Random();

    public void AddPlayer(User user)
    {
        var player = new Player(user);
        
        Players.Add(player);
        GamesManager.currentPlayers.Add(player.User);
    }

    public async Task<bool> TryStartGameAsync()
    {
        if(Players.Count < GameConfiguration.MinimumPlayers)
        {
            GamesManager.ForceEndGame(this);
            return false;
        }
        else
        {
            await StartGameAsync();
            return true;
        }
    }

    private async Task StartGameAsync()
    {
        GiveRoles();
        await NotifyPlayersAboutRolesAsync();
    }

    private async Task NotifyPlayersAboutRolesAsync()
    {
        Telegram.Bot.TelegramBotClient client = await Bot.Get();

        foreach (Player player in Players)
        {
            string msg = "Ти - <b>";
            switch (player.Role)
            {
                case Role.Citizen:
                    msg += "Мирний житель";
                    break;
                case Role.Doctor:
                    msg += "Лікар";
                    break;
                case Role.Commissar:
                    msg += "Комісар";
                    break;
                case Role.Homeless:
                    msg += "Безхатько";
                    break;
                case Role.Prostitute:
                    msg += "Повія";
                    break;
                case Role.Mafia:
                    msg += "Мафія";
                    break;
            }

            msg += "</b>";

            await client.SendTextMessageAsync(player.User.Id, msg, parseMode:Telegram.Bot.Types.Enums.ParseMode.Html);
        }
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

        Console.WriteLine($"Game {Id} ({joinGameMessage.Chat.Title}): Player - {playersWithoutRole[index].User.FirstName} ({playersWithoutRole[index].User.Id}) is {playersWithoutRole[index].Role}");

        playersWithoutRole.RemoveAt(index);
    }
}