using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Состояние диалога, в котором пользователь должен выбрать номер проекта.
/// </summary>
public class ProjectSelectionDialogState : IDialogState
{
    public Dialog Dialog { get; }

    public ProjectSelectionDialogState(Dialog dialog) {
        Dialog = dialog;
    }

    public async Task OnEnter() {
        var projects = await Dialog.DataTransmitter.GetAllProjectsAsync();
        var projectsMessagesText = FormatProjectsText(projects);

        var noSuitableProjectsButton = new InlineKeyboardButton("Я не нашёл необходимую мне проектную заявку") {
            CallbackData = "noSuitableProjectsButton"
        };
        var inlineKeyboard = new InlineKeyboardMarkup(noSuitableProjectsButton);

        await Dialog.BotClient.SendTextMessageAsync(
            Dialog.UserId,
            "\u270f\ufe0f Для легкого восприятия информации с витрины проектов просим Вас вспомнить следующие интересные факты:\n\n" +
            "\u2714\ufe0f <strong>Цель проекта</strong> раскрывает то, что хочет увидеть заказчик в результате вашей работы\n" +
            "\u2714\ufe0f <strong>Носитель проблемы</strong> – это человек, которому не известны возможные решения, необходимые для достижения поставленной цели\n" +
            "(Обращаем Ваше внимание на тот факт, что носитель проблемы одновременно может являться заказчиком)\n" +
            "\u2714\ufe0f <strong>Барьер проекта</strong> отвечает на следующий вопрос: «Что мешает носителю проблемы достичь поставленную цель?»\n" +
            "\u2714\ufe0f <strong>Существующие решения</strong> - это перечень инструментов, методов, подходов и готовых решений, неподходящих для выполнения поставленной цели\n\n" +
            "Ниже приведен список доступных для выбора проектов.\n" +
            "Введите, пожалуйста, <strong><u>номер</u></strong> выбранного Вами проекта.",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard
        );

        foreach (var messageText in projectsMessagesText) {
            await Dialog.BotClient.SendTextMessageAsync(Dialog.UserId, messageText);
        }
    }

    public async Task Reply(Message message) {
        var user = message.From;
        var messageText = message.Text ?? string.Empty;
        if (user is null) {
            return;
        }

        if (!await Dialog.DataTransmitter.IsProjectNumberValidAsync(messageText)) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Проект с указанным номером не существует");
            return;
        }

        var projectTeamsCount = await Dialog.DataTransmitter.GetProjectTeamsCountAsync(messageText);
        var projectTeamsSameGroupCount = await Dialog.DataTransmitter.GetProjectTeamsSameGroupCountAsync(messageText, user.Id);

        if (projectTeamsSameGroupCount == null) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Вы не зарегистрированы!\n" +
                                                                 "Воспользуйтесь командой /start для просмотра статуса");
            return;
        }

        var maxProjectReservationsCount = await Dialog.DataTransmitter.GetMaxProjectReservationsCountAsync();
        var maxProjectReservationsSameGroupCount = await Dialog.DataTransmitter.GetMaxProjectReservationsSameGroupCountAsync();
        if (projectTeamsCount >= maxProjectReservationsCount || projectTeamsSameGroupCount >= maxProjectReservationsSameGroupCount) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Выбранный вами проект уже занят. Сделайте другой выбор");
            return;
        }

        var reservationIsSuccessful = await Dialog.DataTransmitter.TryReserveProjectAsync(messageText, user.Id);
        if (!reservationIsSuccessful) {
            await Dialog.BotClient.SendTextMessageAsync(user.Id, "Вы не зарегистрированы!\n" +
                                                                 "Воспользуйтесь командой /start для просмотра статуса");
            return;
        }

        var projectName = await Dialog.DataTransmitter.GetProjectNameAsync(messageText);
        await Dialog.BotClient.SendTextMessageAsync(user.Id, $"Спасибо за сделанный выбор! Желаем успехов в реализации проекта \"{projectName}\"");
        await Dialog.SetState(new CompletedDialogState(Dialog));
    }

    private List<string> FormatProjectsText(List<Project> projects) {
        var result = new List<string>();
        var stringBuilder = new StringBuilder();
        foreach (var project in projects) {
            var projectLine = $"{project.Number} - \"{project.Name}\"\n";
            if (stringBuilder.Length + projectLine.Length > 4096) {
                result.Add(stringBuilder.ToString());
                stringBuilder.Clear();
            }
            stringBuilder.Append(projectLine);
        }
        if (stringBuilder.Length > 0) {
            result.Add(stringBuilder.ToString());
        }
        return result;
    }
}
