using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Состояние диалога, в котором пользователь регистрируется в системе.
/// </summary>
public class RegistrationDialogState : IDialogState
{
    private const string CredentialsPattern = @"^(?<leaderFullName>[^,]+),\s?(?<leaderPhoneNumber>\+7\s\d{3}\s\d{3}-\d{2}-\d{2}),\s?(?<groupName>[А-ЯЁ]{3}-\d{3})$";

    public Dialog Dialog { get; }

    public RegistrationDialogState(Dialog dialog) {
        Dialog = dialog;
    }

    public async Task OnEnter() {
        await Dialog.BotClient.SendTextMessageAsync(Dialog.UserId, "Добро пожаловать на витрину проектов института управления и цифровых технологий (ИУЦТ) - РУТ (МИИТ)\ud83d\ude89\n\n" +
                                                                   "Для выбора проектной заявки из витрины проектов просим Вас указать следующие данные <strong>через запятую</strong>:\n\n" +
                                                                   "\u2714\ufe0f Фамилия Имя Отчество - полностью, без сокращений\n" +
                                                                   "\u2714\ufe0f Контактный номер - в формате +7 XXX XXX-XX-XX (с пробелами и тире)\n" +
                                                                   "\u2714\ufe0f Группа - в формате XXX-000, где XXX - заглавные буквы\n\n" +
                                                                   "Пример заполнения:\n" +
                                                                   "Иванов Иван Иванович, +7 123 456-78-90, УЭИ-123\n\n" +
                                                                   "\u2755\u2755\u2755<strong>Будьте внимательны - в случае неправильного заполнения формы <u>внести изменения не получится</u></strong>",
            parseMode: ParseMode.Html);
    }

    public async Task Reply(Message message) {
        var user = message.From;
        var messageText = message.Text ?? string.Empty;
        if (user is null) {
            return;
        }

        if (!CredentialsAreValid(messageText, out var leaderFullName, out var leaderPhoneNumber, out var groupName)) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Некорректный формат данных!\n" +
                                                                 "Пример заполнения:\n\n" +
                                                                 "Иванов Иван Иванович, +7 123 456-78-90, УЭИ-123");
            return;
        }

        var maxTeamsPerGroupCount = await Dialog.DataTransmitter.GetMaxTeamsPerGroupCountAsync();
        var groupTeamsCount = await Dialog.DataTransmitter.GetGroupTeamsCountAsync(groupName);
        if (groupTeamsCount >= maxTeamsPerGroupCount) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, $"Лимит команд для группы исчерпан!\n" +
                                                                 $"В группе не может быть больше {maxTeamsPerGroupCount} команд");
            return;
        }

        var registrationIsSuccessful = await Dialog.DataTransmitter.TryRegisterTeamAsync(user.Id, leaderFullName, leaderPhoneNumber, groupName);
        if (!registrationIsSuccessful) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Вы уже зарегистрированы!\n" +
                                                                 "Воспользуйтесь командой /start для просмотра статуса");
            return;
        }

        await Dialog.BotClient.SendTextMessageAsync(user.Id, "Вы успешно зарегистрировались!\n\n");
        await Dialog.SetState(new ProjectSelectionDialogState(Dialog));
    }

    /// <summary>
    /// Проверяет, соответствует ли форма заданному стандарту.
    /// </summary>
    private bool CredentialsAreValid(string mergedCredentials, out string leaderFullName, out string leaderPhoneNumber, out string groupName) {
        var match = Regex.Match(mergedCredentials, CredentialsPattern);
        if (!match.Success) {
            leaderFullName = string.Empty;
            leaderPhoneNumber = string.Empty;
            groupName = string.Empty;
            return false;
        }

        leaderFullName = match.Groups["leaderFullName"].Value.Trim();
        leaderPhoneNumber = match.Groups["leaderPhoneNumber"].Value.Trim();
        groupName = match.Groups["groupName"].Value.Trim();
        return true;
    }
}
