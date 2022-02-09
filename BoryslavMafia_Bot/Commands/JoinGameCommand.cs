using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public class JoinGameCommand : Command
{
    public override string Name => "/start";

    public override async void Execute(Message message, TelegramBotClient client)
    {
        var groupChatId = message.Text.Substring(message.Text.IndexOf(' ') + 1);

        if (groupChatId[0] != '-') // all chat ids start with '-'
            return;

        var chat = await client.GetChatAsync(groupChatId);
        var chatName = chat.Title;

        var user = message.From;

        if (!GamesManager.IsPlayerAlreadyInGame(user))
        {
            await client.SendTextMessageAsync(message.Chat.Id, "Ви приєдналися до гри у " + chatName);

            Console.WriteLine("ID: " + user.Id
                + " (" + user.FirstName + " " + user.LastName + ") joined game in chat "
                + groupChatId + " (" + chatName + ")\n");

            Game game = await GamesManager.GetGame(long.Parse(groupChatId));
            game.AddPlayer(user);

            var msg = game.JoinGameMessage;

            #region build edited message
            string editedMessage = "<b>Проводиться набір до гри.</b>\nУчасники: ";
            foreach(Player player in game.Players)
            {
                editedMessage += string.Format("<a href=\"tg://user?id={0}\">", player.User.Id);
                editedMessage += player.User.FirstName;
                editedMessage += "</a> ";
            }
            editedMessage = editedMessage.Remove(editedMessage.Length - 1, 1);
            editedMessage += "\nВсього учасників: <b>";
            editedMessage += game.Players.Count;
            editedMessage += "</b>";
            #endregion

            await client.EditMessageTextAsync(
                chat.Id,
                msg.MessageId,
                editedMessage,
                replyMarkup: msg.ReplyMarkup,
                parseMode: ParseMode.Html
                );
        }
        else
        {
            await client.SendTextMessageAsync(message.Chat.Id, $"Ви вже приєдналися до гри");
        }          
    }
}