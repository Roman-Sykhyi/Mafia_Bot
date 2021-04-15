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
        }
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
