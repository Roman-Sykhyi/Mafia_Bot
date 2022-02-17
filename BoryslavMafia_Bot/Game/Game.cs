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

    public bool IsMafiaVoting { get; private set; }

    private int MafiasCount { get { return Players.FindAll(p => p.Role == Role.Mafia).Count; } }
    private int AliveMafiasCount { get { return AlivePlayers.FindAll(p => p.Role == Role.Mafia).Count; } }

    private Random _random = new Random();

    private List<List<InlineKeyboardButton>> _mafiasPollKeyboard = new List<List<InlineKeyboardButton>>();
    private Dictionary<Player, int> _mafiasPoll = new Dictionary<Player, int>();
    private int _mafiaRemainingVotes;
    private Player _lastNightKilledPlayer;
    private bool _playerSurvived;

    private Player _lastNightHealedPlayer;
    private bool _doctorHealed = false;

    private bool _commissarCheckedPlayer = false;

    private Dictionary<Player, int> _playersLynchVoting = new Dictionary<Player, int>();
    private Player _lastDayLynchedPlayer;
    private int _playersRemainingVotes;

    private bool gameEnded = false;

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

    public void GiveVoteForMafiaVictim(User user)
    {
        Player player = AlivePlayers.Find(p => p.User.Id == user.Id);
        _mafiasPoll[player]++;
        _mafiaRemainingVotes--;
    }

    public void GiveVoteForLynchVictim(User user)
    {
        Player player = AlivePlayers.Find(p => p.User.Id == user.Id);
        _playersLynchVoting[player]++;
        _playersRemainingVotes--;
    }

    public void HealPlayer(User user)
    {
        _lastNightHealedPlayer = AlivePlayers.FirstOrDefault(p => p.User.Id == user.Id);
        _doctorHealed = true;
    }

    public void CommissarCheckedPlayer()
    {  
        _commissarCheckedPlayer = true;
    }

    private async Task StartGameCycle()
    {
        TelegramBotClient client = await Bot.Get();

        await SetNight(client);
    }

    private async Task SetNight(TelegramBotClient client)
    {
        if (gameEnded)
            return;

        DisableChat();

        await Task.Delay(1500);

        string msg = "<b>Місто засинає. Просинається мафія</b>";

        await client.SendTextMessageAsync(Id, msg, parseMode: ParseMode.Html);

        await StartMafiasPoll(client);

        while (_mafiaRemainingVotes != 0)
            await Task.Delay(1000);

        IsMafiaVoting = false;
        await client.SendTextMessageAsync(Id, "<b>Мафія вибрала жертву</b>", parseMode:ParseMode.Html);

        await DoctorHealPlayer(client);

        while (!_doctorHealed)
            await Task.Delay(1000);

        await client.SendTextMessageAsync(Id, "<b>Лікар вибрав кого лікувати</b>", parseMode: ParseMode.Html);

        if(Players.Count >= GameConfiguration.CommisarPlayersRequired)
        {
            await CommissarCheckPlayer(client);

            while (!_commissarCheckedPlayer)
                await Task.Delay(1000);

            await client.SendTextMessageAsync(Id, "<b>Комісар провів перевірку</b>", parseMode: ParseMode.Html);
        }

        // other roles act

        KillVictim();

        await CheckForWin(client);

        await Task.Delay(2000);

        await SetDay(client);      
    }

    private async Task CommissarCheckPlayer(TelegramBotClient client)
    {
        _commissarCheckedPlayer = false;

        Player commissar = AlivePlayers.FirstOrDefault(p => p.Role == Role.Commissar);

        if (commissar == null)
        {
            int timeToWait = _random.Next(3000, 7000);
            await Task.Delay(timeToWait);
            _commissarCheckedPlayer = true;
        }
        else
        {
            List<List<InlineKeyboardButton>> commissarKeyboard = new List<List<InlineKeyboardButton>>();

            foreach (Player player in AlivePlayers)
            {
                if (player.Role != Role.Commissar)
                {
                    string name = player.User.FirstName + " " + player.User.LastName + " " + player.User.Username;
                    string callbackData = CallbackQueryType.CommissarCheckPlayer.ToString() + " " + player.User.Id + " " + Id;
                    commissarKeyboard.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(name, callbackData) });
                }
            }

            var keyboard = new InlineKeyboardMarkup(commissarKeyboard);
            await client.SendTextMessageAsync(commissar.User.Id, "<b>Кого будемо перевіряти?</b>", parseMode: ParseMode.Html, replyMarkup: keyboard);
        }
    }

    private async Task SetDay(TelegramBotClient client)
    {
        if (gameEnded)
            return;

        if (_playerSurvived)
            await client.SendTextMessageAsync(Id, "<b>Цієї ночі всі залишились живими</b>", parseMode:ParseMode.Html);
        else
           await client.SendTextMessageAsync(Id, $"Цієї ночі було вбито " +
                $"{_lastNightKilledPlayer.User.FirstName} {_lastNightKilledPlayer.User.LastName} {_lastNightKilledPlayer.User.Username}");

        string msg = "<b>Мафія засинає. Місто просинається</b>\n<b>Живі гравці:</b>\n";

        foreach (Player player in AlivePlayers)
        {
            msg += string.Format("<a href=\"tg://user?id={0}\">", player.User.Id);
            msg += player.User.FirstName + player.User.LastName + " " + player.User.Username;
            msg += "</a>\n";
        }

        await client.SendTextMessageAsync(Id, msg, parseMode: ParseMode.Html);

        await Task.Delay(1000);

        EnableChat();
        await StartDiscussion(client);
        DisableChat();

        await StartPlayersLynchVoting(client);

        while (_playersRemainingVotes != 0)
            await Task.Delay(1000);

        await client.SendTextMessageAsync(Id, "<b>Голосування завершено</b>", parseMode: ParseMode.Html);

        await Task.Delay(1000);

        LynchPlayer();

        await client.SendTextMessageAsync(Id, $"<b>Жителі вирішили повісити</b> <a href=\"tg://user?id={_lastDayLynchedPlayer.User.Id}\">{_lastDayLynchedPlayer.User.FirstName} {_lastDayLynchedPlayer.User.LastName} {_lastDayLynchedPlayer.User.Username}</a>",
            parseMode: ParseMode.Html);

        await Task.Delay(1000);

        await CheckForWin(client);

        await SetNight(client);
    }

    private async Task DoctorHealPlayer(TelegramBotClient client)
    {
        _doctorHealed = false;
        Player doctor = AlivePlayers.FirstOrDefault(p => p.Role == Role.Doctor);

        if(doctor == null)
        {
            int timeToWait = _random.Next(3000, 7000);
            await Task.Delay(timeToWait);
            _doctorHealed = true;
        }
        else
        {
            List<List<InlineKeyboardButton>> doctorKeyboard = new List<List<InlineKeyboardButton>>();

            foreach (Player player in AlivePlayers)
            {
                //if (player.Role != Role.Doctor)
                //{
                    string name = player.User.FirstName + " " + player.User.LastName + " " + player.User.Username;
                    string callbackData = CallbackQueryType.DoctorHealPlayer.ToString() + " " + player.User.Id + " " + Id;
                    doctorKeyboard.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(name, callbackData) });
                //}
            }

            var keyboard = new InlineKeyboardMarkup(doctorKeyboard);
            await client.SendTextMessageAsync(doctor.User.Id, "<b>Кого будемо лікувати?</b>", parseMode: ParseMode.Html, replyMarkup: keyboard);
        }
    }

    private void LynchPlayer()
    {
        Player playerToLynch = _playersLynchVoting.FirstOrDefault(x => x.Value == _playersLynchVoting.Values.Max()).Key;

        AlivePlayers.Remove(playerToLynch);
        _lastDayLynchedPlayer = playerToLynch;
    }

    private async Task StartPlayersLynchVoting(TelegramBotClient client)
    {
        _playersLynchVoting.Clear();
        _playersRemainingVotes = AlivePlayers.Count;

        foreach (var item in AlivePlayers)
            _playersLynchVoting.Add(item, 0);

        foreach (var player in AlivePlayers)
        {
            await SendLynchPoll(client, player);
        }

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Перейти до голосування", "t.me/boryslavmafia_debug_bot")); 
        await client.SendTextMessageAsync(Id, "<b>Обговорення завершено. Час вибирати кого будемо вішати</b>", parseMode:ParseMode.Html, replyMarkup: keyboard);
    }

    private async Task SendLynchPoll(TelegramBotClient client, Player player)
    {
        var keyboard = new InlineKeyboardMarkup(BuildLynchPoll(player));
        await client.SendTextMessageAsync(player.User.Id, "<b>Виберіть кого будемо вішати</b>", parseMode:ParseMode.Html, replyMarkup:keyboard);
    }

    private List<List<InlineKeyboardButton>> BuildLynchPoll(Player player)
    {
        List<List<InlineKeyboardButton>> playersLynchVotingKeyboard = new List<List<InlineKeyboardButton>>();

        foreach (var item in AlivePlayers)
        {
            if(item.User.Id != player.User.Id)
            {
                string name = item.User.FirstName + " " + item.User.LastName + " " + item.User.Username;
                string callbackData = CallbackQueryType.PlayersChooseLynchVictim.ToString() + " " + item.User.Id + " " + Id;
                playersLynchVotingKeyboard.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData(name, callbackData) });
            }
        }

        return playersLynchVotingKeyboard;
    }

    private async Task StartDiscussion(TelegramBotClient client)
    {
        await client.SendTextMessageAsync(Id, "<b>Час для обговорення</b>", parseMode:ParseMode.Html);

        int remainingTime = GameConfiguration.DiscussionTime;

        while (remainingTime > 0)
        {
            await client.SendTextMessageAsync(Id, string.Format("До завершення обговорення залишилось <b>{0} секунд</b>", remainingTime),
                parseMode: ParseMode.Html);

            await Task.Delay(15000);
            remainingTime -= 15;
        }
    }

    private async Task CheckForWin(TelegramBotClient client)
    {
        if (MafiasCount < AlivePlayers.Count - MafiasCount)
        {
            await client.SendTextMessageAsync(Id, "<b>Перемогла мафія</b>", parseMode:ParseMode.Html);
            GamesManager.ForceEndGame(this);
            gameEnded = true;
            await ShowPlayerRoles(client);
        }
        else if (MafiasCount == 0)
        {
            await client.SendTextMessageAsync(Id, "<b>Перемогли мирні жителі</b>", parseMode: ParseMode.Html);
            GamesManager.ForceEndGame(this);
            gameEnded = true;
            await ShowPlayerRoles(client);
        }
    }

    private async Task ShowPlayerRoles(TelegramBotClient client)
    {
        string msg = "<b>Учасники гри:</b>\n";

        foreach (Player player in Players)
        {
            msg += $"<a href=\"tg://user?id={player.User.Id}\">{player.User.FirstName} {player.User.LastName} {player.User.Username}</a> - ";
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
            msg += "\n";
        }

        await client.SendTextMessageAsync(Id, msg, parseMode: ParseMode.Html);
    }

    private void KillVictim()
    {
        Player playerToKill = _mafiasPoll.FirstOrDefault(x => x.Value == _mafiasPoll.Values.Max()).Key;

        if(playerToKill.User.Id == _lastNightHealedPlayer.User.Id)
        {
            _playerSurvived = true;
            return;
        }

        AlivePlayers.Remove(playerToKill);
        _lastNightKilledPlayer = playerToKill;
        _playerSurvived = false;
    }

    private async Task StartMafiasPoll(TelegramBotClient client)
    {
        _mafiasPoll.Clear();
        _mafiaRemainingVotes = AliveMafiasCount;
        IsMafiaVoting = true;

        foreach (var item in Players)
        {
            if(item.Role != Role.Mafia)
                _mafiasPoll.Add(item, 0);
        }

        BuildMafiasPoll();

        var keyboard = new InlineKeyboardMarkup(_mafiasPollKeyboard);

        foreach (Player player in AlivePlayers)
        {
            if(player.Role == Role.Mafia)
            {
                await client.SendTextMessageAsync(player.User.Id, "<b>Виберіть жертву:</b>", parseMode:ParseMode.Html, replyMarkup: keyboard);
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

        GivePlayerRole(playersWithoutRole, Role.Mafia); //for test
        GivePlayerRole(playersWithoutRole, Role.Doctor);

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