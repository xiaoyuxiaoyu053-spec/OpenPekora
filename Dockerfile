
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

# 恢复并编译项目
RUN dotnet restore Roblox/Roblox.Website/Roblox.Website.csproj

RUN dotnet publish Roblox/Roblox.Website/Roblox.Website.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# 检查配置文件
RUN test -f Roblox/Roblox.Website/appsettings.json
RUN test -f Roblox/Roblox.Website/game-servers.json

# 自动查找密钥文件，并复制到统一目录
RUN set -eu; \
    BLOB="$(find /src -type f -name 'PrivateKeyBlob.txt' -print -quit)"; \
    PEM="$(find /src -type f -name 'PrivateKey2048.pem' -print -quit)"; \
    test -n "$BLOB"; \
    test -n "$PEM"; \
    mkdir -p /app/Keys; \
    cp "$BLOB" /app/Keys/PrivateKeyBlob.txt; \
    cp "$PEM" /app/Keys/PrivateKey2048.pem

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /src/Roblox/Roblox.Website/appsettings.json /app/appsettings.json
COPY --from=build /src/Roblox/Roblox.Website/game-servers.json /app/game-servers.json
COPY --from=build /app/Keys/ /app/Keys/

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "exec dotnet Roblox.Website.dll --urls http://0.0.0.0:${PORT:-10000}"]
