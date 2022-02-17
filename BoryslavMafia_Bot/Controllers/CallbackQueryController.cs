using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class CallbackQueryController
{
    public static async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        CallbackQueryType type = Enum.Parse<CallbackQueryType>(callbackQuery.Data.Substring(0, callbackQuery.Data.IndexOf(' ')));
            
        switch (type)
        {
            case CallbackQueryType.GameStarter:
                await GameStarterCallbackQueryReceived(callbackQuery, client);
                break;
            case CallbackQueryType.MafiaPollChooseVictim:
                await MafiaChooseVictimCallBackQueryReceived(callbackQuery, client);
                break;
            case CallbackQueryType.PlayersChooseLynchVictim:
                await PlayersChooseLynchVictimCallbackQueryReceived(callbackQuery, client);
                break;
            case CallbackQueryType.DoctorHealPlayer:
                await DoctorHealPlayerCallbackQueryReceived(callbackQuery, client);
                break;
            case CallbackQueryType.CommissarCheckPlayer:
                await CommisarCheckPlayerCallbackQueryReceived(callbackQuery, client);
                break;
        }
    }

    private static async Task CommisarCheckPlayerCallbackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        string[] callbackData = callbackQuery.Data.Split();

        await client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);

        Game game = await GamesManager.GetGame(long.Parse(callbackData[2]));
        Player playerToCheck = game.AlivePlayers.FirstOrDefault(p => p.User.Id == int.Parse(callbackData[1]));

        string msgText = $"Ви вибрали: <a href=\"tg://user?id={playerToCheck.User.Id}\">" + playerToCheck.User.FirstName + " " + playerToCheck.User.LastName + " " + playerToCheck.User.Username + "</a>";
        await client.SendTextMessageAsync(callbackQuery.From.Id, msgText, parseMode: ParseMode.Html);

        await Task.Delay(1000);

        string msg = $"<a href=\"tg://user?id={playerToCheck.User.Id}\">{playerToCheck.User.FirstName} {playerToCheck.User.LastName} {playerToCheck.User.Username}</a> - ";
        switch (playerToCheck.Role)
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

        await client.SendTextMessageAsync(callbackQuery.From.Id, msg, parseMode:ParseMode.Html);

        game.CommissarCheckedPlayer();
    }

    private static async Task DoctorHealPlayerCallbackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        string[] callbackData = callbackQuery.Data.Split();

        await client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);

        User user = GamesManager.currentPlayers.Find(p => p.Id == int.Parse(callbackData[1]));

        string msgText = $"Ви вибрали: <a href=\"tg://user?id={user.Id}\">" + user.FirstName + " " + user.LastName + " " + user.Username + "</a>";
        await client.SendTextMessageAsync(callbackQuery.From.Id, msgText, parseMode: ParseMode.Html);

        Game game = await GamesManager.GetGame(long.Parse(callbackData[2]));
        game.HealPlayer(user);
    }

    private static async Task MafiaChooseVictimCallBackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        string[] callbackData = callbackQuery.Data.Split();

        await client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);

        User user = GamesManager.currentPlayers.Find(p => p.Id == int.Parse(callbackData[1]));
        string msgText = $"Ви вибрали: <a href=\"tg://user?id={user.Id}\">" + user.FirstName + " " + user.LastName + " " + user.Username + "</a>";
        await client.SendTextMessageAsync(callbackQuery.From.Id, msgText, parseMode: ParseMode.Html);

        Game game = await GamesManager.GetGame(long.Parse(callbackData[2]));
        game.GiveVoteForMafiaVictim(user);
    }

    private static async Task PlayersChooseLynchVictimCallbackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        string[] callbackData = callbackQuery.Data.Split();

        await client.DeleteMessageAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId);

        User user = GamesManager.currentPlayers.Find(p => p.Id == int.Parse(callbackData[1]));

        string msgText = $"Ви вибрали: <a href=\"tg://user?id={user.Id}\">" + user.FirstName + " " + user.LastName + " " + user.Username + "</a>";
        await client.SendTextMessageAsync(callbackQuery.From.Id, msgText, parseMode:ParseMode.Html);

        string msgTextGroup = $"<a href=\"tg://user?id={callbackQuery.From.Id}\">" +
            $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName} {callbackQuery.From.Username}</a> голосує за " +
            $"<a href=\"tg://user?id={user.Id}\">{user.FirstName} {user.LastName} {user.Username}</a>";
        await client.SendTextMessageAsync(long.Parse(callbackData[2]), msgTextGroup, parseMode:ParseMode.Html);

        Game game = await GamesManager.GetGame(long.Parse(callbackData[2]));
        game.GiveVoteForLynchVictim(user);
    }

    private static async Task GameStarterCallbackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        await client.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            $"Received {callbackQuery.Data}"
        );

        await client.SendTextMessageAsync(
            callbackQuery.Message.Chat.Id,
            $"Received {callbackQuery.Data}"
        );
    }
}