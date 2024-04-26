FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /ProjectsReservationBot

COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /App
COPY --from=build-env /ProjectsReservationBot/out .
COPY credentials.json /App/credentials.json
ENTRYPOINT ["dotnet", "ProjectsReservationBot.dll"]