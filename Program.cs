using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SirRothchild;
using SirRothchild.Settings;

await Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(configurationBuilder =>
    {
        configurationBuilder.AddJsonFile("settings.json", true, false);
        configurationBuilder.AddEnvironmentVariables("SIR_ROTHCHILD");
        configurationBuilder.AddCommandLine(args);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<DiscordOptions>()
            .Bind(context.Configuration.GetSection(DiscordOptions.SectionName))
            .ValidateOnStart();
            
        services.AddHostedService<DiscordService>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();