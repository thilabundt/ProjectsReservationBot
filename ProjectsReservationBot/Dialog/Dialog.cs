using ProjectsReservationBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Диалог с пользователем, имеющий состояние.
/// </summary>
public class Dialog
{
    public long UserId { get; }
    public ITelegramBotClient BotClient { get; }
    public DataTransmitter DataTransmitter { get; }
    public IDialogState? State { get; set; }

    public Dialog(long userId, ITelegramBotClient botClient, DataTransmitter dataTransmitter) {
        UserId = userId;
        BotClient = botClient;
        DataTransmitter = dataTransmitter;
    }

    /// <summary>
    /// Устанавливает состояние диалога и вызывает метод <see cref="IDialogState.OnEnter"/> нового состояния.
    /// </summary>
    public async Task SetState(IDialogState state) {
        State = state;
        var newUserState = state switch {
            StartDialogState => UserState.Start,
            RegistrationDialogState => UserState.Registration,
            ProjectSelectionDialogState => UserState.ProjectSelection,
            CompletedDialogState => UserState.CompletedSelection,
            _ => throw new ArgumentOutOfRangeException()
        };
        await DataTransmitter.UpdateUserAsync(UserId, newUserState);
        await State.OnEnter();
    }

    public async Task Reply(Message message) {
        var user = message.From;
        if (user is null) {
            return;
        }

        if (State is null) {
            await BotClient.SendTextMessageAsync(user.Id, "Для начала работы с ботом воспользуйтесь командой /start");
            return;
        }

        await State.Reply(message);
    }
}
