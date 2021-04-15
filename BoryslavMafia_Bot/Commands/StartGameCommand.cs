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
            if (!GamesManager.GameExists(chatId))
            {
                StartNewGame(message, client, chatId);
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

    private async void StartNewGame(Message message, TelegramBotClient client, long chatId)
    {
        var link = string.Format("https://t.me/{0}?start={1}", Configuration.Name, chatId);
        var inlineKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Приєднатися до гри", link));

        var msg = await client.SendTextMessageAsync
            (
            chatId,
            "<b>Проводиться набір до гри.</b>",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
            );

        GamesManager.NewGame(chatId, msg);

        Console.WriteLine($"New game has been started in chat {chatId}");

        StartNotifying(msg, client, chatId); 
    }

    private async void StartNotifying(Message message, TelegramBotClient client, long chatId)
    {
        int remainingTime = 60;

        while (remainingTime > 0)
        {
            await client.SendTextMessageAsync
            (
            chatId,
            string.Format("Проводиться набір до гри.\n" +
            "До завершення реєстрації <b>{0} секунд</b>", remainingTime),
            parseMode: ParseMode.Html,
            replyToMessageId: message.MessageId
            );

            await Task.Delay(15000);
            remainingTime -= 15;
        }

        await client.SendTextMessageAsync(chatId, "Реєстрацію завершено");
    }
}
