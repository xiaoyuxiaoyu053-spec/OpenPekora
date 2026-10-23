
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Roblox/Roblox.Website/Roblox.Website.csproj

RUN dotnet publish Roblox/Roblox.Website/Roblox.Website.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# 确认必需的配置文件存在
RUN test -f Roblox/Roblox.Website/appsettings.json
RUN test -f Roblox/Roblox.Website/game-servers.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /src/Roblox/Roblox.Website/appsettings.json /app/appsettings.json
COPY --from=build /src/Roblox/Roblox.Website/game-servers.json /app/game-servers.json

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "exec dotnet Roblox.Website.dll --urls http://0.0.0.0:${PORT:-10000}"]
