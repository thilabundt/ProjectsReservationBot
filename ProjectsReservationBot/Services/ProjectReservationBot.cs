using NLog;
using ProjectsReservationBot.Dialog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ProjectsReservationBot.Services;

/// <summary>
/// Телеграм бот для автоматизации распределения проектов.
/// </summary>
public class ProjectReservationBot
{
    private readonly DataTransmitter dataTransmitter = new();
    private readonly TelegramBotClient botClient = new(Config.TELEGRAM_BOT_TOKEN);
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly ILogger logger = LogManager.GetCurrentClassLogger();

    public ProjectReservationBot() {
        LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
            builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile("log.txt");
        });

        cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Запускает бота.
    /// </summary>
    public void StartReceiving() {
        var receiverOptions = new ReceiverOptions {
            ThrowPendingUpdates = true,
        };

        botClient.StartReceiving(
            HandleUpdate,
            HandleError,
            receiverOptions,
            cancellationTokenSource.Token
        );
    }

    /// <summary>
    /// Останавливает бота.
    /// </summary>
    public void Stop() {
        cancellationTokenSource.Cancel();
    }

    private async Task HandleUpdate(ITelegramBotClient _, Update update, CancellationToken cancellationToken) {
        try {
            switch (update.Type) {
                case UpdateType.Message:
                    await HandleMessage(update.Message!);
                    break;

                case UpdateType.CallbackQuery:
                    await HandleButton(update.CallbackQuery!);
                    break;
            }
        }
        catch (Exception exception) {
            logger.Error(exception);
        }
    }

    /// <summary>
    /// Обрабатывает ошибки.
    /// </summary>
    private Task HandleError(ITelegramBotClient _, Exception exception, CancellationToken cancellationToken) {
        logger.Error(exception);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Обрабатывает сообщения от пользователей.
    /// </summary>
    private async Task HandleMessage(Message message) {
        var user = message.From;
        if (user is null) {
            return;
        }

        var dialog = new Dialog.Dialog(user.Id, botClient, dataTransmitter);

        var existingUser = await dataTransmitter.GetUserAsync(user.Id);
        if (existingUser is null) {
            if (message.Text != "/start") {
                await botClient.SendTextMessageAsync(user.Id, "Для начала работы с ботом воспользуйтесь командой /start");
                return;
            }

            await dataTransmitter.TryCreateUserAsync(user.Id, UserState.Start);
            await dialog.SetState(new StartDialogState(dialog));
            return;
        }

        if (message.Text == "/start") {
            await dataTransmitter.UpdateUserAsync(user.Id, UserState.Start);
            await dialog.SetState(new StartDialogState(dialog));
            return;
        }

        IDialogState dialogState = existingUser.State switch {
            UserState.Start => new StartDialogState(dialog),
            UserState.Registration => new RegistrationDialogState(dialog),
            UserState.ProjectSelection => new ProjectSelectionDialogState(dialog),
            UserState.CompletedSelection => new CompletedDialogState(dialog),
            _ => throw new ArgumentOutOfRangeException()
        };
        dialog.State = dialogState;

        await dialog.Reply(message);
    }

    /// <summary>
    /// Обрабатывает нажатия на Inline кнопки.
    /// </summary>
    private async Task HandleButton(CallbackQuery query) {
        if (query.Data == "noSuitableProjectsButton") {
            await SendPersonInChargeNotification(query);
        }
    }

    /// <summary>
    /// Отправляет пользователю уведомление с контактами ответственного по проектной деятельности.
    /// </summary>
    private async Task SendPersonInChargeNotification(CallbackQuery query) {
        var notificationText = "Просим Вас обратиться к ответственному по проектной деятельности за связь с партнёрами и производством - " +
            "старший преподаватель кафедры «ЖДСТУ» Янев Живко";

        await botClient.AnswerCallbackQueryAsync(query.Id, notificationText, showAlert: true);
    }
}

