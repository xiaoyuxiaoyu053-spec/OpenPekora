
using Roblox.Rendering;
using Roblox.Website.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Roblox;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.Hubs;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;

var domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

Roblox.Services.Database.Configure(
    configuration.GetSection("Postgres").Value
    ?? throw new InvalidOperationException("Missing Postgres configuration."));

Roblox.Services.Cache.Configure(
    configuration.GetSection("Redis").Value
    ?? throw new InvalidOperationException("Missing Redis configuration."));

Roblox.Configuration.CdnBaseUrl =
    configuration.GetSection("CdnBaseUrl").Value ?? "";
Roblox.Configuration.AssetDirectory =
    configuration.GetSection("Directories:Asset").Value!;
Roblox.Configuration.StorageDirectory =
    configuration.GetSection("Directories:Storage").Value!;
Roblox.Configuration.ThumbnailsDirectory =
    configuration.GetSection("Directories:Thumbnails").Value!;
Roblox.Configuration.GroupIconsDirectory =
    configuration.GetSection("Directories:GroupIcons").Value!;
Roblox.Configuration.PublicDirectory =
    configuration.GetSection("Directories:Public").Value!;
Roblox.Configuration.XmlTemplatesDirectory =
    configuration.GetSection("Directories:XmlTemplates").Value!;
Roblox.Configuration.JsonDataDirectory =
    configuration.GetSection("Directories:JsonData").Value!;
Roblox.Configuration.ScriptDirectory =
    configuration.GetSection("Directories:ScriptsData").Value!;
Roblox.Configuration.AdminBundleDirectory =
    configuration.GetSection("Directories:AdminBundle").Value!;
Roblox.Configuration.EconomyChatBundleDirectory =
    configuration.GetSection("Directories:EconomyChatBundle").Value!;
Roblox.Configuration.BaseUrl =
    configuration.GetSection("BaseUrl").Value!;
Roblox.Configuration.ShortBaseUrl =
    Roblox.Configuration.BaseUrl!.Replace("https://www.", "");
Roblox.Configuration.HCaptchaPublicKey =
    configuration.GetSection("HCaptcha:Public").Value!;
Roblox.Configuration.HCaptchaPrivateKey =
    configuration.GetSection("HCaptcha:Private").Value!;

Roblox.Configuration.DiscordClientId =
    configuration.GetSection("Discord:ClientId").Value!;
Roblox.Configuration.DiscordClientSecret =
    configuration.GetSection("Discord:ClientSecret").Value!;
Roblox.Configuration.DiscordGuildId =
    configuration.GetSection("Discord:GuildId").Value!;
Roblox.Configuration.DiscordBotToken =
    configuration.GetSection("Discord:BotToken").Value!;
Roblox.Configuration.DiscordLogChannelId =
    configuration.GetSection("Discord:LogChannelId").Value!;
Roblox.Configuration.DiscordApplicationCallback =
    Roblox.Configuration.BaseUrl
    + configuration.GetSection("Discord:ApplicationCallback").Value;
Roblox.Configuration.DiscordLoginCallback =
    Roblox.Configuration.BaseUrl
    + configuration.GetSection("Discord:LoginCallback").Value;
Roblox.Configuration.DiscordLinkCallback =
    Roblox.Configuration.BaseUrl
    + configuration.GetSection("Discord:LinkCallback").Value;

Roblox.Configuration.GameServerAuthorization =
    configuration.GetSection("GameServerAuthorization").Value!;
Roblox.Configuration.BotAuthorization =
    configuration.GetSection("BotAuthorization").Value!;
Roblox.Configuration.RccAuthorization =
    configuration.GetSection("RccAuthorization").Value!;
Roblox.Configuration.ArbiterAuthorization =
    configuration.GetSection("ArbiterAuthorization").Value!;
Roblox.Configuration.GameServerIp =
    configuration.GetSection("GameServerIp").Value!;
Roblox.Configuration.UserAgentBypassSecret =
    configuration.GetSection("UserAgentBypassSecret").Value!;
Roblox.Configuration.VerificationSecret =
    configuration.GetSection("VerificationSecret").Value!;
Roblox.Configuration.LuaScriptsDirectory =
    configuration.GetSection("Directories:RCCLuaScripts").Value!;

IConfiguration gameServerConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("game-servers.json", optional: false, reloadOnChange: false)
    .Build();

Roblox.Configuration.GameServerIpAddresses =
    gameServerConfig.GetSection("GameServers")
        .Get<IEnumerable<GameServerConfigEntry>>()!;

Roblox.Configuration.AssetValidationServiceUrl =
    configuration.GetSection("AssetValidation:BaseUrl").Value!;
Roblox.Configuration.AssetValidationServiceAuthorization =
    configuration.GetSection("AssetValidation:Authorization").Value!;

GameServerService.Configure(
    string.Join(
        Guid.NewGuid().ToString(),
        Enumerable.Range(0, 16).Select(_ => Guid.NewGuid().ToString())
    )
);

Roblox.Configuration.PackageShirtAssetId =
    long.Parse(configuration.GetSection("PackageShirtAssetId").Value!);
