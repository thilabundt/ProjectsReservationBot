namespace ProjectsReservationBot.Services;
/// <summary>
/// Сервис для передачи данных.
/// </summary>
public class DataTransmitter
{
    private readonly GoogleSheetsService googleSheetsService = new();
    private readonly Mapper mapper = new();

    private const int DefaultMaxTeamsPerGroupCount = 7;
    private const int DefaultMaxProjectReservationsCount = 3;
    private const int DefaultMaxProjectReservationsSameGroupCount = 1;

    /// <summary>
    /// Возвращает пользователя с указанным id.
    /// </summary>
    /// <returns>
    /// <see cref="User"/> или <see langword="null"/>, если пользователя с указанным id не существует.
    /// </returns>
    public async Task<User?> GetUserAsync(long userId) {
        var usersValues = await googleSheetsService.GetUsersValuesAsync();
        if (usersValues is null) {
            return null;
        }

        var users = mapper.MapUsersFromValues(usersValues);
        return users
            .FirstOrDefault(user => user.Id == userId.ToString());
    }

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> в случае успеха и <see langword="false"/>, если пользователь с таким id уже существует.
    /// </returns>
    public async Task<bool> TryCreateUserAsync(long userId, UserState state) {
        var existingUser = await GetUserAsync(userId);
        if (existingUser is not null) {
            return false;
        }

        var user = new User {
            Id = userId.ToString(),
            State = state
        };

        var userValues = mapper.MapValuesFromUser(user);
        await googleSheetsService.AddUserValuesAsync(userValues);
        return true;
    }

    /// <summary>
    /// Обновляет пользователя с указанным id.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, в случае успеха, <see langword="false"/>, если отсутствует пользователь, с указанным id.
    /// </returns>
    public async Task<bool> UpdateUserAsync(long userId, UserState state) {
        return await googleSheetsService.TryUpdateUserValuesAsync(userId.ToString(), state.ToString());
    }

    /// <summary>
    /// Возвращает максимальное количество команд в одной учебной группе.
    /// </summary>
    public async Task<int> GetMaxTeamsPerGroupCountAsync() {
        var constraintValue = await googleSheetsService.GetMaxTeamsPerGroupValueAsync();
        if (int.TryParse(constraintValue?.ToString(), out int maxTeamsPerGroupCount)) {
            return maxTeamsPerGroupCount;
        }

        return DefaultMaxTeamsPerGroupCount;
    }

    /// <summary>
    /// Возвращает максимальное количество резервов одного проекта.
    /// </summary>
    public async Task<int> GetMaxProjectReservationsCountAsync() {
        var constraintValue = await googleSheetsService.GetMaxProjectReservationsValueAsync();
        if (int.TryParse(constraintValue?.ToString(), out int maxProjectReservationsCount)) {
            return maxProjectReservationsCount;
        }

        return DefaultMaxProjectReservationsCount;
    }

    /// <summary>
    /// Возвращает максимальное количество резервов одного проекта в одной учебной группе.
    /// </summary>
    public async Task<int> GetMaxProjectReservationsSameGroupCountAsync() {
        var constraintValue = await googleSheetsService.GetMaxProjectReservationsSameGroupValueAsync();
        if (int.TryParse(constraintValue?.ToString(), out int maxProjectReservationsSameGroupCount)) {
            return maxProjectReservationsSameGroupCount;
        }

        return DefaultMaxProjectReservationsSameGroupCount;
    }

    /// <summary>
    /// Возвращает количество зарегистрированных команд в учебной группе с указанным названием.
    /// </summary>
    public async Task<int> GetGroupTeamsCountAsync(string groupName) {
        var teamsValues = await googleSheetsService.GetTeamsValuesAsync();
        if (teamsValues == null) {
            return 0;
        }

        var teams = mapper.MapTeamsFromValues(teamsValues);
        var groupTeams = teams
            .Where(team => team.GroupName == groupName);

        return groupTeams.Count();
    }

    /// <summary>
    /// Регистрирует команду с указанными параметрами.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, в случае успеха, и <see langword="false"/>, если пользователь с указанным id уже зарегистрировал команду.
    /// </returns>
    public async Task<bool> TryRegisterTeamAsync(long leaderTelegramId, string leaderFullName, string leaderPhoneNumber, string groupName) {
        var existingTeam = await GetTeamAsync(leaderTelegramId);
        if (existingTeam is not null) {
            return false;
        }

        var newTeam = new Team {
            LeaderTelegramId = leaderTelegramId.ToString(),
            LeaderFullName = leaderFullName,
            LeaderPhoneNumber = leaderPhoneNumber,
            GroupName = groupName
        };

        var teamValues = mapper.MapValuesFromTeam(newTeam);
        await googleSheetsService.AddTeamsValuesAsync(teamValues);
        return true;
    }

