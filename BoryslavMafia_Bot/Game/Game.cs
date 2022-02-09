using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Game
{
    public long Id { get; private set; }
    public List<Player> Players { get; private set; }
    public List<Player> AlivePlayers { get; private set; }
    public List<Player> AllowedInChat { get; private set; }
    public Message JoinGameMessage { get; private set; }

    private int MafiasCount { get { return Players.FindAll(p => p.Role == Role.Mafia).Count; } }

    private Random _random = new Random();

    private List<List<InlineKeyboardButton>> _mafiasPollKeyboard = new List<List<InlineKeyboardButton>>();
    private Dictionary<Player, int> _mafiasPoll = new Dictionary<Player, int>();
    private int _mafiaRemainingVotes;
    private Player _lastNightKilledPlayer;
    private Player _lastNightHealedPlayer;
    private bool _playerSurvived;

    public Game(long id, Message msg)
    {
        Id = id;
        JoinGameMessage = msg;

        Players = new List<Player>();
        AlivePlayers = new List<Player>();
        AllowedInChat = new List<Player>();
    }

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

    public void GiveVoteForVictim(User user)
    {
        Player player = Players.Find(p => p.User.Id == user.Id);
        _mafiasPoll[player]++;
        _mafiaRemainingVotes--;
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

        string msg = "Місто засинає. Просинається мафія.\nЖиві гравці:\n";

        foreach (Player player in Players)
        {
            msg += string.Format("<a href=\"tg://user?id={0}\">", player.User.Id);
            msg += player.User.FirstName + player.User.LastName + " " + player.User.Username;
            msg += "</a>\n";
        }

        await client.SendTextMessageAsync(Id, msg, parseMode: ParseMode.Html);

        await SendMafiasPoll(client);

        while (_mafiaRemainingVotes != 0)
            await Task.Delay(1000);

        // other roles act

        KillVictim();

        if(MafiasCount >= AlivePlayers.Count - MafiasCount)
        {
            await client.SendTextMessageAsync(Id, "Перемогла мафія");
            GamesManager.ForceEndGame(this);
        }
        else if(MafiasCount == 0)
        {
            await client.SendTextMessageAsync(Id, "Перемогли мирні жителі");
            GamesManager.ForceEndGame(this);
        }

        await Task.Delay(5000);
        EnableChat();
    }

    private void KillVictim()
    {
        Player playerToKill = _mafiasPoll.FirstOrDefault(x => x.Value == _mafiasPoll.Values.Max()).Key;

        if(playerToKill == _lastNightHealedPlayer)
        {
            _playerSurvived = true;
            return;
        }

        AlivePlayers.Remove(playerToKill);
        _lastNightKilledPlayer = playerToKill;
        _playerSurvived = false;
    }

    private async Task SendMafiasPoll(TelegramBotClient client)
    {
        _mafiaRemainingVotes = MafiasCount;

        foreach (var item in Players)
        {
            if(item.Role != Role.Mafia)
                _mafiasPoll.Add(item, 0);
        }

        BuildMafiasPoll();

        var keyboard = new InlineKeyboardMarkup(_mafiasPollKeyboard);

        foreach (Player player in Players)
        {
            if(player.Role == Role.Mafia)
            {
                await client.SendTextMessageAsync(player.User.Id, "Виберіть жертву:", replyMarkup: keyboard);
            }
        }
    }

    private void BuildMafiasPoll()
    {
        _mafiasPollKeyboard.Clear();

        foreach (var item in _mafiasPoll)
        {
            if(item.Key.Role != Role.Mafia)
            {
                string name = item.Key.User.FirstName + " " + item.Key.User.LastName + " " + item.Key.User.Username;
                string callbackData = CallbackQueryType.MafiaPollChooseVictim.ToString() + " " + item.Key.User.Id + " " + Id;
                _mafiasPollKeyboard.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(name, callbackData) });
            }
        }
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
            string msg = "Твоя роль - <b>";
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

        GivePlayerRole(playersWithoutRole, Role.Mafia);

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

        Console.WriteLine($"Game {Id} ({JoinGameMessage.Chat.Title}): " +
            $"Player - {playersWithoutRole[index].User.FirstName} {playersWithoutRole[index].User.LastName} ({playersWithoutRole[index].User.Username}) " +
            $"({playersWithoutRole[index].User.Id}) is {playersWithoutRole[index].Role}");

        playersWithoutRole.RemoveAt(index);
    }
}