FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PodcastApi.sln .
COPY PodcastApi/PodcastApi.csproj PodcastApi/
RUN dotnet restore PodcastApi/PodcastApi.csproj

COPY PodcastApi/ PodcastApi/
RUN dotnet publish PodcastApi/PodcastApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite db lives here; mount a volume on /app/data to persist it.
RUN mkdir -p /app/data

EXPOSE 8080
ENTRYPOINT ["dotnet", "PodcastApi.dll"]
