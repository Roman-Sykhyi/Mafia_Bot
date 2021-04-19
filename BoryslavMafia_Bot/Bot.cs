using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class Bot
{
    public static CancellationTokenSource CTS;
    public static IReadOnlyList<Command> Commands { get => commandsList.AsReadOnly(); }

    private static TelegramBotClient client;
    private static List<Command> commandsList;

    public static async Task<TelegramBotClient> Get()
    {
        if (client != null)
        {
            return client;
        }
        #region magic
        client = new TelegramBotClient(BotConfiguration.Key);
        var me = await client.GetMeAsync();
        Console.Title = me.Username;

        CTS = new CancellationTokenSource();

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        client.StartReceiving(
            new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
            CTS.Token
        );
        Console.WriteLine($"Start listening for @{me.Username}\n");
        #endregion

        commandsList = new List<Command>();

        commandsList.Add(new StartGameCommand());
        commandsList.Add(new JoinGameCommand());
        //TODO add more commands here

        return client;
    }
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => MessageController.BotOnMessageReceived(update.Message),
            //UpdateType.EditedMessage => BotOnMessageReceived(update.Message),
            UpdateType.CallbackQuery => CallbackQueryController.BotOnCallbackQueryReceived(update.CallbackQuery, client),
            //UpdateType.InlineQuery => BotOnInlineQueryReceived(update.InlineQuery),
            //UpdateType.ChosenInlineResult => BotOnChosenInlineResultReceived(update.ChosenInlineResult),
            //UpdateType.Unknown:
            //UpdateType.ChannelPost:
            //UpdateType.EditedChannelPost:
            //UpdateType.ShippingQuery:
            //UpdateType.PreCheckoutQuery:
            //UpdateType.Poll:
            _ => UnknownUpdateHandlerAsync(update)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(botClient, exception, cancellationToken);
        }
    }

    public static async Task UnknownUpdateHandlerAsync(Update update)
    {
        Console.WriteLine($"Unknown update type: {update.Type}");
    }

    public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
    }
}