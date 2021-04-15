using System;
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
                var link = string.Format("https://t.me/{0}?start={1}", Configuration.Name, chatId);
                var inlineKeyboard = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl("Приєднатися до гри", link));

                await client.SendTextMessageAsync
                    (
                    chatId,
                    "Проводиться набір до гри.\n" +
                    "До завершення реєстрації 60 секунд",
                    replyMarkup: inlineKeyboard
                    );

                GamesManager.NewGame(chatId);

                Console.WriteLine($"New game has been started in chat {chatId}");
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
}
