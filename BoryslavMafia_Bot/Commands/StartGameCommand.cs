using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class StartGameCommand : Command
{
    public override string Name => "/startgame";

    public override async void Execute(Message message, TelegramBotClient client)
    {
        var chatId = message.Chat.Id;
        var chatType = message.Chat.Type;

        if (chatType == ChatType.Group)
        {           
            if (!await GamesManager.GameExists(chatId))
            {
                InitiateNewGame(message, client, chatId);
            }
            else
            {
                await client.SendTextMessageAsync(chatId, "Гру вже запущено");
            }
        }
        else
        {
            await client.SendTextMessageAsync(chatId, "Цю команду можна використовувати тільки в груповому чаті");
        }
    }

    private async void InitiateNewGame(Message message, TelegramBotClient client, long chatId)
    {
        var link = string.Format("https://t.me/{0}?start={1}", BotConfiguration.Name, chatId);
        var inlineKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Приєднатися до гри", link));

        var keyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData(CallbackQueryType.GameStarter.ToString()));

        var msg = await client.SendTextMessageAsync
            (
            chatId,
            "<b>Проводиться набір до гри.</b>",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
            );

        GamesManager.NewGame(chatId, msg);

        Console.WriteLine($"New game has been initiated in chat {chatId}\n");

        await StartNotifying(msg, client, chatId);
        await StartGame(client, chatId);
    }

    private async Task StartNotifying(Message messageToReply, TelegramBotClient client, long chatId)
    {
        int remainingTime = GameConfiguration.TimeToStartGame;

        while (remainingTime > 0)
        {
            await client.SendTextMessageAsync
            (
            chatId,
            string.Format("Проводиться набір до гри.\n" +
            "До завершення реєстрації <b>{0} секунд</b>", remainingTime),
            parseMode: ParseMode.Html,
            replyToMessageId: messageToReply.MessageId
            );

            await Task.Delay(15000);
            remainingTime -= 15;
        }
    }

    private async Task StartGame(TelegramBotClient client, long chatId)
    {
        Game game = await GamesManager.GetGame(chatId);

        var chat = await client.GetChatAsync(chatId);
        var chatName = chat.Title;

        bool canStartGame = game.Players.Count >= GameConfiguration.MinimumPlayers;

        if (canStartGame)
        {
            await client.SendTextMessageAsync(chatId, "<b>Гра починається</b>", parseMode: ParseMode.Html);
            Console.WriteLine($"\nStarted game in chat: {chatId} ({chatName})\n");

            await game.StartGame();
        }
        else
        {
            await client.SendTextMessageAsync(chatId, "<b>Недостатньо гравців для початку гри</b>", parseMode: ParseMode.Html);
            Console.WriteLine($"Not enought players to start game in chat: {chatId} ({chatName}) \nDeleting game instance\n");

            GamesManager.ForceEndGame(game);
        }
    }
}