using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class MessageController
{
    private static TelegramBotClient client;
    public static async Task BotOnMessageReceived(Message message)
    {
        //Console.WriteLine($"Receive message type: {message.Type}");
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