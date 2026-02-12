FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/RewardWorker/RewardWorker.csproj", "src/RewardWorker/"]
COPY ["src/JourneyService.Infrastructure/JourneyService.Infrastructure.csproj", "src/JourneyService.Infrastructure/"]
COPY ["src/JourneyService.Application/JourneyService.Application.csproj", "src/JourneyService.Application/"]
COPY ["src/JourneyService.Domain/JourneyService.Domain.csproj", "src/JourneyService.Domain/"]

RUN dotnet restore "src/RewardWorker/RewardWorker.csproj"

COPY . .
WORKDIR "/src/src/RewardWorker"
RUN dotnet publish "RewardWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RewardWorker.dll"]
