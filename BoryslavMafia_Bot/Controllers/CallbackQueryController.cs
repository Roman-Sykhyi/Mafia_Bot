using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

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
        }
    }

    private static async Task MafiaChooseVictimCallBackQueryReceived(CallbackQuery callbackQuery, TelegramBotClient client)
    {
        string[] callbackData = callbackQuery.Data.Split();

        int messageId = callbackQuery.Message.MessageId;

        await client.DeleteMessageAsync(callbackQuery.From.Id, messageId);

        User user = GamesManager.currentPlayers.Find(p => p.Id == int.Parse(callbackData[1]));
        string msgText = "Ви вибрали: " + user.FirstName + " " + user.LastName + " " + user.Username;
        await client.SendTextMessageAsync(callbackQuery.From.Id, msgText);

        Game game = await GamesManager.GetGame(long.Parse(callbackData[2]));
        game.GiveVoteForVictim(user);
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