    /// <summary>
    /// Возвращает номера всех существующих проектов.
    /// </summary>
    public async Task<List<string>> GetAllProjectsNumbersAsync() {
        var projectsValues = await googleSheetsService.GetProjectsValuesAsync();
        if (projectsValues == null) {
            return new List<string>();
        }

        var projects = mapper.MapProjectsFromValues(projectsValues);

        return projects
            .Select(project => project.Number)
            .ToList();
    }

    /// <summary>
    /// Возвращает количество команд, выбравших проект с указанным номером.
    /// </summary>
    public async Task<int> GetProjectTeamsCountAsync(string projectNumber) {
        var teamsValues = await googleSheetsService.GetTeamsValuesAsync();
        if (teamsValues == null) {
            return 0;
        }

        var teams = mapper.MapTeamsFromValues(teamsValues);
        return teams
            .Count(team => team.ProjectNumber == projectNumber);
    }

    /// <summary>
    /// Возвращает количество команд в учебной группе лидера с указанным id, выбравших проект с указанным номером.
    /// </summary>
    /// <returns>
    /// <see cref="int"/>, или <see langword="null"/>, если отсутствует команда с лидером с указанным id.
    /// </returns>
    public async Task<int?> GetProjectTeamsSameGroupCountAsync(string projectNumber, long teamLeaderTelegramId) {
        var userTeam = await GetTeamAsync(teamLeaderTelegramId);
        if (userTeam == null) {
            return null;
        }

        var teamsValues = await googleSheetsService.GetTeamsValuesAsync();
        if (teamsValues == null) {
            return 0;
        }

        var teams = mapper.MapTeamsFromValues(teamsValues);
        return teams
            .Count(team => team.ProjectNumber != string.Empty &&
                           team.ProjectNumber == projectNumber &&
                           team.GroupName == userTeam.GroupName);
    }

    /// <summary>
    /// Резервирует проект с указанным номером за командой, с лидером с указанным id. 
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, в случае успеха, <see langword="false"/>, если отсутствует команда, с лидером с указанным id.
    /// </returns>
    public async Task<bool> TryReserveProjectAsync(string projectNumber, long teamLeaderTelegramId) {
        return await googleSheetsService.TrySetTeamProjectValueAsync(projectNumber, teamLeaderTelegramId.ToString());
    }

    /// <summary>
    /// Возвращает название проекта на основе его номера.
    /// </summary>
    /// <returns>
    /// <see cref="string"/> или <see langword="null"/>, если проект с таким номером не существует
    /// </returns>
    public async Task<string?> GetProjectNameAsync(string projectNumber) {
        var projectsValues = await googleSheetsService.GetProjectsValuesAsync();
        if (projectsValues == null) {
            return null;
        }

        var projects = mapper.MapProjectsFromValues(projectsValues);
        return projects
            .FirstOrDefault(project => project.Number == projectNumber)?
            .Name;
    }

    /// <summary>
    /// Возвращает название проекта, выбранного командой, на основе id её лидера.
    /// </summary>
    /// <returns>
    /// <see cref="string"/> или <see langword="null"/>, если нет команды с лидером с указанным id или
    /// нет проекта с указанным номером.
    /// </returns>
    public async Task<string?> GetProjectNameAsync(long teamLeaderTelegramId) {
        var team = await GetTeamAsync(teamLeaderTelegramId);
        if (team is null) {
            return null;
        }

        var projectsValues = await googleSheetsService.GetProjectsValuesAsync();
        if (projectsValues == null) {
            return null;
        }

        var projects = mapper.MapProjectsFromValues(projectsValues);
        return projects
            .FirstOrDefault(project => project.Number == team.ProjectNumber)?
            .Name;
    }

    /// <summary>
    /// Определяет, существует ли проект с указанным номером.
    /// </summary>
    public async Task<bool> IsProjectNumberValidAsync(string projectNumber) {
        var projectsValues = await googleSheetsService.GetProjectsValuesAsync();
        if (projectsValues == null) {
            return false;
        }

        var projects = mapper.MapProjectsFromValues(projectsValues);
        return projects
            .Any(project => project.Number == projectNumber);
    }

    /// <summary>
    /// Возвращает команду <see cref="Team"/>, с лидером с указанным id.
    /// </summary>
    /// <returns>
    /// <see cref="Team"/> или <see langword="null"/>, если отсутствует команда с лидером с указанным id.
    /// </returns>
    public async Task<Team?> GetTeamAsync(long teamLeaderTelegramId) {
        var teamsValues = await googleSheetsService.GetTeamsValuesAsync();
        if (teamsValues == null) {
            return null;
        }

        var teams = mapper.MapTeamsFromValues(teamsValues);
        return teams
            .FirstOrDefault(team => team.LeaderTelegramId == teamLeaderTelegramId.ToString());
    }
}
