using Telegram.Bot;
using Telegram.Bot.Types;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Состояние диалога, в котором пользователь уже выбрал проект.
/// </summary>
public class CompletedDialogState : IDialogState
{
    public Dialog Dialog { get; }

    public CompletedDialogState(Dialog dialog) {
        Dialog = dialog;
    }

    public Task OnEnter() {
        return Task.CompletedTask;
    }

    public async Task Reply(Message message) {
        var user = message.From;
        if (user is null) {
            return;
        }

        var reservedProjectName = await Dialog.DataTransmitter.GetProjectNameAsync(user.Id);
        if (reservedProjectName is null) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Вы не зарегистрированы или выбранный Вами проект более не существует\n" +
                                                                 "Воспользуйтесь командой /start для просмотра статуса");
            return;
        }
        await Dialog.BotClient.SendTextMessageAsync(user.Id, $"Вы уже выбрали проект \"{reservedProjectName}\". Изменение заявки невозможно");
    }
}
