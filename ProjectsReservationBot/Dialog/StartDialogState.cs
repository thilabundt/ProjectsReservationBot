using Telegram.Bot;
using Telegram.Bot.Types;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Входное (начальное) состояние диалога.
/// </summary>
public class StartDialogState : IDialogState
{
    public Dialog Dialog { get; }

    public StartDialogState(Dialog dialog) {
        Dialog = dialog;
    }

    public async Task OnEnter() {
        var existingTeam = await Dialog.DataTransmitter.GetTeamAsync(Dialog.UserId);
        if (existingTeam is not null) {
            if (existingTeam.ProjectNumber == string.Empty) {
                await Dialog.BotClient.SendTextMessageAsync(Dialog.UserId, "Вы уже зарегистрированы!\n" +
                                                                           "Выберите проект");
                await Dialog.SetState(new ProjectSelectionDialogState(Dialog));
                return;
            }

            await Dialog.BotClient.SendTextMessageAsync(Dialog.UserId, "Вы уже зарегистрировались и выбрали проект!\n\n");
            await Dialog.SetState(new CompletedDialogState(Dialog));
            return;
        }

        await Dialog.SetState(new RegistrationDialogState(Dialog));
    }

    public async Task Reply(Message message) {
        var user = message.From;
        var messageText = message.Text ?? string.Empty;
        if (user is null) {
            return;
        }

        if (messageText != "/start") {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Для начала работы с ботом воспользуйтесь командой /start");
            return;
        }

        await Dialog.SetState(new RegistrationDialogState(Dialog));
    }
}
