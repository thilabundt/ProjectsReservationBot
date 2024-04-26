namespace ProjectsReservationBot;
public static class Config
{
    public static string TELEGRAM_BOT_TOKEN { get; } = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? string.Empty;
    public static string GOOGLE_SPREADSHEET_ID { get; } = Environment.GetEnvironmentVariable("GOOGLE_SPREADSHEET_ID") ?? string.Empty;
}
