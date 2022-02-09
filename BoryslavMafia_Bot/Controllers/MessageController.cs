using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class MessageController
{
    private static TelegramBotClient client;
    public static async Task BotOnMessageReceived(Message message)
    {

        Game game = await GamesManager.GetGame(message.Chat.Id);

        if(game != null)
        {
            Player player = game.AllowedInChat.FirstOrDefault(p => p.User.Id == message.From.Id);

            if (player == null)
            {
                await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                return;
            }
        }

        if (message.Type != MessageType.Text)
            return;

        var commands = Bot.Commands;

        client = await Bot.Get();

        foreach (var command in commands)
        {
            if (command.Contains(message.Text))
            {
                command.Execute(message, client);
                break;
            }
        }
    }
}