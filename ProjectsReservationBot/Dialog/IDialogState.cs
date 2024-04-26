using Telegram.Bot.Types;

namespace ProjectsReservationBot.Dialog;
/// <summary>
/// Интерфейс, определяющий поведение диалогов.
/// </summary>
public interface IDialogState
{
    /// <summary>
    /// Выполняет действия, необходимые при входе в состояние.
    /// </summary>
    Task OnEnter();

    /// <summary>
    /// Отправляет ответ на сообщение пользователя.
    /// </summary>
    Task Reply(Message message);
}
