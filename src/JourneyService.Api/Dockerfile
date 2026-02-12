# Base stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy csproj files using their relative paths from the solution root
COPY ["src/JourneyService.Api/JourneyService.Api.csproj", "src/JourneyService.Api/"]
COPY ["src/JourneyService.Domain/JourneyService.Domain.csproj", "src/JourneyService.Domain/"]
COPY ["src/JourneyService.Infrastructure/JourneyService.Infrastructure.csproj", "src/JourneyService.Infrastructure/"]
COPY ["src/JourneyService.Application/JourneyService.Application.csproj", "src/JourneyService.Application/"]

# 2. Restore using the path relative to /src
RUN dotnet restore "src/JourneyService.Api/JourneyService.Api.csproj"

# 3. Copy the rest of the source code
COPY . .

# 4. FIX: Move into the project directory properly
# Since we are already in /src, we just go into the project folder
WORKDIR "/src/src/JourneyService.Api"

# 5. Build the project
RUN dotnet build "JourneyService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JourneyService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JourneyService.Api.dll"]