Roblox.Configuration.PackagePantsAssetId =
    long.Parse(configuration.GetSection("PackagePantsAssetId").Value!);

Roblox.Libraries.TwitterApi.TwitterApi.Configure(
    configuration.GetSection("Twitter:Bearer").Value
);

Roblox.Configuration.SignupAssetIds =
    configuration.GetSection("SignupAssetIds")
        .GetChildren()
        .Select(item => long.Parse(item.Value!));
Roblox.Configuration.SignupAvatarAssetIds =
    configuration.GetSection("SignupAvatarAssetIds")
        .GetChildren()
        .Select(item => long.Parse(item.Value!));

#if DEBUG
Roblox.Configuration.RobloxAppPrefix = "rbxeconsimdev:";
#endif

FeatureFlags.StartUpdateFlagTask();

List<long> ownerUserIds =
    configuration.GetSection("OwnerUserId").Get<List<long>>()!;
Roblox.Website.Filters.StaffFilter.Configure(ownerUserIds);

builder.Services.AddRazorPages();
builder.Services.AddRequestDecompression();

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
    options.RespectBrowserAcceptHeader = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    options.IgnoreObsoleteActions();
    options.IgnoreObsoleteProperties();
    options.CustomSchemaIds(type => type.FullName);
    options.EnableAnnotations();

    options.SwaggerDoc("UserV1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Users Api v1"
    });

    options.SchemaGeneratorOptions.SchemaIdSelector = type => type.ToString();
    options.OperationFilter<SwaggerFileOperationFilter>();

    var xmlFilename =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

    options.IncludeXmlComments(
        Path.Combine(AppContext.BaseDirectory, xmlFilename)
    );
});

builder.Services.AddMvc(options =>
{
    options.Conventions.Add(new ApiExplorerGetsOnlyConvention());
});

var app = builder.Build();

app.UseRouting();

app.UseSwaggerUI(options =>
{
    options.ShowCommonExtensions();
    options.SwaggerEndpoint("/swagger/UserV1/swagger.json", "UserV1");
});

var prepareResponseForCache = (StaticFileResponseContext context) =>
{
    const int durationInSeconds = 86400 * 365;
    context.Context.Response.Headers[HeaderNames.CacheControl] =
        "public,max-age=" + durationInSeconds;
    context.Context.Response.Headers.Remove(HeaderNames.LastModified);
};

// Only register static-file providers when their directories exist.
void UseStaticFilesIfExists(string directory, string requestPath)
{
    if (!Directory.Exists(directory))
    {
        Console.WriteLine(
            $"[Warning] Static directory not found; skipping {requestPath}: {directory}");
        return;
    }

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(directory),
        RequestPath = requestPath,
        OnPrepareResponse = prepareResponseForCache
    });
}

UseStaticFilesIfExists(
    Path.Combine(Roblox.Configuration.PublicDirectory, "css", "roblox"),
    "/css");

UseStaticFilesIfExists(
    Path.Combine(Roblox.Configuration.PublicDirectory, "js"),
    "/js");

UseStaticFilesIfExists(
    Path.Combine(Roblox.Configuration.PublicDirectory, "UnsecuredContent"),
    "/UnsecuredContent");

if (string.IsNullOrWhiteSpace(Roblox.Configuration.CdnBaseUrl))
{
    UseStaticFilesIfExists(
        Roblox.Configuration.ThumbnailsDirectory,
        "/images/thumbnails");

    UseStaticFilesIfExists(
        Roblox.Configuration.GroupIconsDirectory,
        "/images/groups");
}

UseStaticFilesIfExists(
    Path.Combine(Roblox.Configuration.PublicDirectory, "img"),
    "/img");

app.UseRobloxSessionMiddleware();
app.UseMiddleware<ThumbnailMiddleware>(
    Roblox.Configuration.ThumbnailsDirectory);
app.UseMiddleware<RobloxLoggingMiddleware>();
app.UseRobloxPlayerCorsMiddleware();
app.UseRobloxCsrfMiddleware();
app.UseApplicationGuardMiddleware();

Roblox.Website.Middleware.ApplicationGuardMiddleware.Configure(
    configuration.GetSection("Authorization").Value!);

Roblox.Website.Middleware.CsrfMiddleware.Configure(
    Guid.NewGuid().ToString()
    + Guid.NewGuid().ToString()
    + Guid.NewGuid().ToString());

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<FrontendProxyMiddleware>();
app.UseExceptionHandler("/error");

RenderingHandler.Configure();

SessionMiddleware.Configure(
    configuration.GetSection("Jwt:Sessions").Value!);

app.UseTimerMiddleware();
Roblox.Services.Signer.SignService.Setup();

_ = Task.Run(async () =>
{
    using var assets =
        Roblox.Services.ServiceProvider.GetOrCreate<AssetsService>();
    await assets.FixAssetImagesWithoutMetadata();
});

_ = Task.Run(AvatarService.StartTimerClear3D);

app.MapControllers();
app.MapRazorPages();
app.UseWebSockets();
app.UseRequestDecompression();
app.MapHub<MessageRouterHub>("/v1/router/signalr");

app.Run();
