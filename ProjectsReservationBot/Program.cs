using ProjectsReservationBot.Services;

var bot = new ProjectReservationBot();
bot.StartReceiving();

var stopSignalReceived = false;
Console.CancelKeyPress += (_, args) => {
    args.Cancel = true;
    stopSignalReceived = true;
};

while (!stopSignalReceived) { }