namespace ProjectsReservationBot.Services;
/// <summary>
/// Сервис для преобразования значений в сущности и наоборот
/// </summary>
public class Mapper
{
    /// <summary>
    /// Преобразует набор значений в <see cref="User"/>.
    /// </summary>
    public List<User> MapUsersFromValues(IList<IList<object>> values) {
        var users = new List<User>();

        foreach (var value in values) {
            if (value.Count >= 2) {
                var id = value[0].ToString() ?? string.Empty;
                Enum.TryParse<UserState>(value[1].ToString(), out var state);

                var user = new User {
                    Id = id,
                    State = state
                };
                users.Add(user);
            }
        }

        return users;
    }

    /// <summary>
    /// Преобразует <see cref="User"/> в набор значений.
    /// </summary>
    public IList<object> MapValuesFromUser(User user) {
        var values = new List<object> {
            user.Id,
            user.State.ToString(),
        };

        return values;
    }

    /// <summary>
    /// Преобразует набор значений в <see cref="Project"/>.
    /// </summary>
    public List<Project> MapProjectsFromValues(IList<IList<object>> values) {
        return values
            .Where(value => value.Count >= 2)
            .Select(value => new Project {
                Number = value[0].ToString() ?? string.Empty,
                Name = value[1].ToString() ?? string.Empty
            })
            .ToList();
    }

    /// <summary>
    /// Преобразует набор значений в <see cref="Team"/>.
    /// </summary>
    public List<Team> MapTeamsFromValues(IList<IList<object>> values) {
        return values
            .Where(value => value.Count >= 4)
            .Select(value => new Team {
                LeaderTelegramId = value[0].ToString() ?? string.Empty,
                LeaderFullName = value[1].ToString() ?? string.Empty,
                LeaderPhoneNumber = value[2].ToString() ?? string.Empty,
                GroupName = value[3].ToString() ?? string.Empty,
                ProjectNumber = value.Count >= 5 ? value[4].ToString() ?? string.Empty : string.Empty
            })
            .ToList();
    }

    /// <summary>
    /// Преобразует <see cref="Team"/> в набор значений.
    /// </summary>
    public IList<object> MapValuesFromTeam(Team team) {
        var values = new List<object> {
            team.LeaderTelegramId,
            team.LeaderFullName,
            team.LeaderPhoneNumber,
            team.GroupName,
            team.ProjectNumber
        };

        return values;
    }
}
