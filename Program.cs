using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SirRothchild;
using SirRothchild.Settings;

await Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddJsonFile("settings.json", true, false);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<DiscordOptions>()
            .Bind(context.Configuration.GetSection(DiscordOptions.SectionName))
            .ValidateOnStart();
            
        services.AddHostedService<DiscordBackgroundService>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();