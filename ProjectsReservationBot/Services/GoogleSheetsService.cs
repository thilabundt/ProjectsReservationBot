using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace ProjectsReservationBot.Services;
/// <summary>
/// Сервис для взаимодействия с Google Sheets.
/// </summary>
public class GoogleSheetsService
{
    private readonly string projectsShowcaseSheetId = Config.GOOGLE_SPREADSHEET_ID;

    private const string ProjectsSheetName = "Проекты";
    private const string TeamsSheetName = "Команды";
    private const string ConstraintsSheetName = "Ограничения";
    private const string UsersDatabaseSheetName = "База пользователей";

    private readonly SheetsService sheetsService;

    public GoogleSheetsService() {
        GoogleCredential credential;
        using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read)) {
            credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }

        sheetsService = new SheetsService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = "ProjectsShowcase"
        });
    }

    /// <summary>
    /// Создает нового пользователя.
    /// </summary>
    public async Task AddUserValuesAsync(IList<object> values) {
        const string appendRange = $"{UsersDatabaseSheetName}!A2:B";
        var valueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
        var insertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        var requestBody = new ValueRange {
            Values = new List<IList<object>>()
        };
        requestBody.Values.Add(values);

        var appendRequest = sheetsService.Spreadsheets.Values.Append(requestBody, projectsShowcaseSheetId, appendRange);
        appendRequest.ValueInputOption = valueInputOption;
        appendRequest.InsertDataOption = insertDataOption;

        await appendRequest.ExecuteAsync();
    }

    /// <summary>
    /// Возвращает значения ячеек, хранящих сведения о пользователях.
    /// </summary>
    public async Task<IList<IList<object>>?> GetUsersValuesAsync() {
        const string range = $"{UsersDatabaseSheetName}!A2:B";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values;
    }

    /// <summary>
    /// Обновляет значения ячеек пользователя с указанным id.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, в случае успеха, <see langword="false"/>, если отсутствует пользователь, с указанным id.
    /// </returns>
    public async Task<bool> TryUpdateUserValuesAsync(string userId, string state) {
        const string usersIdsRange = $"{UsersDatabaseSheetName}!A:A";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, usersIdsRange);

        var getResponse = await getRequest.ExecuteAsync();
        var usersIdsValues = getResponse.Values;

        int? userRowIndex = null;
        for (var i = 0; i < usersIdsValues.Count; i++) {
            if (usersIdsValues[i].Count <= 0) {
                continue;
            }

            if (usersIdsValues[i][0].ToString() == userId) {
                userRowIndex = i + 1;
            }
        }

        if (userRowIndex is null) {
            return false;
        }

        var updateCell = $"{UsersDatabaseSheetName}!B{userRowIndex}";
        var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        var requestBody = new ValueRange {
            Values = new List<IList<object>> { new List<object> { state } }
        };

        var updateRequest = sheetsService.Spreadsheets.Values.Update(requestBody, projectsShowcaseSheetId, updateCell);
        updateRequest.ValueInputOption = valueInputOption;

        await updateRequest.ExecuteAsync();
        return true;
    }

    /// <summary>
    /// Возвращает значения ячейки, хранящей сведения о максимальном количестве команд в одной учебной группе.
    /// </summary>
    public async Task<object?> GetMaxTeamsPerGroupValueAsync() {
        const string range = $"{ConstraintsSheetName}!B1:B1";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values[0][0];
    }

    /// <summary>
    /// Возвращает значения ячейки, хранящей сведения о максимальном количестве резервов проекта.
    /// </summary>
    public async Task<object?> GetMaxProjectReservationsValueAsync() {
        const string range = $"{ConstraintsSheetName}!B2:B2";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values[0][0];
    }

    /// <summary>
    /// Возвращает значения ячейки, хранящей сведения о максимальном количестве резервов проекта в одной учебной группе.
    /// </summary>
    public async Task<object?> GetMaxProjectReservationsSameGroupValueAsync() {
        const string range = $"{ConstraintsSheetName}!B3:B3";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values[0][0];
    }

    /// <summary>
    /// Возвращает значения ячеек, хранящих сведения о существующих проектах.
    /// </summary>
    public async Task<IList<IList<object>>?> GetProjectsValuesAsync() {
        const string range = $"{ProjectsSheetName}!A2:B";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values;
    }

    /// <summary>
    /// Возвращает значения ячеек, хранящих сведения о зарегистрированных командах.
    /// </summary>
    public async Task<IList<IList<object>>?> GetTeamsValuesAsync() {
        const string range = $"{TeamsSheetName}!A2:E";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, range);

        var getResponse = await getRequest.ExecuteAsync();
        return getResponse?.Values;
    }

    /// <summary>
    /// Добавляет в новые ячейки параметры новой команды.
    /// </summary>
    public async Task AddTeamsValuesAsync(IList<object> values) {
        const string appendRange = $"{TeamsSheetName}!A2:D";
        var valueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
        var insertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

        var requestBody = new ValueRange {
            Values = new List<IList<object>>()
        };
        requestBody.Values.Add(values);

        var appendRequest = sheetsService.Spreadsheets.Values.Append(requestBody, projectsShowcaseSheetId, appendRange);
        appendRequest.ValueInputOption = valueInputOption;
        appendRequest.InsertDataOption = insertDataOption;

        await appendRequest.ExecuteAsync();
    }

    /// <summary>
    /// Устанавливает значение ячейки проекта команды с лидером с указанным id.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, в случае успеха, <see langword="false"/>, если отсутствует команда, с лидером с указанным id.
    /// </returns>
    public async Task<bool> TrySetTeamProjectValueAsync(string projectNumber, string teamLeaderTelegramId) {
        const string teamLeadersIdsRange = $"{TeamsSheetName}!A:A";
        var getRequest = sheetsService.Spreadsheets.Values.Get(projectsShowcaseSheetId, teamLeadersIdsRange);

        var getResponse = await getRequest.ExecuteAsync();
        var teamLeadersIdsValues = getResponse.Values;

        int? teamRowIndex = null;
        for (var i = 0; i < teamLeadersIdsValues.Count; i++) {
            if (teamLeadersIdsValues[i].Count <= 0) {
                continue;
            }

            if (teamLeadersIdsValues[i][0].ToString() == teamLeaderTelegramId) {
                teamRowIndex = i + 1;
            }
        }

        if (teamRowIndex is null) {
            return false;
        }

        var updateCell = $"{TeamsSheetName}!E{teamRowIndex}";
        var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        var requestBody = new ValueRange {
            Values = new List<IList<object>> { new List<object> { projectNumber } }
        };

        var updateRequest = sheetsService.Spreadsheets.Values.Update(requestBody, projectsShowcaseSheetId, updateCell);
        updateRequest.ValueInputOption = valueInputOption;

        await updateRequest.ExecuteAsync();
        return true;
    }
}