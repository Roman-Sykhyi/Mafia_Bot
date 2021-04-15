using System;
using System.Threading.Tasks;
using Telegram.Bot;

public static class Program
{
    private static TelegramBotClient client;

    public static async Task Main()
    {
        client = await Bot.Get();

        Console.ReadLine();
        // Send cancellation request to stop bot
        Bot.CTS.Cancel();
    }      
}
