using System;
using Telegram.Bot;
using Telegram.Bot.Types;

public class JoinGameCommand : Command
{
    public override string Name => "/start";

    public override async void Execute(Message message, TelegramBotClient client)
    {
        var groupChatId = message.Text.Substring(message.Text.IndexOf(' ') + 1);

        if (groupChatId[0] != '-')
            return;

        var chat = await client.GetChatAsync(groupChatId);
        var chatName = chat.Title;

        if (!GamesManager.IsPlayerAlreadyInGame(message.From))
        {
            await client.SendTextMessageAsync(message.Chat.Id, "Ви приєдналися до гри у " + chatName);

            Console.WriteLine("ID: " + message.From.Id
                + " (" + message.From.FirstName + " " + message.From.LastName + ") joined game in chat "
                + groupChatId + " (" + chatName + ")");

            Game game = GamesManager.GetGame(long.Parse(groupChatId));
            game.AddPlayer(message.From);
        }
        else
        {
            await client.SendTextMessageAsync(message.Chat.Id, "Ви вже приєдналися до гри");
        }          
    }
}
