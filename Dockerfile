
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

# 恢复依赖
RUN dotnet restore Roblox/Roblox.Website/Roblox.Website.csproj

# 发布项目
RUN dotnet publish Roblox/Roblox.Website/Roblox.Website.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# 检查配置文件和密钥文件
RUN test -f Roblox/Roblox.Website/appsettings.json
RUN test -f Roblox/Roblox.Website/game-servers.json
RUN test -f Keys/PrivateKeyBlob.txt
RUN test -f Keys/PrivateKey2048.pem

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 复制发布文件
COPY --from=build /app/publish .

# 复制配置文件
COPY --from=build /src/Roblox/Roblox.Website/appsettings.json /app/appsettings.json
COPY --from=build /src/Roblox/Roblox.Website/game-servers.json /app/game-servers.json

# 复制密钥目录，修复 Linux 下找不到 Keys 文件的问题
COPY --from=build /src/Keys/ /app/Keys/

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "exec dotnet Roblox.Website.dll --urls http://0.0.0.0:${PORT:-10000}"]
