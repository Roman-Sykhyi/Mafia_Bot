using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class Game
{
    public List<Player> Players { get; private set; }
    public List<Player> AlivePlayers { get; private set; }
    public List<Player> AllowedInChat { get; private set; }
    public long Id { get; private set; }
    public Message JoinGameMessage { get; private set; }

    public Game(long id, Message msg)
    {
        Id = id;
        JoinGameMessage = msg;
        Players = new List<Player>();
        AlivePlayers = new List<Player>();
        AllowedInChat = new List<Player>();
    }

    private Random _random = new Random();
    private ChatPermissions _chatPermissions = new ChatPermissions();

    public void AddPlayer(User user)
    {
        var player = new Player(user);
        
        Players.Add(player);
        AlivePlayers.Add(player);
        AllowedInChat.Add(player);
        GamesManager.currentPlayers.Add(player.User);
    }

    public async Task StartGame()
    {
        GiveRoles();
        await NotifyPlayersAboutRoles();
        await StartGameCycle();
    }

    private async Task StartGameCycle()
    {
        TelegramBotClient client = await Bot.Get();

        await SetNight(client);
    }

    private async Task SetNight(TelegramBotClient client)
    {
        DisableChat();

        await Task.Delay(1500);

        string msg = "Місто засинає. Просинається мафія.\nЖиві гравці: ";

        foreach (Player player in Players)
        {
            msg += string.Format("<a href=\"tg://user?id={0}\">", player.User.Id);
            msg += player.User.FirstName;
            msg += "</a> ";
        }

        await client.SendTextMessageAsync(Id, msg, parseMode: ParseMode.Html);

        await Task.Delay(5000);
        EnableChat();
    }

    private void DisableChat()
    {
        AllowedInChat.Clear();
    }

    private void EnableChat()
    {
        foreach (Player player in AlivePlayers)
        {
            AllowedInChat.Add(player);
        }
    }

    private async Task NotifyPlayersAboutRoles()
    {
        TelegramBotClient client = await Bot.Get();

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

            await client.SendTextMessageAsync(player.User.Id, msg, parseMode: ParseMode.Html);
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
        int index = _random.Next(0, playersWithoutRole.Count);

        playersWithoutRole[index].Role = role;

        Console.WriteLine($"Game {Id} ({JoinGameMessage.Chat.Title}): Player - {playersWithoutRole[index].User.FirstName} " +
            $"({playersWithoutRole[index].User.Id}) is {playersWithoutRole[index].Role}");

        playersWithoutRole.RemoveAt(index);
    }
}